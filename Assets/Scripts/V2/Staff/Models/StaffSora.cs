namespace V2.Staff
{
    public class StaffSora : StaffModel
    {
        private int countOfComboBreakers = 2;

        public StaffSora()
        {
            name = "Sora";
            staffEspeciality = StaffEspeciality.Combo;
        }

        public override bool IsComboBreaker()
        {
            countOfComboBreakers--;
            return countOfComboBreakers > 0;
        }
    }
}