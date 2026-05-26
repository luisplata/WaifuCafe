using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragAndDrop
{
    // Añadir a elementos UI (Image) que se quieran arrastrar desde Canvas
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private CanvasGroup _cg;
        private GameObject _ghost;
        private Image _sourceImage;
        private Vector2 _ghostSize = Vector2.zero;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            _sourceImage = GetComponent<Image>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_cg != null) { _cg.alpha = 0f; _cg.blocksRaycasts = false; }

            Sprite sprite = null;
            if (_sourceImage != null) sprite = _sourceImage.sprite;

            if (sprite != null)
            {
                _ghost = DragGhostManager.Instance.CreateGhost(sprite, _ghostSize == Vector2.zero ? new Vector2(sprite.rect.width, sprite.rect.height) : _ghostSize);
            }
            else
            {
                _ghost = null;
            }

            if (_ghost != null) DragGhostManager.Instance.SetGhostPosition(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghost != null) DragGhostManager.Instance.SetGhostPosition(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_ghost != null) DragGhostManager.Instance.DestroyGhost();

            var payload = new DropPayload()
            {
                origin = gameObject,
                originType = OriginType.UI,
                data = gameObject,
                pointerId = eventData.pointerId,
                originalParent = transform.parent
            };

            // Delegar a DragManager para resolver el drop (UI o World)
            if (DragManager.Instance != null)
            {
                DragManager.Instance.HandleDropFromUI(eventData.position, payload);
            }
            else
            {
                // Si no hay DragManager, fallback: intentar raycast UI directo
                TryDirectUI(eventData);
            }

            // restaurar visual del origin (DragManager puede haber cambiado)
            if (_cg != null) { _cg.alpha = 1f; _cg.blocksRaycasts = true; }
        }

        private void TryDirectUI(PointerEventData eventData)
        {
            if (EventSystem.current == null) return;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var r in results)
            {
                var receiver = r.gameObject.GetComponentInParent<IDropReceiver>();
                if (receiver != null && receiver.Accepts(new DropPayload { origin = gameObject, originType = OriginType.UI }))
                {
                    receiver.OnDrop(new DropPayload { origin = gameObject, originType = OriginType.UI });
                    return;
                }
            }
        }
    }
}

