using V2.Food;

namespace V2.Staff
{
    public class StaffYuki : StaffModel
    {
        private float discountToBreakfast = 0.2f;

        public StaffYuki()
        {
            name = "Yuki";
            timeToIntro = 2f;
            staffEspeciality = StaffEspeciality.BreakfastSpecialist;
        }

        public override float ModificadorDeTiempoDePreparacion(FoodModel food)
        {
            if (food.foodModelType == FoodModelType.Desayuno)
            {
                return discountToBreakfast;
            }

            return 0;
        }
    }
}