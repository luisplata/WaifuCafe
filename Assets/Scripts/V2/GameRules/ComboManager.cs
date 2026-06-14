using System;
using System.Collections.Generic;
using UnityEngine;
using V2.Customer;
using V2.Food;

public class ComboManager : MonoBehaviour
{
    [SerializeField] private List<ComboResolution> comboResolutions;
    public Action<int> onComboFinished;
    private FoodModelType currentComboType = FoodModelType.None;
    private int currentComboCount;
    private int currentReward;
    private IGameRules _gameRules;

    public (int, FoodModelType, int) RegisterServedFood(FoodModel food, CustomerClientModel customer)
    {
        if (currentComboType == FoodModelType.None)
        {
            currentComboType = food.foodModelType;
        }

        if (currentComboType == food.foodModelType)
        {
            currentComboCount++;
        }
        else
        {
            if (_gameRules.IsComboBreaker())
            {
                FinishCurrentCombo();

                currentComboType = food.foodModelType;
                currentComboCount = 1;
            }
        }

        currentReward += Mathf.RoundToInt(food.pointsToAttend + customer.pointsToAttend);

        Debug.Log($"Combo {food.foodModelType}: x{currentComboCount}");
        return (currentComboCount, currentComboType, CalculateReward(currentComboCount));
    }

    private void FinishCurrentCombo()
    {
        int reward = CalculateReward(currentComboCount);

        currentReward = 0;

        Debug.Log($"Combo terminado x{currentComboCount}. Reward: {reward}");

        onComboFinished?.Invoke(reward);

        // Agregar oro
        // Agregar score
        // Mostrar popup
    }

    private int CalculateReward(int comboSize)
    {
        float multiplier = comboResolutions
            .Find(r => r.count == comboSize)?.multiplier ?? 1f;

        return Mathf.RoundToInt(currentReward * multiplier);
    }

    public void Configure(IGameRules gameRules)
    {
        _gameRules = gameRules;
    }
}