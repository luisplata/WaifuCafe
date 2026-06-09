using UnityEngine;
using V2.Customer;

public class CustomerSpawnerCoroutine : MonoBehaviour
{
    [SerializeField] private CustomerClient customerPrefab;
    [SerializeField] private float timeToSpawn;
    [SerializeField] private int countOfCustomerByStep;
    [SerializeField] private CustomerPositions customerPositions;
    [SerializeField] private GameObject customerSpawnPosition;
    private float localTime;


    private void FixedUpdate()
    {
        localTime += Time.fixedDeltaTime;

        if (localTime >= timeToSpawn)
        {
            SpawnCustomers();
            localTime = 0f;
        }
    }

    private void SpawnCustomers()
    {
        for (int i = 0; i < countOfCustomerByStep; i++)
        {
            if (customerPositions.GetNextSeat(out var seat))
            {
                var customer = Instantiate(customerPrefab, customerSpawnPosition.transform.position,
                    Quaternion.identity);
                customer.Configure(seat, customerSpawnPosition);
            }
            else
            {
                return;
            }
        }
    }
}