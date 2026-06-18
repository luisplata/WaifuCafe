using UnityEngine;
using V2.Food;

[System.Serializable]
public class StepsOfRun
{
    public float percente;
    public int spawnPerSecond;
    public bool isRandomCustomer;
    public bool isRandomFood;
    public CustomerIdentify customerIdentify;
    public FoodModelType foodModelType;
    public int countOfCustomer;
    [Range(0f, 1f)] public float specificCustomerProbability = 0.7f;
    [Range(0f, 1f)] public float specificFoodProbability = 0.7f;
}