using System;
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
    private StepsOfRun currentStep;

    [Header("Prototype: previsión y oleadas")]
    [SerializeField] private int previewCount = 3;
    [SerializeField] private float waveRate = 1f;
    private readonly List<NextSpawn> preview = new();
    public event Action OnPreviewChanged;
    public IReadOnlyList<NextSpawn> Preview => preview;
    public float WaveRate { get => waveRate; set => waveRate = Mathf.Max(0.05f, value); }

    [Serializable]
    public class NextSpawn
    {
        public CustomerClient customerPrefab;
        public FoodModel food;
    }


    private void FixedUpdate()
    {
        if (!isConfigured) return;
        localTime += Time.fixedDeltaTime;

        foreach (var stepsOfRun in stepsOfRuns)
        {
            if (_gameRules.Percent >= stepsOfRun.percente)
            {
                timeToSpawn = stepsOfRun.spawnPerSecond;
                currentStep = stepsOfRun;
                stepsOfRuns.Remove(stepsOfRun);
                break;
            }
        }

        if (currentStep == null) return;
        RefillPreview();
        if (!(localTime >= timeToSpawn / waveRate)) return;
        SpawnCustomers();
        localTime = 0f;
    }

    private void SpawnCustomers()
    {
        for (int i = 0; i < currentStep.countOfCustomer; i++)
        {
            if (preview.Count == 0) RefillPreview();
            if (preview.Count == 0) break;
            if (!customerPositions.GetNextSeat(out var seat))
            {
                break;
            }

            var next = preview[0];
            preview.RemoveAt(0);

            var customer = Instantiate(
                next.customerPrefab,
                customerSpawnPosition.transform.position,
                Quaternion.identity
            );

            customer.Configure(
                seat,
                customerSpawnPosition,
                next.food,
                GetModificadorDePaciencia(),
                this
            );
            customer.OnCustomerAttended += OnCustomerAttended;
            customer.OnLeftGo += () => { customer.OnCustomerAttended -= OnCustomerAttended; };
            customer.OnServedPoints += OnServedPoints;
        }
        RefillPreview();
        OnPreviewChanged?.Invoke();
    }

    private void RefillPreview()
    {
        if (currentStep == null) return;
        while (preview.Count < previewCount)
        {
            preview.Add(new NextSpawn { customerPrefab = RollCustomer(), food = RollFood() });
        }
    }

    private CustomerClient RollCustomer()
    {
        return currentStep.isRandomCustomer ||
               !ShouldSpawnSpecific(currentStep.specificCustomerProbability)
            ? customerFactory.GetCustomerByRandom()
            : customerFactory.GetCustomerById(currentStep.customerIdentify);
    }

    private FoodModel RollFood()
    {
        return currentStep.isRandomFood ||
               !ShouldSpawnSpecific(currentStep.specificFoodProbability)
            ? foodFactory.GetFoodByRandom()
            : foodFactory.GetFoodByType(currentStep.foodModelType);
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

    private void OnServedPoints(float points)
    {
        _gameRules.AddPoints(Mathf.RoundToInt(points));
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

    private bool ShouldSpawnSpecific(float probability)
    {
        return UnityEngine.Random.value <= probability;
    }

    public void Stop()
    {
        isConfigured = false;
    }
}