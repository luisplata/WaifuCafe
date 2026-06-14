using System;
using UnityEngine;
using UnityEngine.EventSystems;
using V2.Staff;

namespace DragAndDrop
{
    // Añadir a GameObjects con SpriteRenderer + Collider2D
    // Requiere Physics2DRaycaster en la Main Camera para recibir eventos IPointer* a través del EventSystem.
    public class DraggableSprite : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler,
        IDragOriginControllerHandle
    {
        public Action OnEnterDrag, OnDragging;
        public Action<IDropReceiver> OnFinishDrag;
        private bool _dragging;
        private Vector3 _startPos;
        private int _pointerId;
        private IDragControllerHandle _IDragControllerHandle;

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
            if (!_IDragControllerHandle.CanUse()) return;
            _dragging = true;
            _pointerId = eventData.pointerId;
            _startPos = transform.position;
            OnEnterDrag?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 screenPos = new Vector3(eventData.position.x, eventData.position.y,
                Mathf.Abs(cam.transform.position.z - transform.position.z));
            Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);
            worldPos.z = transform.position.z;
            transform.position = worldPos;
            OnDragging?.Invoke();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;

            IDropReceiver target;

            var payload = new DropPayload
            {
                origin = this,
                originType = OriginType.Sprite,
                data = gameObject,
                pointerId = eventData.pointerId,
                startWorldPos = _startPos
            };

            if (DragManager.Instance != null)
            {
                target = DragManager.Instance.HandleDropFromWorld(eventData.position, payload);
            }
            else
            {
                // fallback: intentar detectar sprites directamente
                target = TryDirectWorld(eventData.position, payload);
            }

            OnFinishDrag?.Invoke(target);
        }

        private IDropReceiver TryDirectWorld(Vector2 screenPos, DropPayload payload)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return null;
            }

            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y,
                Mathf.Abs(cam.transform.position.z - transform.position.z)));
            Collider2D[] cols = Physics2D.OverlapPointAll(wp);
            foreach (var c in cols)
            {
                if (c.gameObject == gameObject) continue;
                var receiver = c.gameObject.GetComponentInParent<IDropReceiver>();
                if (receiver != null && receiver.Accepts(payload))
                {
                    receiver.OnDrop(payload);
                    return receiver;
                }
            }

            // si nada aceptó: volver a start
            transform.position = payload.startWorldPos;
            return null;
        }

        public void Configure(IDragControllerHandle dragControllerHandle)
        {
            _IDragControllerHandle = dragControllerHandle;
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public IDragControllerHandle GetDragControllerHandle()
        {
            return _IDragControllerHandle;
        }

        public StaffModel GetStaffModel()
        {
            return _IDragControllerHandle.GetStaffModel();
        }
    }
}