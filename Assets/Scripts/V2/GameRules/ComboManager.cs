using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using V2.Customer;
using V2.Food;

public class ComboManager : MonoBehaviour, IComboManager
{
    [SerializeField] private CustomerComboManager customerComboManager;
    [SerializeField] private FoodComboManager foodComboManager;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI comboFood;
    [SerializeField] private TextMeshProUGUI ComboCustomer;

    public Action<int> onComboFinished;
    private IGameRules _gameRules;
    private List<ICustomComboManager> comboManagers = new();

    public void RegisterServed(FoodModel food, CustomerClientModel customer)
    {
        var comboInput = new ComboInput
        {
            food = food,
            customer = customer
        };

        foreach (var customComboManager in comboManagers)
        {
            UpdateUi(customComboManager.RegisterServed(comboInput));
        }
    }

    private void UpdateUi(ComboData comboData)
    {
        switch (comboData.comboType)
        {
            case ComboType.Vip or ComboType.Casual or ComboType.Rush:
                ComboCustomer.text = $"Combo: x{comboData.comboSize} de {comboData.comboType}";
                break;
            case ComboType.Breakfast or ComboType.Drink or ComboType.Lunch:
                comboFood.text = $"Combo: x{comboData.comboSize} de {comboData.comboType}";
                break;
            case ComboType.CustomerMatch:
                ComboCustomer.text = $"Match! Combo: x{comboData.comboSize} de {comboData.comboType}";
                break;
            case ComboType.FoodMatch:
                comboFood.text = $"Match! Combo: x{comboData.comboSize} de {comboData.comboType}";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnMatch(ComboData comboData)
    {
        Debug.Log($"Combo Matched! {comboData.comboSize} de {comboData.comboType}");
    }

    private int CalculateReward(int comboSize)
    {
        return foodComboManager.GetReward(comboSize) + customerComboManager.GetReward(comboSize);
    }

    public void Configure(IGameRules gameRules)
    {
        _gameRules = gameRules;
        customerComboManager.Configure(this);
        foodComboManager.Configure(this);

        comboManagers.Add(customerComboManager);
        comboManagers.Add(foodComboManager);

        foreach (var comboManager in comboManagers)
        {
            comboManager.OnMatch += OnMatch;
        }

        pointsText.text = $"Points: {0}";
        comboFood.text = $"Combo: x{0}";
        ComboCustomer.text = $"Acumulado: {0}";
    }
}