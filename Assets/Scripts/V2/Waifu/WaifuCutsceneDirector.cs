using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using V2.Board;
using V2.Waifu.Animation;

namespace V2.Waifu
{
    /// <summary>
    /// Conecta la party con la cinematica de activacion dentro del tablero. Construye un
    /// overlay world-space (fondo oscuro + <see cref="WaifuView"/> + textos) por encima de
    /// todo, dispara una vez por waifu cuando queda READY y encola si el tablero esta
    /// resolviendo. La cinematica corta el input mientras corre. C dispara el slot de debug
    /// aunque ya se haya visto.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaifuCutsceneDirector : MonoBehaviour
    {
        [Header("Referencias (si faltan se resuelven en la escena)")]
        [SerializeField] private WaifuPartyView party;
        [SerializeField] private BoardSession session;
        [SerializeField] private BoardView boardView;
        [SerializeField] private RuntimeAnimatorController animatorController;
        [Header("Cinematica")]
        [Tooltip("Duracion de respaldo si no hay PlayableDirector (s)")]
        [SerializeField] private float duration = 2f;
        [Tooltip("Ventana sin skip al arrancar (s)")]
        [SerializeField] private float skipGrace = 0.2f;
        [Tooltip("Ortho de diseno del WaifuView: escala el actor al view real")]
        [SerializeField] private float designOrtho = 5.2f;

        [Header("Debug")]
        [SerializeField] private Key debugKey = Key.C;
        [SerializeField] private int debugSlot;
        private const int OrderBackdrop = 500;
        private const int OrderActor = 502;
        private const int OrderFlash = 503;
        private const int OrderText = 520;
        private const float TextHeightPerUnit = 3.5f;

        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _pending = new List<int>();
        private CutscenePlayer _player;
        private WaifuView _waifu;
        private TextMesh _title;
        private Transform _root;
        private Transform _visual;
        private Transform _backdrop;
        private Transform _stage;
        private bool _playing;

        private void Awake()
        {
            if (party == null) party = FindFirstObjectByType<WaifuPartyView>();
            if (session == null) session = FindFirstObjectByType<BoardSession>();
            if (boardView == null) boardView = FindFirstObjectByType<BoardView>();
            BuildOverlay();
        }

        private void Update()
        {
            if (_player == null) return;

            // Atajo de debug: muestra la activacion del slot 0 sin esperar la carga.
            if (!_playing && Keyboard.current != null && Keyboard.current[debugKey].wasPressedThisFrame)
            {
                _seen.Add(debugSlot);
                if (IsBoardBusy())
                {
                    if (!_pending.Contains(debugSlot)) _pending.Add(debugSlot);
                }
                else Activate(debugSlot);
            }
            // Primera vez READY por waifu en la partida: se encola; el tablero decide cuando sale.
            if (!_playing && party != null && party.Runtime != null)
            {
                WaifuPartyRuntime runtime = party.Runtime;
                for (int slot = 0; slot < runtime.SlotCount; slot++)
                {
                    if (_seen.Contains(slot) || !runtime.IsReady(slot)) continue;
                    _seen.Add(slot);
                    _pending.Add(slot);
                }
            }

            if (_playing || _pending.Count == 0 || IsBoardBusy()) return;
            int next = _pending[0];
            _pending.RemoveAt(0);
            Activate(next);
        }

        private void LateUpdate()
        {
            if (_playing) FitToView();
        }

        /// <summary>Dispara la cinematica del slot con su nombre y color de elemento.</summary>
        public void Activate(int slot)
        {
            if (_playing || _player == null || party == null || party.Runtime == null) return;

            WaifuDefinitionSO def = party.Runtime[slot];
            if (def == null) return;
            _seen.Add(slot);
            _playing = true;

            Color element = boardView != null ? boardView.DishColor(def.Element) : def.PortraitColor;
            _waifu.ApplyIdentity(def.DisplayName, def.Element, element);
            if (_title != null)
            {
                _title.text = "Activación de " + def.DisplayName;
                _title.color = Color.Lerp(Color.white, element, 0.35f);
            }

            _visual.gameObject.SetActive(true);
            FitToView();
            try
            {
                _player.Play();
            }
            finally
            {
                // Si el player no arranco (o fallo), el overlay no puede quedar colgado.
                if (!_player.IsPlayingNow)
                {
                    _playing = false;
                    _visual.gameObject.SetActive(false);
                }
            }
        }

        private bool IsBoardBusy() =>
            (boardView != null && boardView.IsBusy) ||
            (session != null && session.IsResolving);

        private void OnFinished()
        {
            _playing = false;
            if (_visual != null) _visual.gameObject.SetActive(false);
        }

        private void BuildOverlay()
        {
            _root = new GameObject("WaifuCutsceneOverlay").transform;
            _root.SetParent(transform, false);
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(_root, false);

            _backdrop = Block(_visual, "Backdrop", DishSprites.Solid(),
                new Color(0f, 0f, 0f, 0.97f), Vector3.one, OrderBackdrop).transform;
            _backdrop.localPosition = Vector3.zero;
            _stage = new GameObject("Stage").transform;
            _stage.SetParent(_visual, false);

            _waifu = BuildWaifu(_stage);
            _title = Label(_stage, "Title", "Activación de waifu", 3.9f, 0.5f, Color.white);
            TextMesh help = Label(_stage, "Help", "Click o ESC para saltar", -4.35f, 0.3f,
                new Color(1f, 1f, 1f, 0.7f));
            // Sin PlayableDirector: CutscenePlayer usa su duracion de respaldo.
            _player = _root.gameObject.AddComponent<CutscenePlayer>();
            _player.Bind(_waifu, help != null ? help.gameObject : null, duration, skipGrace);
            _player.Finished += OnFinished;

            _visual.gameObject.SetActive(false);
        }

        private WaifuView BuildWaifu(Transform parent)
        {
            var go = new GameObject("WaifuView");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            Block(go.transform, "Body", DishSprites.RoundRect(), new Color(0.38f, 0.68f, 1f),
                new Vector3(2.6f, 3.4f, 1f), OrderActor);
            Block(go.transform, "Flash", DishSprites.RoundRect(), new Color(1f, 1f, 1f, 0f),
                new Vector3(2.7f, 3.5f, 1f), OrderFlash);

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = animatorController;
            go.AddComponent<WaifuAnimationBridge>();
            return go.AddComponent<WaifuView>();
        }

        /// <summary>
        /// El overlay vive en world space: cada frame copia el view real de la camara (que
        /// CameraFit ensancha) en vez de usar un tamano hardcodeado.
        /// </summary>
        private void FitToView()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null || !cam.orthographic) return;

            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            Vector3 center = cam.transform.position;
            center.z = 0f;

            _root.position = center;
            _backdrop.localScale = new Vector3(width * 1.02f, height * 1.02f, 1f);
            _stage.localScale = Vector3.one * (cam.orthographicSize / Mathf.Max(0.01f, designOrtho));
        }

        private static SpriteRenderer Block(Transform parent, string name, Sprite sprite, Color color,
            Vector3 scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private static TextMesh Label(Transform parent, string name, string text, float y, float size,
            Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);

            var mesh = go.AddComponent<TextMesh>();
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.fontSize = 64;
            mesh.characterSize = size / TextHeightPerUnit;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontStyle = FontStyle.Bold;
            mesh.text = text;
            mesh.color = color;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && mesh.font != null)
            {
                renderer.sharedMaterial = mesh.font.material;
                renderer.sortingOrder = OrderText;
            }

            return mesh;
        }
    }
}
