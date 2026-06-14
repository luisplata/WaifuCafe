using System.Collections.Generic;
using UnityEngine;
using V2.Customer;
using V2.Food;

public class CustomerSpawnerCoroutine : MonoBehaviour, ICustomerSpawn
{
    [SerializeField] private FoodFactory foodFactory;
    [SerializeField] private CustomerFactory customerFactory;
    [SerializeField] private CustomerClient customerPrefab;
    [SerializeField] private float timeToSpawn;
    [SerializeField] private int countOfCustomerByStep;
    [SerializeField] private CustomerPositions customerPositions;
    [SerializeField] private GameObject customerSpawnPosition;
    [SerializeField] private List<StepsOfRun> stepsOfRuns;
    private float localTime;
    private IGameRules _gameRules;
    private bool isConfigured;


    private void FixedUpdate()
    {
        if (!isConfigured) return;
        localTime += Time.fixedDeltaTime;

        foreach (var stepsOfRun in stepsOfRuns)
        {
            if (_gameRules.Percent >= stepsOfRun.percente)
            {
                timeToSpawn = stepsOfRun.spawnPerSecond;
                stepsOfRuns.Remove(stepsOfRun);
                break;
            }
        }

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
            customer.Configure(seat, customerSpawnPosition, foodFactory.GetFoodByRandom(), GetModificadorDePaciencia(),
                this);
            customer.OnCustomerAttended += OnCustomerAttended;
            customer.OnLeftGo += () => { customer.OnCustomerAttended -= OnCustomerAttended; };
        }
    }

    private float GetModificadorDePaciencia()
    {
        if (_gameRules.IsPatienceAltered())
        {
            return _gameRules.GetAlteredPatience();
        }

        return 0;
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

    public bool IsEconomyModify()
    {
        return _gameRules.IsEconomyModify();
    }

    public IGameRules GetGameRules()
    {
        return _gameRules;
    }
}