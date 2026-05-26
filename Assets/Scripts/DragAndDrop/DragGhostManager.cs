using UnityEngine;
using UnityEngine.UI;

namespace DragAndDrop
{
    // Crea y mantiene un "ghost" UI que sigue al cursor durante drags originados en UI.
    // No requiere prefab: crea un Canvas/Imagen en runtime.
    public class DragGhostManager : MonoBehaviour
    {
        private static DragGhostManager _instance;
        private Canvas _canvas;
        private GameObject _ghost;
        private Image _ghostImage;

        public static DragGhostManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("_DragGhostManager");
                    _instance = go.AddComponent<DragGhostManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void EnsureCanvasExists()
        {
            if (_canvas != null) return;

            // Buscar Canvas existente llamado DragGhostCanvas
            var existing = GameObject.Find("DragGhostCanvas");
            if (existing != null) { _canvas = existing.GetComponent<Canvas>(); }

            if (_canvas == null)
            {
                var cgo = new GameObject("DragGhostCanvas");
                _canvas = cgo.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cgo.AddComponent<CanvasScaler>();
                cgo.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(cgo);
            }
        }

        public GameObject CreateGhost(Sprite sprite, Vector2 size)
        {
            EnsureCanvasExists();

            if (_ghost == null)
            {
                _ghost = new GameObject("DragGhost");
                _ghost.transform.SetParent(_canvas.transform, false);
                _ghostImage = _ghost.AddComponent<Image>();
                var cg = _ghost.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false; // no bloquear raycasts
            }

            _ghostImage.raycastTarget = false;
            _ghostImage.sprite = sprite;
            if (size.x > 0 && size.y > 0)
            {
                var rt = _ghost.GetComponent<RectTransform>();
                rt.sizeDelta = size;
            }
            _ghost.SetActive(true);
            return _ghost;
        }

        public void SetGhostPosition(Vector2 screenPos)
        {
            if (_ghost == null) return;
            var rt = _ghost.transform as RectTransform;
            if (rt == null) return;
            rt.position = screenPos;
        }

        public void DestroyGhost()
        {
            if (_ghost != null) { _ghost.SetActive(false); }
        }
    }
}


