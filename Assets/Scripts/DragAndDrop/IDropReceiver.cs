using UnityEngine;

namespace DragAndDrop
{
    // Interfaz para receptores de drop (UI o sprites)
    public interface IDropReceiver
    {
        // Llamado cuando se hace drop. Implementar la reacción.
        void OnDrop(DropPayload payload);

        // Opcional: si el receptor puede rechazar el payload antes de aplicar OnDrop
        bool Accepts(DropPayload payload);
        GameObject GetGameObject();
    }
}


