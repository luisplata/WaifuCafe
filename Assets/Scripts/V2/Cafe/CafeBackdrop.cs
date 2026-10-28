using UnityEngine;

namespace V2.Cafe
{
    /// <summary>
    /// Fondo ilustrado del cafe legacy: un SpriteRenderer world-space que cubre el viewport
    /// de la camara y queda por detras de todo el gameplay. Se auto-carga desde Resources
    /// y no intercepta input. Si el arte no esta, el juego sigue con el fondo plano.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CafeBackdrop : MonoBehaviour
    {
        [SerializeField] private string resourcePath = "ArtV3/Backgrounds/Cafe";
        [Tooltip("Multiplicador de brillo: 1 = arte original, 0 = negro.")]
        [SerializeField, Range(0f, 1f)] private float dim = 0.42f;
        [SerializeField] private int sortingOrder = -100;

        private SpriteRenderer _renderer;
        private Transform _root;

        private void Awake()
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null) return;

            var go = new GameObject("CafeBackdrop");
            go.transform.SetParent(transform, false);
            _root = go.transform;

            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.color = new Color(dim, dim, dim, 1f);
            _renderer.sortingOrder = sortingOrder;
        }

        private void LateUpdate() => Fit();

        /// <summary>Cubre el viewport (no deforma): escala por el lado mas restrictivo.</summary>
        private void Fit()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null || !cam.orthographic || _root == null || _renderer == null ||
                _renderer.sprite == null)
                return;

            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            Vector2 size = _renderer.sprite.bounds.size;
            float scale = Mathf.Max(width / Mathf.Max(0.001f, size.x),
                                    height / Mathf.Max(0.001f, size.y));

            Vector3 center = cam.transform.position;
            _root.position = new Vector3(center.x, center.y, 0f);
            _root.localScale = Vector3.one * scale;
        }
    }
}
