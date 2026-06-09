using UnityEngine;

public class StaffPosition : MonoBehaviour
{
    private bool isBusy;

    public bool IsBusy => isBusy;

    public void Hold()
    {
        isBusy = true;
    }

    public void Release()
    {
        isBusy = false;
    }
}