using System;
using UnityEngine;
using V2.Customer;
using V2.Food;

public class CustomerSpawnerCoroutine : MonoBehaviour
{
    [SerializeField] private FoodFactory foodFactory;
    [SerializeField] private CustomerFactory customerFactory;
    [SerializeField] private CustomerClient customerPrefab;
    [SerializeField] private float timeToSpawn;
    [SerializeField] private int countOfCustomerByStep;
    [SerializeField] private CustomerPositions customerPositions;
    [SerializeField] private GameObject customerSpawnPosition;
    private float localTime;
    private IGameRules _gameRules;
    private bool isConfigured;


    private void FixedUpdate()
    {
        if (!isConfigured) return;
        localTime += Time.fixedDeltaTime;

        if (!(localTime >= timeToSpawn)) return;
        SpawnCustomers();
        localTime = 0f;
    }

    private void SpawnCustomers()
    {
        for (int i = 0; i < countOfCustomerByStep; i++)
        {
            if (!customerPositions.GetNextSeat(out var seat))
            {
                break;
            }

            var customer = Instantiate(
                customerFactory.GetCustomerByRandom(),
                customerSpawnPosition.transform.position,
                Quaternion.identity
            );
            customer.Configure(seat, customerSpawnPosition, foodFactory.GetFoodByRandom());
            customer.OnCustomerAttended += OnCustomerAttended;
        }
    }

    private void OnCustomerAttended(CustomerClientModel customer, FoodModel food)
    {
        _gameRules.CustomerAttended(customer, food);
    }

    public void Configure(IGameRules gameRules)
    {
        localTime = timeToSpawn;
        _gameRules = gameRules;
        isConfigured = true;
    }
}

public enum CustomerIdentify
{
    None,
    Customer1,
    Customer2,
    Customer3,
    Customer4
}