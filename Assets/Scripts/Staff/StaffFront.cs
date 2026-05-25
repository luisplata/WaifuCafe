using UnityEngine;

namespace Staff
{
    public class StaffFront : MonoBehaviour
    {
        [SerializeField] private Staff staff;

        // Devuelve el index (id) del staff
        public int GetIndex() => staff != null ? staff.Index : -1;

        // Indica si el staff puede atender ahora mismo
        public bool CanAttend() => staff != null && staff.CanAttend();

        // Marca al staff como ocupado
        public void MarkBusy()
        {
            if (staff != null) staff.IsBusy = true;
        }

        // Marca al staff como libre
        public void MarkFree()
        {
            if (staff != null) staff.IsBusy = false;
        }

        // Devuelve el tiempo de servicio configurado en el staff
        public float GetServiceTime() => staff != null ? staff.ServiceTime : 0f;
    }
}


