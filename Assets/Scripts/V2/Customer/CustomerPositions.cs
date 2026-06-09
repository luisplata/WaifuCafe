using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace V2.Customer
{
    public class CustomerPositions : MonoBehaviour
    {
        [SerializeField] private List<CustomerSeat> customerPosition;

        public bool GetNextSeat(out CustomerSeat unknown)
        {
            foreach (var seat in customerPosition.Where(seat => !seat.IsBusy))
            {
                unknown = seat;
                return true;
            }
            unknown = null;
            return false;
        }
    }
}