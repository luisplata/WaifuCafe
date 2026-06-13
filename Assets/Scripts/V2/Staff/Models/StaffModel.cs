using System;
using PrimeTween;

[Serializable]
public class StaffModel
{
    public StaffModel()
    {
        timeToIntro = 2f;
    }

    public string name;
    protected float timeToIntro;
    protected StaffEspeciality staffEspeciality;
    public float TimeToIntro => timeToIntro;
}