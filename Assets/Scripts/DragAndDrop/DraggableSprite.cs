using UnityEngine;
using UnityEngine.EventSystems;

namespace DragAndDrop
{
    // Añadir a GameObjects con SpriteRenderer + Collider2D
    // Requiere Physics2DRaycaster en la Main Camera para recibir eventos IPointer* a través del EventSystem.
    public class DraggableSprite : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        private bool _dragging;
        private Vector3 _startPos;
        private RigidbodyType2D _prevBodyType;
        private int _pointerId;

        private void Awake()
        {
            _startPos = transform.position;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // requerido para algunos pipelines
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _pointerId = eventData.pointerId;
            _startPos = transform.position;
            // si tiene rigidbody, podríamos dejarlo kinematic durante el drag
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                _prevBodyType = rb.bodyType;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y, Mathf.Abs(cam.transform.position.z - transform.position.z));
            Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
            worldPos.z = transform.position.z;
            transform.position = worldPos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = _prevBodyType;
            }

            var payload = new DropPayload
            {
                origin = gameObject,
                originType = OriginType.Sprite,
                data = gameObject,
                pointerId = eventData.pointerId,
                startWorldPos = _startPos
            };

            if (DragManager.Instance != null)
            {
                DragManager.Instance.HandleDropFromWorld(eventData.position, payload);
            }
            else
            {
                // fallback: intentar detectar sprites directamente
                TryDirectWorld(eventData.position, payload);
            }
        }

        private void TryDirectWorld(Vector2 screenPos, DropPayload payload)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z - transform.position.z)));
            Collider2D[] cols = Physics2D.OverlapPointAll(wp);
            foreach (var c in cols)
            {
                if (c.gameObject == gameObject) continue;
                var receiver = c.gameObject.GetComponentInParent<IDropReceiver>();
                if (receiver != null && receiver.Accepts(payload))
                {
                    receiver.OnDrop(payload);
                    return;
                }
            }

            // si nada aceptó: volver a start
            transform.position = payload.startWorldPos;
        }
    }
}


