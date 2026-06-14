namespace V2.Staff
{
    public class StaffEmi : StaffModel
    {
        private float aumentoDePropina = 0.5f;

        public StaffEmi()
        {
            name = "Emi";
            staffEspeciality = StaffEspeciality.CasualSpecialist;
        }
        public override float GetIncrementOfPoints()
        {
            return aumentoDePropina;
        }
    }
}