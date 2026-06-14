using System;
using UnityEngine;
using V2.Food;

[Serializable]
public class StaffModel
{
    public string name;
    protected float timeToIntro;
    protected StaffEspeciality staffEspeciality;
    protected Sprite staffSprite;

    public StaffModel()
    {
        timeToIntro = 2f;
    }

    public Sprite Sprite => staffSprite;

    public StaffEspeciality StaffEspeciality => staffEspeciality;

    public void SetStaffSprite(Sprite newSprite)
    {
        staffSprite = newSprite;
    }

    public virtual float ModificadorDeTiempoDePreparacion(FoodModel food)
    {
        return 0;
    }

    public virtual float TimeToMove()
    {
        return timeToIntro;
    }

    public virtual float GetIncrementOfPoints()
    {
        return 0;
    }

    public virtual float GetPatienceAltered()
    {
        return 0;
    }

    public virtual bool IsComboBreaker()
    {
        return true;
    }

    public virtual float GetEconomyAltered()
    {
        return 0;
    }
}