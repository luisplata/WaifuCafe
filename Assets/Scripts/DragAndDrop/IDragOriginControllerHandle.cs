using UnityEngine;
using V2.Staff;

namespace DragAndDrop
{
    public interface IDragOriginControllerHandle
    {
        GameObject GetGameObject();
        IDragControllerHandle GetDragControllerHandle();
    }
}