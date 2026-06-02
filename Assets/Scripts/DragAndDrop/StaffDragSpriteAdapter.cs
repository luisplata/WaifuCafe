using UnityEngine;
using Staff;

namespace DragAndDrop
{
    // Adapter que expone la misma API que StaffDragItem pero para prefabs de mundo (Sprite)
    // Debe colocarse en la raíz del prefab draggable para que DragManager/DropPayload.origin lo encuentre.
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(DraggableSprite))]
    public class StaffDragSpriteAdapter : MonoBehaviour, IStaffDragView
    {
        [SerializeField] private StaffComponentUi staffComponentUi;

        private int _staffIndex = -1;
        private Staff.Staff _staff;
        private Staff.IStaffMediator _mediator;
        private DraggableSprite _draggable;
        private Collider2D _collider2d;

        private void Awake()
        {
            _draggable = GetComponent<DraggableSprite>();
            _collider2d = GetComponent<Collider2D>();
        }

        public void Configure(Staff.Staff staff, int index, Staff.IStaffMediator staffFront)
        {
            _staff = staff;
            _staffIndex = index;
            _mediator = staffFront;

            if (staffComponentUi != null && _staff != null)
            {
                staffComponentUi.Configure(_staff);
            }

            // Alineación visual inicial si es necesario
            if (_staff != null && _staff.IsBusy)
            {
                SetInteractable(false);
            }
        }

        public void SetInteractable(bool isInteractable)
        {
            if (_collider2d != null) _collider2d.enabled = isInteractable;
            if (_draggable != null) _draggable.enabled = isInteractable;

            if (staffComponentUi != null)
            {
                // Si el UI tiene métodos para controlar interactividad, aplicarlos (opcional)
                // Ej: staffComponentUi.SetInteractable?.Invoke(isInteractable);
            }
        }

        // Exponer helper para obtener el index desde un receiver al recibir el payload
        public int GetStaffIndex() => _staffIndex;
        public Staff.Staff GetStaff() => _staff;
    }
}


