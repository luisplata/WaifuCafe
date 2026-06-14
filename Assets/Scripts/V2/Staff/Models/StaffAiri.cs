namespace V2.Staff
{
    public class StaffAiri : StaffModel
    {
        private float multiplier = 0.5f;
        public StaffAiri()
        {
            name = "Airi";
            staffEspeciality = StaffEspeciality.Economy;
        }

        public override float GetEconomyAltered()
        {
            return multiplier;
        }
    }
}