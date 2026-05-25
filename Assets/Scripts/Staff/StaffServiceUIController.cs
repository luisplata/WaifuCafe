using UnityEngine;
using Customers;
using Customers.Queue;

namespace Staff
{
    /// <summary>
    /// Controla la asignación manual de clientes a staff mediante selecciones de UI.
    /// El flujo es: SelectStaff(i) -> SelectCustomer(j) -> ConfirmAssignment()
    /// </summary>
    public class StaffServiceUIController : MonoBehaviour
    {
        [SerializeField] private StaffManager staffManager;
        [SerializeField] private CustomerQueue customerQueue;

        private int selectedStaffIndex = -1;
        private int selectedCustomerQueueIndex = -1;

        // Método para seleccionar un staff por su índice (0, 1, 2, etc)
        public void SelectStaff(int staffIndex)
        {
            if (staffManager == null)
            {
                Debug.LogWarning("StaffServiceUIController: StaffManager no asignado");
                return;
            }

            var staff = staffManager.GetStaffByIndex(staffIndex);
            if (staff == null)
            {
                Debug.LogWarning($"No se encontró staff con índice {staffIndex}");
                return;
            }

            selectedStaffIndex = staffIndex;
            Debug.Log($"[UI] Staff {staffIndex} seleccionado");
        }

        // Método para seleccionar un cliente de la cola por posición (0 = primero, 1 = segundo, etc)
        public void SelectCustomer(int queuePosition)
        {
            if (customerQueue == null)
            {
                Debug.LogWarning("StaffServiceUIController: CustomerQueue no asignado");
                return;
            }

            var queuedCustomers = customerQueue.GetQueuedCustomersSnapshot();
            if (queuePosition < 0 || queuePosition >= queuedCustomers.Count)
            {
                Debug.LogWarning($"Posición de cola {queuePosition} fuera de rango (total: {queuedCustomers.Count})");
                return;
            }

            selectedCustomerQueueIndex = queuePosition;
            Debug.Log($"[UI] Customer en posición {queuePosition} seleccionado");
        }

        // Método para confirmar y ejecutar la asignación
        public void ConfirmAssignment()
        {
            if (selectedStaffIndex < 0)
            {
                Debug.LogWarning("[UI] No hay staff seleccionado");
                return;
            }

            if (selectedCustomerQueueIndex < 0)
            {
                Debug.LogWarning("[UI] No hay customer seleccionado");
                return;
            }

            if (staffManager == null || customerQueue == null)
            {
                Debug.LogWarning("[UI] StaffManager o CustomerQueue no asignados");
                return;
            }

            // Obtener el staff
            var staff = staffManager.GetStaffByIndex(selectedStaffIndex);
            if (staff == null || !staff.CanAttend())
            {
                Debug.LogWarning($"[UI] Staff {selectedStaffIndex} no está disponible");
                return;
            }

            // Obtener el cliente de la posición en la cola
            var queuedCustomers = customerQueue.GetQueuedCustomersSnapshot();
            if (selectedCustomerQueueIndex >= queuedCustomers.Count)
            {
                Debug.LogWarning($"[UI] Posición de cliente {selectedCustomerQueueIndex} ya no existe en la cola");
                return;
            }

            Customer customer = queuedCustomers[selectedCustomerQueueIndex];
            
            // Sacar al cliente de la cola
            customerQueue.ServeCustomer(customer); // Usa el mejor método existente; para ser manual quitamos directamente
            
            // Asignar al staff (esto usa la corutina de ServiceTime)
            bool assigned = staffManager.TryAssignCustomer(customer);
            
            if (assigned)
            {
                Debug.Log($"[UI] Staff {selectedStaffIndex} asignado a customer. Tiempo de servicio: {staff.GetServiceTime()} segundos");
            }
            else
            {
                Debug.LogWarning($"[UI] Fallo al asignar staff {selectedStaffIndex} a customer");
                // Re-encolar el cliente si falla la asignación (simplemente re-añadir a la cola)
                customerQueue.EnqueueCustomer(customer);
            }

            // Limpiar selecciones
            selectedStaffIndex = -1;
            selectedCustomerQueueIndex = -1;
        }

        // Método auxiliar para limpiar selecciones
        public void ClearSelection()
        {
            selectedStaffIndex = -1;
            selectedCustomerQueueIndex = -1;
            Debug.Log("[UI] Selecciones limpias");
        }

        // Getters para consultar estado actual (opcional, para mostrar en UI)
        public int GetSelectedStaffIndex() => selectedStaffIndex;
        public int GetSelectedCustomerQueueIndex() => selectedCustomerQueueIndex;
    }
}

