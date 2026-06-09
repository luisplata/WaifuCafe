using UnityEngine;

namespace Customers
{
    public class CustomerFront : MonoBehaviour
    {
        [SerializeField] private Customer customer;
        [SerializeField] private CustomerComponentUi customerComponentUi;

        public Customer GetCustomer() => customer;

        public void Configure(Transform parentOfCustomers)
        {
            transform.position = parentOfCustomers.position;

            if (customerComponentUi != null)
            {
                customerComponentUi.Configure(customer);
                customerComponentUi.StartWaiting(customer.GetCurrentPhaseDuration());
            }
        }

        public void UpdateWaitingProgress(float elapsed)
        {
            customerComponentUi?.UpdateWaitingProgress(elapsed);
        }

        public void FinishWaitingAndClear()
        {
            customerComponentUi?.FinishWaitingAndClear();
        }

        public void MarkLeaving()
        {
            customerComponentUi?.MarkLeaving();
        }
    }
}