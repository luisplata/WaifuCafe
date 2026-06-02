// Abstracción mínima para vistas/representaciones draggable de Staff (UI o Sprite)
namespace DragAndDrop
{
    public interface IStaffDragView
    {
        // Configura la vista con los datos del staff
        void Configure(Staff.Staff staff, int index, Staff.IStaffMediator staffFront);

        // Habilita / deshabilita la interactividad del drag
        void SetInteractable(bool isInteractable);
    }
}


