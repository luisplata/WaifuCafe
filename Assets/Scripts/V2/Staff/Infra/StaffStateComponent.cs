using UnityEngine;

namespace V2.Staff.Infra
{
    public class StaffStateComponent : MonoBehaviour
    {
        private Domain.Staff _staff;

        public void Configure(Domain.Staff staff)
        {
            _staff = staff;
        }
    }
}