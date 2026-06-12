using System.Collections.Generic;
using UnityEngine;

public class CustomerFactory : MonoBehaviour
{
    [SerializeField] private List<CustomerClient> customers;
    private Dictionary<CustomerIdentify, CustomerClient> _customerDictionary;

    private void Start()
    {
        _customerDictionary = new Dictionary<CustomerIdentify, CustomerClient>();
        foreach (var customer in customers)
        {
            _customerDictionary.Add(customer.CustomerData.customerIdentify, customer);
        }
    }

    public CustomerClient GetCustomerById(CustomerIdentify customerId)
    {
        return _customerDictionary.TryGetValue(customerId, out var customer)
            ? customer
            : throw new KeyNotFoundException($"Customer with id {customerId} does not exist");
    }

    public CustomerClient GetCustomerByRandom()
    {
        var randomIndex = Random.Range(0, customers.Count);
        return customers[randomIndex];
    }
}