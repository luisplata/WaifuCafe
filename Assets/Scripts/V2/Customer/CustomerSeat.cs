using UnityEngine;

namespace V2.Customer
{
    public class CustomerSeat : MonoBehaviour
    {
        [SerializeField] private bool isBusy;

        public bool IsBusy => isBusy;

        public void Hold()
        {
            isBusy = true;
        }

        public void Release()
        {
            isBusy = false;
        }
    }
}