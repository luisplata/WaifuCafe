using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using V2.Staff;

public class StaffFactory : MonoBehaviour
{
    [SerializeField] private List<StaffClient> staffPrefabs;
    private Dictionary<StaffIdentified, StaffClient> _staffDictionary;

    private void Start()
    {
        _staffDictionary = new Dictionary<StaffIdentified, StaffClient>();
        foreach (var staff in staffPrefabs.Where(staff =>
                     !_staffDictionary.TryAdd(staff.GetModel().staffIdentified, staff)))
        {
            Debug.LogWarning($"Duplicate staff ID found: {staff.GetModel().staffIdentified}. Skipping.");
        }
    }

    public StaffClient GetStaffById(StaffIdentified staffId)
    {
        if (_staffDictionary.TryGetValue(staffId, out var staffPrefab))
        {
            return staffPrefab;
        }

        throw new ArgumentException($"Staff with ID {staffId} not found in the factory.");
    }
}