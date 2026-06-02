using UnityEngine;

namespace Customers.Queue
{
    public class CustomerPosition : MonoBehaviour
    {
        [SerializeField] private bool isBusy;
        public bool IsOccupied()
        {
            return isBusy;
        }

        public void Occupy()
        {
            isBusy = true;
        }
    }
}