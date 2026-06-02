using DragAndDrop;
using UnityEngine;

namespace Staff
{
    public class StaffFront : MonoBehaviour, IStaffMediator
    {
        [SerializeField] private Staff staff;
        [SerializeField] private StaffDragItem staffDragItem; // legacy UI prefab
        [SerializeField] private GameObject staffDragPrefab; // optional: sprite-based prefab or alternative drag view

        private StaffComponentUi _staffComponentUi;
        private IStaffDragView _staffDragItemInstance;
        private GameObject _staffDragGameObjectInstance;

        // Devuelve el index (id) del staff
        public int GetIndex() => staff != null ? staff.Index : -1;

        // Indica si el staff puede atender ahora mismo
        public bool CanAttend() => staff != null && staff.CanAttend();

        // Marca al staff como ocupado
        public void MarkBusy()
        {
            if (staff != null) staff.IsBusy = true;

            _staffDragItemInstance?.SetInteractable(false);
            if (_staffComponentUi != null)
            {
                _staffComponentUi.StartBusy(GetServiceTime());
            }
        }

        public void UpdateBusyProgress(float elapsedBusyTime)
        {
            _staffComponentUi?.UpdateBusyProgress(elapsedBusyTime);
        }

        public void SyncVisualState()
        {
            if (staff == null) return;
            _staffComponentUi?.SyncFromStaff(staff);
        }

        // Marca al staff como libre
        public void MarkFree()
        {
            if (staff != null) staff.IsBusy = false;

            _staffComponentUi?.FinishBusy();
            _staffDragItemInstance?.SetInteractable(true);
        }

        // Devuelve el tiempo de servicio configurado en el staff
        public float GetServiceTime() => staff != null ? staff.ServiceTime : 0f;
        
        public Staff GetStaff() => staff;

        public void Configure(Transform canvasParent, Staff staffData, int index)
        {
            if (staffData == null) return;

            staff = staffData;

            SyncVisualState();

            _staffDragItemInstance?.SetInteractable(!staffData.IsBusy);
        }
    }
}