using UnityEngine;

namespace DragAndDrop
{
    // Implementar en objetos UI que quieran recibir drops (ej: slots)
    public class DropTargetUI : MonoBehaviour, IDropReceiver
    {
        public bool Accepts(DropPayload payload)
        {
            return true;
        }

        public void OnDrop(DropPayload payload)
        {
            Debug.Log($"DropTargetUI: recibí drop desde {payload.origin.name}");
            // TODO: lógica: añadir item al slot, anim, etc.
        }
    }
}



