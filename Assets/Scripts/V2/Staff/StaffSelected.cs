using System.Collections.Generic;
using UnityEngine;

public class StaffSelected : MonoBehaviour
{
    [SerializeField] private List<StaffNames> listOfStaffsSelected;
    [SerializeField] private List<StaffNames> fallbackStaff = new() { StaffNames.Rika, StaffNames.Rika, StaffNames.Rika };

    private void Awake()
    {
        if (SaveGame.Instance != null && SaveGame.Instance.GetWaifusSelected().Count > 0)
        {
            listOfStaffsSelected = SaveGame.Instance.GetWaifusSelected();
        }
        else
        {
            listOfStaffsSelected = new List<StaffNames>(fallbackStaff);
        }
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