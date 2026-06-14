using System;
using System.Collections.Generic;
using UnityEngine;
using V2.Staff;
using V2.Staff.Models;

public class StaffFactory : MonoBehaviour
{
    [SerializeField] private List<StaffModelConfiguration> staffPrefabs;
    private Dictionary<StaffNames, StaffModel> _staffDictionary;

    private void Start()
    {
        _staffDictionary = new Dictionary<StaffNames, StaffModel>();
        foreach (var staffPrefab in staffPrefabs)
        {
            try
            {
                var staffModel = GetModel(staffPrefab.StaffName);
                staffModel.SetStaffSprite(staffPrefab.StaffSprite);
                _staffDictionary[staffPrefab.StaffName] = staffModel;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load StaffModel for prefab: {staffPrefab}");
            }
        }
    }

    private StaffModel GetModel(StaffNames staffPrefab)
    {
        switch (staffPrefab)
        {
            case StaffNames.Airi:
                return new StaffAiri();
            case StaffNames.Yuki:
                return new StaffYuki();
            case StaffNames.Luna:
                return new StaffLuna();
            case StaffNames.Neko:
                return new StaffNeko();
            case StaffNames.Alice:
                return new StaffAlice();
            case StaffNames.Emi:
                return new StaffEmi();
            case StaffNames.Hana:
                return new StaffHana();
            case StaffNames.Rika:
                return new StaffRika();
            case StaffNames.Miko:
                return new StaffMiko();
            case StaffNames.Sora:
                return new StaffSora();
            default:
                throw new ArgumentException($"Staff with ID {staffPrefab} not found in the factory.");
        }
    }

    public StaffModel GetStaffById(StaffNames staffId)
    {
        if (_staffDictionary.TryGetValue(staffId, out var staffPrefab))
        {
            return staffPrefab;
        }

        throw new ArgumentException($"Staff with ID {staffId} not found in the factory.");
    }
}