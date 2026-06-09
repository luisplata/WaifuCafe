using System.Collections.Generic;
using UnityEngine;

public class StaffPositions : MonoBehaviour
{
    [SerializeField] private List<StaffPosition> staffPositions;

    public bool GetNextPosition(out StaffPosition staffPosition)
    {
        foreach (var position in staffPositions)
        {
            if (position.IsBusy) continue;
            position.Hold();
            staffPosition = position;
            return true;
        }

        staffPosition = null;
        return false;
    }
}