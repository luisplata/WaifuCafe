using Customers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Staff;
using StateMachines;

namespace DragAndDrop
{
    /// <summary>
    /// Target de drop para customer en UI. Al soltar un StaffDragItem se confirma la asignacion.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class CustomerDropSlot : MonoBehaviour, IDropHandler
    {
        [SerializeField] private int queuePosition = -1;
        [SerializeField] private StaffServiceUIController staffServiceUIController;
        
        private CanvasGroup _canvasGroup;
        private Customer _customer;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            // Añadir CanvasGroup si falta (esto no siempre se aplicó en prefabs existentes)
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                Debug.Log("[DragAndDrop] CanvasGroup añadido dinámicamente a " + gameObject.name);
            }

            // Asegurar que el CanvasGroup pueda recibir raycast para eventos de drop
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            // Asegurar que exista un Graphic (Image) para que el GraphicRaycaster lo detecte.
            var img = GetComponent<Image>();
            if (img == null)
            {
                img = gameObject.AddComponent<Image>();
                // Hacerla invisible pero raycastable
                img.color = new Color(0f, 0f, 0f, 0f);
                img.raycastTarget = true;
                Debug.Log("[DragAndDrop] Image invisible añadido dinámicamente a " + gameObject.name);
            }

            if (staffServiceUIController == null)
            {
                staffServiceUIController = FindAnyObjectByType<StaffServiceUIController>();
            }

            Debug.Log($"[DragAndDrop] CustomerDropSlot Awake: {gameObject.name} - queuePosition={queuePosition} - hasCanvasGroup={_canvasGroup != null} - hasImage={img != null}");

            // Diagnóstico: comprobar si algún CanvasGroup padre está bloqueando raycasts
            Transform ancestor = transform.parent;
            while (ancestor != null)
            {
                var parentCg = ancestor.GetComponent<CanvasGroup>();
                if (parentCg != null && !parentCg.blocksRaycasts)
                {
                    Debug.LogWarning($"[DragAndDrop] CanvasGroup padre bloquea raycasts en ancestor {ancestor.name}. Esto impedirá recibir eventos de drop.");
                }
                ancestor = ancestor.parent;
            }
        }

        public void SetQueuePosition(int position)
        {
            queuePosition = position;
        }

        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log($"[DragAndDrop] OnDrop llamado en CustomerDropSlot (posición en cola: {queuePosition})");
            
            if (staffServiceUIController == null)
            {
                Debug.LogWarning("[DragAndDrop] StaffServiceUIController no asignado");
                return;
            }

            if (queuePosition < 0)
            {
                Debug.LogWarning("[DragAndDrop] QueuePosition no configurado en CustomerDropSlot");
                return;
            }

            if (eventData.pointerDrag == null)
            {
                Debug.LogWarning("[DragAndDrop] No hay pointerDrag en el evento");
                return;
            }

            var draggable = eventData.pointerDrag.GetComponent<StaffDragItem>();
            if (draggable == null)
            {
                Debug.LogWarning($"[DragAndDrop] El objeto dropeado no tiene StaffDragItem: {eventData.pointerDrag.name}");
                return;
            }

            if (_customer != null && _customer.CurrentPhase != CustomerPhase.EsperaPedido)
            {
                Debug.Log($"[DragAndDrop] Customer en estado {_customer.CurrentPhase}; no está listo para asignación.");
                return;
            }

            bool assigned;
            if (_customer != null)
            {
                Debug.Log($"[DragAndDrop] Intentando asignar Staff {draggable.StaffIndex} a Customer directo");
                assigned = staffServiceUIController.TryAssignByCustomer(draggable.StaffIndex, _customer);
            }
            else
            {
                Debug.Log($"[DragAndDrop] Intentando asignar Staff {draggable.StaffIndex} a Customer en posición {queuePosition}");
                assigned = staffServiceUIController.TryAssignByIndices(draggable.StaffIndex, queuePosition);
            }

            if (!assigned)
            {
                Debug.LogWarning($"[DragAndDrop] No se pudo asignar staff {draggable.StaffIndex} a customer en cola {queuePosition}");
            }
            else
            {
                Debug.Log($"[DragAndDrop] ✓ Staff asignado exitosamente");
            }
        }

        public void Configure(Customer customer)
        {
            _customer = customer;
        }
    }
}


