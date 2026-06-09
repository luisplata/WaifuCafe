using StateMachines;
using UnityEngine;

namespace V2.Staff
{
    public class StaffStateMachine : MonoBehaviour
    {
        [SerializeField] private StaffPhase staffPhase = StaffPhase.Entrando;

        public bool CanUse()
        {
            return staffPhase == StaffPhase.EnEspera;
        }

        public void SetState(StaffPhase newState)
        {
            staffPhase = newState;
        }
    }
}