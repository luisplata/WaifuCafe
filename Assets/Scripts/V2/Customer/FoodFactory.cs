using System.Collections.Generic;
using UnityEngine;
using V2.Customer;
using V2.Food;

public class FoodFactory : MonoBehaviour
{
    [SerializeField] private List<FoodDataSO> foods;

    public FoodModel GetFoodByRandom()
    {
        return foods[Random.Range(0, foods.Count)].Food;
    }

    public FoodModel GetFoodByType(FoodModelType currentStepFoodModelType)
    {
        return foods.Find(food => food.Food.foodModelType == currentStepFoodModelType).Food;
    }
}