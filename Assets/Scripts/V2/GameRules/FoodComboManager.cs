using System;
using UnityEngine;
using V2.Food;

public class FoodComboManager : MonoBehaviour, ICustomComboManager
{
    private IComboManager _comboManager;
    private ComboData currentComboData;
    private FoodModelType currentFood;
    private int countOfCombo;

    public Action<ComboData> OnMatch { get; set; }

    public void Configure(IComboManager comboManager)
    {
        _comboManager = comboManager;
    }

    public int GetReward(int comboSize)
    {
        return 0;
    }

    public ComboData RegisterServed(ComboInput input)
    {
        var food = input.food;

        if (currentFood == FoodModelType.None)
        {
            currentFood = food.foodModelType;
        }

        if (food.foodModelType == currentFood)
        {
            countOfCombo++;
        }
        else
        {
            countOfCombo = 1;
            currentFood = food.foodModelType;
        }

        currentComboData = new ComboData
        {
            comboSize = countOfCombo,
            comboType = GetComboType(currentFood),
            matched = false
        };

        if (countOfCombo >= 3)
        {
            currentComboData.matched = true;

            OnMatch?.Invoke(currentComboData);

            countOfCombo = 0;
            currentFood = FoodModelType.None;
        }

        return currentComboData;
    }

    public ComboData GetComboData()
    {
        return currentComboData;
    }

    private ComboType GetComboType(FoodModelType foodType)
    {
        return foodType switch
        {
            FoodModelType.Almuerzo => ComboType.Lunch,
            FoodModelType.Bebida => ComboType.Drink,
            FoodModelType.Desayuno => ComboType.Breakfast,
            _ => ComboType.None
        };
    }
}