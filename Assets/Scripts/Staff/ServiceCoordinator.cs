using UnityEngine;
using Customers;
using Customers.Queue;

namespace Staff
{
    /// <summary>
    /// Orquesta la asignación automática entre CustomerQueue y StaffManager.
    /// Cuando hay clientes en la cola y staff disponible, toma el siguiente cliente
    /// y lo envía al staff. Cuando el servicio termina, notifica a la cola para
    /// que complete el servicio.
    /// </summary>
    public class ServiceCoordinator : MonoBehaviour
    {
        [SerializeField] private StaffManager staffManager;
        [SerializeField] private CustomerQueue customerQueue;

        [SerializeField] private bool isAutomaticMode = false;

        private void Start()
        {
            if (!isAutomaticMode) return; // Si es modo manual, no hacer nada

            if (customerQueue != null)
            {
                customerQueue.OnCustomerEnqueued += OnCustomerEnqueuedHandler;
                customerQueue.OnQueueCountChanged += OnQueueCountChangedHandler;
            }

            if (staffManager != null)
            {
                staffManager.OnServiceCompleted += HandleServiceCompleted;
                staffManager.OnStaffBecameFree += OnStaffBecameFreeHandler;
            }

            // Intentar asignar al inicio en caso de que ya haya clientes o staff configurado
            TryAssignNext();
        }

        private void OnDestroy()
        {
            if (customerQueue == null || !isAutomaticMode) return;

            if (customerQueue != null)
            {
                customerQueue.OnCustomerEnqueued -= OnCustomerEnqueuedHandler;
                customerQueue.OnQueueCountChanged -= OnQueueCountChangedHandler;
            }

            if (staffManager != null)
            {
                staffManager.OnServiceCompleted -= HandleServiceCompleted;
                staffManager.OnStaffBecameFree -= OnStaffBecameFreeHandler;
            }
        }

        private void OnCustomerEnqueuedHandler(Customer c)
        {
            TryAssignNext();
        }

        private void OnQueueCountChangedHandler(int count)
        {
            TryAssignNext();
        }

        private void OnStaffBecameFreeHandler(StaffFront sf)
        {
            TryAssignNext();
        }

        // Intenta asignar tantos clientes como staff disponible haya
        private void TryAssignNext()
        {
            if (customerQueue == null || staffManager == null) return;

            // Mientras haya staff libre y clientes en la cola, asignar
            while (true)
            {
                var availableIndex = staffManager.GetAvailableStaffIndex();
                if (availableIndex < 0) break;

                if (!customerQueue.TryTakeNextCustomer(out Customer c)) break;

                // Asignar al staff; si falla (por race), re-enqueue al cliente al final
                bool assigned = staffManager.TryAssignCustomer(c);
                if (!assigned)
                {
                    // No se pudo asignar: volver a encolar al frente
                    customerQueue.EnqueueCustomer(c);
                    break;
                }
            }
        }

        // Cuando el staff completa el servicio, avisar a CustomerQueue para finalizar el servicio
        private void HandleServiceCompleted(StaffFront sf, Customer customer)
        {
            if (customerQueue != null)
            {
                customerQueue.FinishService(customer);
            }
        }
    }
}



