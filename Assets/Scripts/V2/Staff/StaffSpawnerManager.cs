using System.Collections.Generic;
using UnityEngine;
using V2.Staff;

public class StaffSpawnerManager : MonoBehaviour
{
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
            if (staffPositions.GetNextPosition(out StaffPosition staffPosition))
            {
                var staffInstantiated = Instantiate(staffPrefab, spawnPosition.transform.position, Quaternion.identity);
                staffInstantiated.Configure(staffPosition, spawnPosition);
                _staffClientsInstantiated.Add(staffInstantiated);
            }
        }
    }
}