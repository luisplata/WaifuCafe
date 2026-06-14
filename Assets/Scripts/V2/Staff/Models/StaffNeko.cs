using V2.Food;

namespace V2.Staff
{
    public class StaffNeko : StaffModel
    {
        private float discountToBreakfast = 0.2f;

        public StaffNeko()
        {
            name = "Neko";
            staffEspeciality = StaffEspeciality.DrinkSpecialist;
        }

        public override float ModificadorDeTiempoDePreparacion(FoodModel food)
        {
            if (food.foodModelType == FoodModelType.Bebida)
            {
                return discountToBreakfast;
            }

            return 0;
        }
    }
}