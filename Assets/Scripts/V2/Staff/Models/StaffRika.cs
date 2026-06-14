namespace V2.Staff
{
    public class StaffRika : StaffModel
    {
        public StaffRika()
        {
            name = "Rika";
            staffEspeciality = StaffEspeciality.Speed;
        }

        public override float TimeToMove()
        {
            return base.TimeToMove() / 2;
        }
    }
}