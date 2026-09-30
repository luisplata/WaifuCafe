using V2.Customer;
using V2.Food;

public interface IGameRules
{
    void CustomerAttended(CustomerClientModel customer, FoodModel food);
    void AddPoints(int points);
    float Percent { get; }
    bool IsPatienceAltered();
    float GetAlteredPatience();
    bool IsComboBreaker();
    float GetAlteredEconomy();
    bool IsEconomyModify();
}