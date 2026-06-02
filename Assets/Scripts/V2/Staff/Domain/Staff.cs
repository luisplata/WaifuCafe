using System;
using StateMachines;

namespace V2.Staff.Domain
{
    [Serializable]
    public class Staff
    {
        public string Name;
        public float ServiceTime = 5f;
        public float AttendedServiceTime = 2f;
        public float MovementTime = 2f;
        public StaffPhase CurrentPhase = StaffPhase.EnEspera;
        public bool IsBusy = false;
        public int Index;
        public string GetCurrentPhaseLabel() => CurrentPhase.ToString();
        public bool CanAttend() => CurrentPhase == StaffPhase.EnEspera && !IsBusy;

        public bool CommandAttend()
        {
            if (!CanAttend()) return false;
            CurrentPhase = StaffPhase.AtendiendoCliente;
            IsBusy = true;
            return true;
        }

        public void StartPhase(StaffPhase newState)
        {
            CurrentPhase = newState;
            if (newState == StaffPhase.EnEspera)
            {
                IsBusy = false;
            }
        }
    }
}