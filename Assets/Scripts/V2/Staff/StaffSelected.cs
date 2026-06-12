using System.Collections.Generic;
using UnityEngine;

public class StaffSelected : MonoBehaviour
{
    [SerializeField] private List<StaffIdentified> listOfStaffs;

    public StaffIdentified GetNextStaff(int index)
    {
        if (listOfStaffs.Count == 0 || index >= listOfStaffs.Count)
        {
            return StaffIdentified.None;
        }

        return listOfStaffs[index];
    }
}