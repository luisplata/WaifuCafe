using System.Collections.Generic;
using UnityEngine;
using V2.Staff;

public class StaffSpawnerManager : MonoBehaviour
{
    [SerializeField] private StaffSelected staffSelected;
    [SerializeField] private StaffFactory staffFactory;
    [SerializeField] private StaffClient staffPrefab;
    [SerializeField] private int countOfStaff;
    [SerializeField] private StaffPositions staffPositions;
    [SerializeField] private GameObject spawnPosition;
    private readonly List<StaffClient> _staffClientsInstantiated = new();
    private IGameRules _gameRules;

    public void Configure(IGameRules gameRules)
    {
        _gameRules = gameRules;
        for (int i = 0; i < countOfStaff; i++)
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
}