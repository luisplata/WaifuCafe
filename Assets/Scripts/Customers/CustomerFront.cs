using DragAndDrop;
using UnityEngine;

namespace Customers
{
    public class CustomerFront : MonoBehaviour
    {
        [SerializeField] private Customer customer;
        [SerializeField] private CustomerDropSlot customerDropSlot;
        private CustomerDropSlot _customerDropSlotInstance;
        private CustomerComponentUi _customerComponentUi;

        public Customer GetCustomer() => customer;

        public void Configure(Transform parentOfCustomers)
        {
            _customerDropSlotInstance = Instantiate(customerDropSlot, parentOfCustomers);
            _customerDropSlotInstance.Configure(customer);

            _customerComponentUi = _customerDropSlotInstance.GetComponent<CustomerComponentUi>();
            if (_customerComponentUi != null)
            {
                _customerComponentUi.Configure(customer);
                _customerComponentUi.StartWaiting(customer.GetCurrentPhaseDuration());
            }
        }

        public void UpdateWaitingProgress(float elapsed)
        {
            _customerComponentUi?.UpdateWaitingProgress(elapsed);
        }

        public void FinishWaitingAndClear()
        {
            _customerComponentUi?.FinishWaitingAndClear();

            // Destruir el CustomerDropSlot asociado (se instancia en parent separado)
            if (_customerDropSlotInstance != null)
            {
                Destroy(_customerDropSlotInstance.gameObject);
                _customerDropSlotInstance = null;
            }
            // También limpiar referencia al componente UI
            _customerComponentUi = null;
        }

        public void MarkLeaving()
        {
            _customerComponentUi?.MarkLeaving();
            // In case leaving finishes without explicit destroy later, ensure drop slot will be removed
            if (_customerDropSlotInstance != null)
            {
                // The CustomerQueue normally calls RemoveCustomer after a delay; keep removal here minimal.
                // We don't destroy immediately to allow leaving animation to play, but null references will prevent leaks.
            }
        }

        private void OnDestroy()
        {
            // Ensure the instantiated drop slot is cleaned up when the CustomerFront is destroyed
            if (_customerDropSlotInstance != null)
            {
                Destroy(_customerDropSlotInstance.gameObject);
                _customerDropSlotInstance = null;
            }
        }
    }
}