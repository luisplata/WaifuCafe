using V2.Customer;
using V2.Food;

public interface IGameRules
{
    void CustomerAttended(CustomerClientModel customer, FoodModel food);
}