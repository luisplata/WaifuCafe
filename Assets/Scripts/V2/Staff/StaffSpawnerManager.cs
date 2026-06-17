using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using V2.Staff;

public class StaffSpawnerManager : MonoBehaviour
{
    [SerializeField] private StaffSelected staffSelected;
    [SerializeField] private StaffFactory staffFactory;
    [SerializeField] private StaffClient staffPrefab;
    [SerializeField] private StaffPositions staffPositions;
    [SerializeField] private GameObject spawnPosition;
    private readonly List<StaffClient> _staffClientsInstantiated = new();
    private IGameRules _gameRules;

    public void Configure(IGameRules gameRules)
    {
        _gameRules = gameRules;
        for (int i = 0; i < staffSelected.GetCountToSelected(); i++)
        {
            try
            {
                var nextStaff = staffSelected.GetNextStaff(i);
                if (staffPositions.GetNextPosition(out StaffPosition staffPosition))
                {
                    var classOfStaff = staffFactory.GetStaffById(nextStaff);
                    var staffInstantiated = Instantiate(staffPrefab,
                        spawnPosition.transform.position, Quaternion.identity);
                    staffInstantiated.Configure(staffPosition, spawnPosition, classOfStaff);
                    _staffClientsInstantiated.Add(staffInstantiated);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error configuring staff spawner: {e.Message}");
            }
        }
    }

    public bool IsPatienceAltered()
    {
        return _staffClientsInstantiated.Any(staffClient =>
            staffClient.GetModel().StaffEspeciality == StaffEspeciality.Patience);
    }

    public float GetAlteredPatience()
    {
        return _staffClientsInstantiated
            .Where(staffClient => staffClient.GetModel().StaffEspeciality == StaffEspeciality.Patience)
            .Sum(staffClient => staffClient.GetModel().GetPatienceAltered());
    }

    public bool IsComboBreaker()
    {
        foreach (var staffClient in _staffClientsInstantiated.Where(staffClient =>
                     staffClient.GetModel().StaffEspeciality == StaffEspeciality.Combo))
        {
            return staffClient.GetModel().IsComboBreaker();
        }

        return true;
    }

    public bool IsEconomyModify()
    {
        return _staffClientsInstantiated.Any(staffClient =>
            staffClient.GetModel().StaffEspeciality == StaffEspeciality.Economy);
    }

    public float GetAlteredEconomy()
    {
        return _staffClientsInstantiated
            .Where(staffClient => staffClient.GetModel().StaffEspeciality == StaffEspeciality.Economy)
            .Sum(staffClient => staffClient.GetModel().GetEconomyAltered());
    }
}