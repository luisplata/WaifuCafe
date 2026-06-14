namespace V2.Staff
{
    public class StaffAlice : StaffModel
    {
        private float aumentoDePropina = 0.5f;
        public StaffAlice()
        {
            name = "Alice";
            staffEspeciality = StaffEspeciality.VipSpecialist;
        }

        public override float GetIncrementOfPoints()
        {
            return aumentoDePropina;
        }
    }
}