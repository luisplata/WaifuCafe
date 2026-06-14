namespace V2.Staff
{
    public class StaffHana : StaffModel
    {
        private float aumentoDePropina = 0.5f;

        public StaffHana()
        {
            name = "Hana";
            staffEspeciality = StaffEspeciality.RushSpecialist;
        }

        public override float GetIncrementOfPoints()
        {
            return aumentoDePropina;
        }
    }
}