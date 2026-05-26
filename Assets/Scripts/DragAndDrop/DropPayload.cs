using UnityEngine;

namespace DragAndDrop
{
    public enum OriginType { UI, Sprite }

    // Payload enviado al receptor de drop
    public class DropPayload
    {
        public GameObject origin; // objeto que inició el drag
        public OriginType originType;
        public object data; // referencia libre (item id, SO, etc.)
        public int pointerId;
        public Vector3 startWorldPos;
        public Transform originalParent;
    }
}

