using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragAndDrop
{
    // Singleton que centraliza la resolución de drops (prioridad UI vs world)
    public class DragManager : MonoBehaviour
    {
        public static DragManager Instance { get; private set; }

        [Header("Layers / Masks")] public LayerMask spriteLayerMask = ~0; // por defecto todo

        [Header("Behavior")] public bool uiHasPriority = true; // por defecto: UI sobre World

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Ejecutar un drop originado desde un UI (screen position)
        public IDropReceiver HandleDropFromUI(Vector2 screenPos, DropPayload payload)
        {
            if (uiHasPriority)
            {
                if (TryDropToUI(screenPos, payload, out var dropObject1))
                {
                    return dropObject1;
                }

                if (TryDropToWorld(screenPos, payload, out var dropObject2))
                {
                    return dropObject2;
                }
            }
            else
            {
                if (TryDropToWorld(screenPos, payload, out var dropObject3))
                {
                    return dropObject3;
                }

                if (TryDropToUI(screenPos, payload, out var dropObject4))
                {
                    return dropObject4;
                }
            }

            OnDropRejected(payload);
            return null;
        }

        // Ejecutar un drop originado desde un Sprite (screen position)
        public IDropReceiver HandleDropFromWorld(Vector2 screenPos, DropPayload payload)
        {
            if (uiHasPriority)
            {
                if (TryDropToUI(screenPos, payload, out var dropObject1))
                {
                    return dropObject1;
                }

                if (TryDropToWorld(screenPos, payload, out var dropObject2))
                {
                    return dropObject2;
                }
            }
            else
            {
                if (TryDropToWorld(screenPos, payload, out var dropObject3))
                {
                    return dropObject3;
                }

                if (TryDropToUI(screenPos, payload, out var dropObject4))
                {
                    return dropObject4;
                }
            }

            OnDropRejected(payload);
            return null;
        }

        // Intenta hacer drop sobre UI bajo screenPos. Retorna true si algún target aceptó.
        public bool TryDropToUI(Vector2 screenPos, DropPayload payload, out IDropReceiver target)
        {
            if (EventSystem.current == null)
            {
                target = null;
                return false;
            }

            PointerEventData pd = new PointerEventData(EventSystem.current) { position = screenPos };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pd, results);

            foreach (var r in results)
            {
                if (r.gameObject == payload.origin.GetGameObject()) continue; // ignorar origen

                var receiver = r.gameObject.GetComponentInParent<IDropReceiver>();
                if (receiver != null)
                {
                    if (receiver.Accepts(payload))
                    {
                        receiver.OnDrop(payload);
                        target = receiver;
                        return true;
                    }
                }
            }

            target = null;
            return false;
        }

        // Intenta hacer drop sobre sprites en el mundo. Retorna true si aceptado.
        public bool TryDropToWorld(Vector2 screenPos, DropPayload payload, out IDropReceiver target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                target = null;
                return false;
            }

            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cam.nearClipPlane));
            // OverlapPointAll usa XY para 2D
            Collider2D[] cols = Physics2D.OverlapPointAll(wp, spriteLayerMask);
            if (cols == null || cols.Length == 0)
            {
                target = null;
                return false;
            }

            // Elegir candidato top-most: ordenar por SpriteRenderer.sortingLayer + order
            System.Array.Sort(cols, (a, b) => CompareColliderBySpriteOrder(a, b));

            foreach (var c in cols)
            {
                if (c.gameObject == payload.origin.GetGameObject()) continue;
                var receiver = c.gameObject.GetComponentInParent<IDropReceiver>();
                if (receiver != null)
                {
                    if (receiver.Accepts(payload))
                    {
                        receiver.OnDrop(payload);
                        target = receiver;
                        return true;
                    }
                }
            }

            target = null;
            return false;
        }

        private int CompareColliderBySpriteOrder(Collider2D a, Collider2D b)
        {
            var sa = a.GetComponent<SpriteRenderer>();
            var sb = b.GetComponent<SpriteRenderer>();
            if (sa == null && sb == null) return 0;
            if (sa == null) return 1;
            if (sb == null) return -1;

            // mayor order primero
            if (sa.sortingLayerID != sb.sortingLayerID)
            {
                // no hay fácil comparación de layer id vs id; usar order as fallback
                return sb.sortingOrder.CompareTo(sa.sortingOrder);
            }

            return sb.sortingOrder.CompareTo(sa.sortingOrder);
        }

        private void OnDropRejected(DropPayload payload)
        {
            // Comportamiento por defecto: volver al origen si es UI, o reposicionar al startWorldPos
            if (payload == null) return;

            if (payload.originType == OriginType.UI && payload.origin != null)
            {
                var cg = payload.origin.GetGameObject().GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.blocksRaycasts = true;
                }
            }
            else if (payload.originType == OriginType.Sprite && payload.origin != null)
            {
                payload.origin.GetGameObject().transform.position = payload.startWorldPos;
            }
        }
    }
}