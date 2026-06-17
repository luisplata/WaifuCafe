using System.Collections.Generic;
using UnityEngine;

public class StaffSelected : MonoBehaviour
{
    [SerializeField] private List<StaffNames> listOfStaffsSelected;

    private void Awake()
    {
        listOfStaffsSelected = SaveGame.Instance.GetWaifusSelected();
    }

    public StaffNames GetNextStaff(int index)
    {
        if (listOfStaffsSelected.Count == 0 || index >= listOfStaffsSelected.Count)
        {
            throw new System.IndexOutOfRangeException($"Index {index} is out of range for the list of staffs.");
        }

        return listOfStaffsSelected[index];
    }

    public int GetCountToSelected()
    {
        return listOfStaffsSelected.Count;
    }
}