using UnityEngine;
using V2.Food;

namespace V2.Customer
{
    [CreateAssetMenu(menuName = "Create FoodDataSO", fileName = "FoodDataSO", order = 0)]
    public class FoodDataSO : ScriptableObject
    {
        [SerializeField] private FoodModel food;
        public FoodModel Food => food;
    }
}