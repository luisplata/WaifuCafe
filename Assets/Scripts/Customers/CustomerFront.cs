using UnityEngine;

namespace Customers
{
    public class CustomerFront : MonoBehaviour
    {
        [SerializeField] private Customer customer;

        public Customer GetCustomer() => customer;
    }
}