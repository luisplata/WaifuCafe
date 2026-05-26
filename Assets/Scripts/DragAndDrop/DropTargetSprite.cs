using UnityEngine;

namespace DragAndDrop
{
    // Añadir a sprites (GameObject con SpriteRenderer + Collider2D) que quieran recibir drops
    [RequireComponent(typeof(Collider2D))]
    public class DropTargetSprite : MonoBehaviour, IDropReceiver
    {
        public bool Accepts(DropPayload payload)
        {
            // por defecto aceptar todo
            return true;
        }

        public void OnDrop(DropPayload payload)
        {
            Debug.Log($"DropTargetSprite: recibí drop desde {payload.origin.name} sobre {gameObject.name}");
            // Ejemplo simple: si origin es sprite, posicionar origin en el centro del target
            if (payload.originType == OriginType.Sprite && payload.origin != null)
            {
                payload.origin.transform.position = transform.position;
            }

            // Si origin es UI, podrías instanciar un objeto en el mundo o aplicar lógica de uso
        }
    }
}


