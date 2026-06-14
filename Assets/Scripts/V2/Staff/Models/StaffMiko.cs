namespace V2.Staff
{
    public class StaffMiko : StaffModel
    {
        public StaffMiko()
        {
            name = "Miku";
            staffEspeciality = StaffEspeciality.Patience;
        }

        public override float GetPatienceAltered()
        {
            return 0.5f;
        }
    }
}