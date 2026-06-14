using V2.Food;

namespace V2.Staff
{
    public class StaffLuna : StaffModel
    {
        private float discountToBreakfast = 0.2f;

        public StaffLuna()
        {
            name = "Luna";
            timeToIntro = 2f;
            staffEspeciality = StaffEspeciality.LunchSpecialist;
        }

        public override float ModificadorDeTiempoDePreparacion(FoodModel food)
        {
            if (food.foodModelType == FoodModelType.Almuerzo)
            {
                return discountToBreakfast;
            }

            return 0;
        }
    }
}