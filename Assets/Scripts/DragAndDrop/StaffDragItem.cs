using UnityEngine;
using UnityEngine.EventSystems;
using Staff;

namespace DragAndDrop
{
    /// <summary>
    /// Item draggable de staff para UI. Arrastrar sobre un CustomerDropSlot confirma asignacion.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class StaffDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private int staffIndex = -1;
        [SerializeField] private StaffServiceUIController staffServiceUIController;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Canvas _rootCanvas;
        private Transform _originalParent;
        private Vector2 _originalAnchoredPosition;

        public int StaffIndex => staffIndex;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _rootCanvas = GetComponentInParent<Canvas>();

            if (staffServiceUIController == null)
            {
                staffServiceUIController = FindAnyObjectByType<StaffServiceUIController>();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (staffServiceUIController == null || staffIndex < 0)
            {
                Debug.LogWarning($"[DragAndDrop] OnBeginDrag cancelado: controller={staffServiceUIController != null}, index={staffIndex}");
                return;
            }

            _originalParent = _rectTransform.parent;
            _originalAnchoredPosition = _rectTransform.anchoredPosition;

            // Llevar al root canvas para que el item no quede tapado por masks/layouts.
            if (_rootCanvas != null)
            {
                _rectTransform.SetParent(_rootCanvas.transform, true);
            }

            _canvasGroup.blocksRaycasts = false;
            Debug.Log($"[DragAndDrop] OnBeginDrag: Staff {staffIndex} - blocksRaycasts=false");
            staffServiceUIController.SelectStaff(staffIndex);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_rootCanvas == null)
            {
                _rectTransform.position = eventData.position;
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootCanvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                _rectTransform.localPosition = localPoint;
            }
            
            // Debug: revisar qué está debajo del cursor
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            if (raycastResults.Count > 0)
            {
                string raycastInfo = $"[DragAndDrop] Raycast hits: ";
                foreach (var result in raycastResults)
                {
                    raycastInfo += $"{result.gameObject.name} ";
                }
                Debug.Log(raycastInfo);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            Debug.Log($"[DragAndDrop] OnEndDrag: Staff {staffIndex} - blocksRaycasts=true, pointerDrag={eventData.pointerDrag?.name}");

            if (_originalParent != null)
            {
                _rectTransform.SetParent(_originalParent, true);
                _rectTransform.anchoredPosition = _originalAnchoredPosition;
            }
        }

        public void Configure(Staff.Staff staff, int index, IStaffMediator staffFront)
        {
            staffIndex = index;
        }

        public void SetInteractable(bool isInteractable)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.interactable = isInteractable;
            _canvasGroup.blocksRaycasts = isInteractable;
        }
    }
}


