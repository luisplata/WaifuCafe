using System;
using System.Collections.Generic;
using UnityEngine;

namespace Customers.Queue
{
    public class CustomerQueue : MonoBehaviour
    {
        // ============ SERIALIZED FIELDS ============
        [Header("Queue Settings")]
        [SerializeField] private int maxQueueSize = 10;
        [SerializeField] private bool prioritizeVIP = true;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] private float spawnVariation = 1f;
        [SerializeField] private int maxCustomersInGame = 15;

        [Header("Customer Prefabs")]
        [SerializeField] private List<CustomerFront> customerPrefabs;

        // ============ PRIVATE FIELDS ============
        private Queue<Customer> queue = new Queue<Customer>();
        private List<Customer> activeCustomers = new List<Customer>();
        private Dictionary<Customer, CustomerFront> customerViews = new Dictionary<Customer, CustomerFront>();
        
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

        // ============ SPAWN MANAGEMENT ============
        private void UpdateSpawnTimer()
        {
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0f && CanSpawnMore())
            {
                SpawnCustomer();
                ResetSpawnTimer();
            }
        }

        private void SpawnCustomer()
        {
            if (customerPrefabs == null || customerPrefabs.Count == 0)
            {
                Debug.LogWarning("No customer prefabs assigned!");
                return;
            }

            // Instanciar prefab y extraer el Customer
            CustomerFront prefab = customerPrefabs[UnityEngine.Random.Range(0, customerPrefabs.Count)];
            CustomerFront instance = Instantiate(prefab);
            Customer customerData = instance.GetCustomer();

            if (customerData == null)
            {
                Debug.LogWarning("Spawned CustomerFront has no Customer data assigned.");
                Destroy(instance.gameObject);
                return;
            }

            EnqueueCustomer(customerData);
            customerViews[customerData] = instance;
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

            OnCustomerEnqueued?.Invoke(customer);
            OnQueueCountChanged?.Invoke(queue.Count);

            Debug.Log($"Customer enqueued. Queue size: {queue.Count}");
        }

        public Customer DequeueCustomer()
        {
            if (queue.Count == 0)
            {
                OnQueueEmpty?.Invoke();
                return null;
            }

            Customer customer = queue.Dequeue();
            return CompleteService(customer);
        }

        public Customer ServeCustomer(Customer customer)
        {
            if (customer == null || queue.Count == 0)
            {
                return null;
            }

            bool removed = RemoveFromQueue(customer);
            if (!removed)
            {
                return null;
            }

            return CompleteService(customer);
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
                customersLeft++;
                OnQueueCountChanged?.Invoke(queue.Count);

                Debug.Log($"Customer removed from queue. Remaining: {queue.Count}");
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
                customer.WaitTime += Time.deltaTime;

                // Remove if patience expires
                if (customer.WaitTime > customer.Patience)
                {
                    RemoveCustomer(customer);
                    break; // Avoid modifying collection while iterating
                }
            }
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
            Debug.Log($"=== Queue Statistics ===\n" +
                $"Queue Size: {queue.Count}/{maxQueueSize}\n" +
                $"Customers Served: {customersServed}\n" +
                $"Customers Left: {customersLeft}\n" +
                $"Average Wait Time: {GetAverageWaitTime():F2}s\n" +
                $"Queue Satisfaction: {GetAverageSatisfaction()}%");
        }

        // ============ VALIDATION ============
        private void ValidateSettings()
        {
            if (customerPrefabs == null || customerPrefabs.Count == 0)
                Debug.LogError("CustomerQueue: No customer prefabs assigned!");

            if (maxQueueSize <= 0)
                Debug.LogWarning("CustomerQueue: maxQueueSize should be > 0");

            if (spawnInterval <= 0)
                Debug.LogWarning("CustomerQueue: spawnInterval should be > 0");
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

        private Customer CompleteService(Customer customer)
        {
            activeCustomers.Remove(customer);
            TryDestroyCustomerView(customer);

            customersServed++;
            totalWaitTime += customer.WaitTime;

            OnCustomerDequeued?.Invoke(customer);
            OnQueueCountChanged?.Invoke(queue.Count);

            return customer;
        }
    }
}