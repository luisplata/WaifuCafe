using DragAndDrop;
using Staff;
using UnityEngine;

namespace V2.Staff.Infra
{
    public class StaffMediatorComponent : MonoBehaviour, IStaffMediator
    {
        [SerializeField] private Domain.Staff staff;
        [SerializeField] private StaffStateComponent staffStateComponent;
        [SerializeField] private DraggableSprite draggableSprite;

        private void Reset()
        {
            staffStateComponent = GetComponentInChildren<StaffStateComponent>();
            draggableSprite = GetComponentInChildren<DraggableSprite>();
        }

        public void Configure(GameObject staffPosition)
        {
            staffStateComponent.Configure(staff);
            transform.position = staffPosition.transform.position;
        }

        public Domain.Staff GetStaff()
        {
            return staff;
        }

        public void MarkBusy()
        {
            staff.IsBusy = true;
        }

        public void MarkFree()
        {
            staff.IsBusy = false;
        }

        public void UpdatePhase(float dt)
        {
        }
    }
}