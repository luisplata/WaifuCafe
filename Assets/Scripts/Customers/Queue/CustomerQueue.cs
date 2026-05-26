using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StateMachines;

namespace Customers.Queue
{
    public class CustomerQueue : MonoBehaviour
    {
        // ============ SERIALIZED FIELDS ============
        [Header("Queue Settings")] [SerializeField]
        private int maxQueueSize = 10;

        [SerializeField] private bool prioritizeVIP = true;

        [Header("Spawn Settings")] [SerializeField]
        private float spawnInterval = 3f;

        [SerializeField] private float spawnVariation = 1f;
        [SerializeField] private int maxCustomersInGame = 15;
        [SerializeField] private int maxSpawn = 3;

        [Header("Customer Prefabs")] [SerializeField]
        private List<CustomerFront> customerPrefabs;

        [SerializeField] private Transform parentOfCustomers;

        // ============ PRIVATE FIELDS ============
        private Queue<Customer> queue = new Queue<Customer>();
        private List<Customer> activeCustomers = new List<Customer>();
        private Dictionary<Customer, CustomerFront> customerViews = new Dictionary<Customer, CustomerFront>();
        private HashSet<Customer> _pendingRemoval = new HashSet<Customer>();

        private float spawnTimer = 0f;
        private bool isSpawningEnabled = true;
        private bool isPaused = false;

        // Statistics
        private int customersServed = 0;
        private int customersLeft = 0;
        private float totalWaitTime = 0f;

        // ============ EVENTS ============
        public event Action<Customer> OnCustomerEnqueued;
        public event Action<Customer> OnCustomerDequeued;
        public event Action OnQueueFull;
        public event Action OnQueueEmpty;
        public event Action<int> OnQueueCountChanged;

        // ============ LIFECYCLE ============
        private void Start()
        {
            ValidateSettings();
            ResetSpawnTimer();
        }

        private void Update()
        {
            if (isPaused || !isSpawningEnabled) return;

            UpdateSpawnTimer();
            UpdateCustomerPatience();
        }

        private bool _isFirstCall = true;

        // ============ SPAWN MANAGEMENT ============
        private void UpdateSpawnTimer()
        {
            spawnTimer -= Time.deltaTime;

            if (_isFirstCall || spawnTimer <= 0f && CanSpawnMore())
            {
                SpawnCustomer();
                ResetSpawnTimer();
                _isFirstCall = false;
            }
        }

        private void SpawnCustomer()
        {
            if (customerPrefabs == null || customerPrefabs.Count == 0)
            {
                return;
            }

            for (int i = 0; i <= maxSpawn; i++)
            {
                // Instanciar prefab y extraer el Customer
                CustomerFront prefab = customerPrefabs[UnityEngine.Random.Range(0, customerPrefabs.Count)];
                CustomerFront instance = Instantiate(prefab);
                instance.Configure(parentOfCustomers);
                Customer customerData = instance.GetCustomer();

                if (customerData == null)
                {
                    Destroy(instance.gameObject);
                    return;
                }

                customerData.StartPhase(CustomerPhase.Llegada);
                EnqueueCustomer(customerData);
                customerViews[customerData] = instance;
            }
        }

        private bool CanSpawnMore()
        {
            return activeCustomers.Count < maxCustomersInGame && queue.Count < maxQueueSize;
        }

        private void ResetSpawnTimer()
        {
            spawnTimer = spawnInterval + UnityEngine.Random.Range(-spawnVariation, spawnVariation);
            spawnTimer = Mathf.Max(0.1f, spawnTimer);
        }

        // ============ QUEUE OPERATIONS ============
        public void EnqueueCustomer(Customer customer)
        {
            if (queue.Count >= maxQueueSize)
            {
                OnQueueFull?.Invoke();
                return;
            }

            customer.WaitTime = 0f;
            queue.Enqueue(customer);
            activeCustomers.Add(customer);

            if (customerViews.TryGetValue(customer, out var view))
            {
                view.UpdateWaitingProgress(customer.GetCurrentPhaseElapsed());
            }

            OnCustomerEnqueued?.Invoke(customer);
            OnQueueCountChanged?.Invoke(queue.Count);
        }

        public Customer DequeueCustomer()
        {
            if (queue.Count == 0)
            {
                OnQueueEmpty?.Invoke();
                return null;
            }

            if (!TryTakeNextCustomer(out var customer))
            {
                return null;
            }

            return customer;
        }

        public Customer ServeCustomer(Customer customer)
        {
            if (customer == null || queue.Count == 0)
            {
                return null;
            }

            if (!queue.Contains(customer) || customer.CurrentPhase != CustomerPhase.EsperaPedido)
            {
                return null;
            }

            customer.StartPhase(CustomerPhase.EsperandoEntrega);
            SyncCustomerView(customer);
            return customer;
        }

        public Customer ServeCustomerAt(int queueIndex)
        {
            if (queueIndex < 0 || queueIndex >= queue.Count)
            {
                return null;
            }

            Customer[] snapshot = queue.ToArray();
            Customer selected = snapshot[queueIndex];
            return ServeCustomer(selected);
        }

        // Intenta tomar el siguiente cliente para que lo atienda un staff.
        // No marca el servicio como completado; eso debe llamarse luego con FinishService.
        public bool TryTakeNextCustomer(out Customer customer)
        {
            customer = null;

            if (queue.Count == 0)
            {
                OnQueueEmpty?.Invoke();
                return false;
            }

            foreach (var current in queue)
            {
                if (current == null || current.CurrentPhase != CustomerPhase.EsperaPedido)
                {
                    continue;
                }

                current.StartPhase(CustomerPhase.EsperandoEntrega);
                SyncCustomerView(current);
                customer = current;
                return true;
            }

            return false;
        }

        // Finaliza el servicio de un cliente (llamado por el staff cuando termina)
        public void FinishService(Customer customer)
        {
            if (customer == null) return;
            CompleteService(customer);
        }

        public Customer PeekCustomer()
        {
            return queue.Count > 0 ? queue.Peek() : null;
        }

        public void RemoveCustomer(Customer customer)
        {
            if (activeCustomers.Remove(customer))
            {
                // Recreate queue without this customer
                Queue<Customer> newQueue = new Queue<Customer>();
                foreach (var c in queue)
                {
                    if (c != customer)
                        newQueue.Enqueue(c);
                }

                queue = newQueue;

                TryDestroyCustomerView(customer);

                // Contabilizar salida: si fue servido, aumentar customersServed y agregar wait time,
                // si se fue por impaciencia, contabilizar customersLeft.
                if (customer.WasServed)
                {
                    customersServed++;
                    totalWaitTime += customer.WaitTime;
                }
                else
                {
                    customersLeft++;
                }

                OnQueueCountChanged?.Invoke(queue.Count);
            }
        }

        // ============ QUEUE STATE ============
        public int GetQueueCount() => queue.Count;

        public int GetMaxQueueSize() => maxQueueSize;

        public List<Customer> GetQueuedCustomersSnapshot()
        {
            return new List<Customer>(queue.ToArray());
        }

        public int GetAverageSatisfaction()
        {
            if (customersServed == 0) return 100;

            float averageWaitTime = totalWaitTime / customersServed;
            return Mathf.Max(0, (int)(100 - (averageWaitTime * 5))); // Loses 5% satisfaction per second waited
        }

        public bool IsQueueFull() => queue.Count >= maxQueueSize;

        public bool IsQueueEmpty() => queue.Count == 0;

        // ============ PATIENCE MANAGEMENT ============
        private void UpdateCustomerPatience()
        {
            foreach (var customer in queue)
            {
                customer.UpdatePhase(Time.deltaTime);

                if (customerViews.TryGetValue(customer, out var view))
                {
                    view.UpdateWaitingProgress(customer.GetCurrentPhaseElapsed());
                }

                if (customer.CurrentPhase != CustomerPhase.EsperaPedido)
                {
                    if (customer.CurrentPhase == CustomerPhase.Irse &&
                        customer.GetCurrentPhaseElapsed() >= customer.GetCurrentPhaseDuration() &&
                        _pendingRemoval.Add(customer))
                    {
                        StartCoroutine(RemoveCustomerDelayed(customer, 0.6f));
                    }

                    continue;
                }

                customer.WaitTime += Time.deltaTime;

                // Remove if patience expires (play leaving transition first)
                if (customer.WaitTime > customer.Patience)
                {
                    // Start a short visual transition before removing
                    customer.StartPhase(CustomerPhase.Irse);
                    StartCoroutine(RemoveCustomerDelayed(customer, 0.6f));
                    break; // Avoid modifying collection while iterating
                }
            }
        }

        private IEnumerator RemoveCustomerDelayed(Customer customer, float delay)
        {
            if (customer == null) yield break;

            if (customerViews.TryGetValue(customer, out var view))
            {
                view.MarkLeaving();
            }

            yield return new WaitForSeconds(delay);

            RemoveCustomer(customer);
            _pendingRemoval.Remove(customer);
        }

        // ============ GAME CONTROL ============
        public void PauseSpawning()
        {
            isPaused = true;
        }

        public void ResumeSpawning()
        {
            isPaused = false;
            ResetSpawnTimer();
        }

        public void SetSpawningEnabled(bool enabled)
        {
            isSpawningEnabled = enabled;
            if (enabled) ResetSpawnTimer();
        }

        public void ClearQueue()
        {
            queue.Clear();
            activeCustomers.Clear();
            DestroyAllCustomerViews();
            OnQueueCountChanged?.Invoke(0);
        }

        public void Reset()
        {
            ClearQueue();
            customersServed = 0;
            customersLeft = 0;
            totalWaitTime = 0f;
            ResetSpawnTimer();
            isPaused = false;
        }

        // ============ STATISTICS ============
        public int GetCustomersServed() => customersServed;

        public int GetCustomersLeft() => customersLeft;

        public float GetAverageWaitTime() => customersServed > 0 ? totalWaitTime / customersServed : 0f;

        public int GetActiveCustomerCount() => activeCustomers.Count;

        public void PrintStatistics()
        {
        }

        // ============ VALIDATION ============
        private void ValidateSettings()
        {
            if (customerPrefabs == null || customerPrefabs.Count == 0) return;

            if (maxQueueSize <= 0) return;

            if (spawnInterval <= 0) return;
        }

        // ============ DEBUG ============
        private void OnGUI()
        {
            // Optional: Remove this in production
            if (GUI.Button(new Rect(10, 10, 150, 30), "Print Queue Stats"))
            {
                PrintStatistics();
            }
        }

        private void OnDestroy()
        {
            DestroyAllCustomerViews();
        }

        private void TryDestroyCustomerView(Customer customer)
        {
            if (customer == null)
            {
                return;
            }

            if (customerViews.TryGetValue(customer, out CustomerFront view))
            {
                if (view != null)
                {
                    Debug.Log($"[CustomerQueue] Destroying CustomerFront view: {view.name}");
                    Destroy(view.gameObject);
                }

                customerViews.Remove(customer);
            }
        }

        private void DestroyAllCustomerViews()
        {
            foreach (var pair in customerViews)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }

            customerViews.Clear();
        }

        private bool RemoveFromQueue(Customer customer)
        {
            bool removed = false;
            Queue<Customer> newQueue = new Queue<Customer>();

            foreach (Customer current in queue)
            {
                if (!removed && current == customer)
                {
                    removed = true;
                    continue;
                }

                newQueue.Enqueue(current);
            }

            if (removed)
            {
                queue = newQueue;
            }

            return removed;
        }

        // Completa el servicio para el cliente. Public para que sistemas externos (ej. Staff) puedan invocarlo
        public Customer CompleteService(Customer customer)
        {
            if (customer == null) return null;

            // Evitar doble finalización: solo completar si realmente estaba esperando entrega.
            if (customer.CurrentPhase != CustomerPhase.EsperandoEntrega)
            {
                return null;
            }

            // Cuando el staff completa la entrega, el cliente debe pasar a Consumir
            // pero permanecer visible hasta que termine de consumir y se vaya.
            customer.WasServed = true;
            customer.StartPhase(CustomerPhase.Consumir);

            // Actualizar la vista si existe
            if (customerViews.TryGetValue(customer, out var view))
            {
                view.UpdateWaitingProgress(customer.GetCurrentPhaseElapsed());
            }

            // No removemos ni destruimos la vista aquí: la eliminación ocurre cuando
            // el cliente completa su fase de Irse (ver UpdateCustomerPatience / RemoveCustomerDelayed).
            return customer;
        }

        private void SyncCustomerView(Customer customer)
        {
            if (customer == null) return;

            if (customerViews.TryGetValue(customer, out var view) && view != null)
            {
                view.UpdateWaitingProgress(customer.GetCurrentPhaseElapsed());
            }
        }
    }
}