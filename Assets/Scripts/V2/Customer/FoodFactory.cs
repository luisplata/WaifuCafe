using UnityEngine;
using V2.Food;

public class FoodFactory : MonoBehaviour
{
    [SerializeField] private FoodModel customerFactory;
    public FoodModel GetFoodByRandom()
    {
        return customerFactory;
    }
}