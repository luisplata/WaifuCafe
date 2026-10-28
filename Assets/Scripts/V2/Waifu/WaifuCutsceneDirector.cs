using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using V2.Board;
using V2.Waifu.Animation;

namespace V2.Waifu
{
    /// <summary>
    /// Fire = cinemática negra de 2 s skippable; el efecto corre al cerrarla. Ready =
    /// flash ambiental de 0.7 s sin skip ni GameplayLock. Triggers por modo configurables.
    /// Debug: C = cinemática grande; V = flash de READY.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaifuCutsceneDirector : MonoBehaviour
    {
        [Header("Referencias (si faltan se resuelven en la escena)")]
        [SerializeField] private WaifuPartyView party;
        [SerializeField] private BoardView boardView;
        [SerializeField] private RuntimeAnimatorController animatorController;

        [Header("Cinematica de disparo (grande)")]
        [Tooltip("Duracion de respaldo si no hay PlayableDirector (s)")]
        [SerializeField] private float duration = 2f;
        [Tooltip("Ventana sin skip al arrancar (s)")]
        [SerializeField] private float skipGrace = 0.2f;
        [Tooltip("Trigger one-shot del modo grande")]
        [SerializeField] private string bigTrigger = "Activate";
        [Tooltip("Ortho de diseno del WaifuView: escala el actor al view real")]
        [SerializeField] private float designOrtho = 5.2f;

        [Header("Flash de READY (ambiental)")]
        [Tooltip("Duracion total del flash de 'quedo lista' (s)")]
        [SerializeField] private float readyDuration = 0.7f;
        [Tooltip("Alpha maximo del flash del color del elemento (no es un blackout)")]
        [SerializeField, Range(0.05f, 0.6f)] private float readyAlpha = 0.34f;
        [Tooltip("Trigger one-shot del modo rapido")]
        [SerializeField] private string quickTrigger = "Cast";
        [Tooltip("Desde que costado entra el actor, en unidades de diseno")]
        [SerializeField] private float readyEnterFromX = 2.6f;
        [Tooltip("Escala inicial del actor antes de asentar (1 = escala final)")]
        [SerializeField] private float readyScaleFrom = 0.8f;
        [Tooltip("Escala final del actor: chico para no tapar el tablero")]
        [SerializeField] private float readyActorScale = 0.72f;
        [Tooltip("Cuanto del color del elemento se mezcla al texto (0 = blanco)")]
        [SerializeField, Range(0f, 1f)] private float readyTextColorFactor = 0.85f;

        [Header("Debug")]
        [SerializeField] private Key debugKey = Key.C;
        [SerializeField] private Key readyDebugKey = Key.V;
        [SerializeField] private int debugSlot;
        private const int OrderBackdrop = 500;
        private const int OrderActor = 502;
        private const int OrderFlash = 503;
        private const int OrderText = 520;
        private const int OrderCastFlash = 300;
        private const float TextHeightPerUnit = 3.5f;
        private static readonly Vector3 ReadyHome = new Vector3(0f, -0.35f, 0f);

        private System.Action _fireFinished;
        private CutscenePlayer _player;
        private WaifuView _waifu;
        private TextMesh _title;
        private Transform _root;
        private Transform _visual;
        private Transform _backdrop;
        private Transform _stage;
        private bool _playing;
        private string _ultimateTitle = string.Empty;
        private int _ultimateLeaderSlot = -1;
        private bool _ultimateReady;

        private Transform _castRoot;
        private Transform _castStage;
        private Transform _castFlash;
        private SpriteRenderer _castFlashRenderer;
        private SpriteRenderer _castBody;
        private WaifuView _castWaifu;
        private TextMesh _castTitle;
        private Sequence _castSeq;
        private bool _castPlaying;

        private void Awake()
        {
            if (party == null) party = FindFirstObjectByType<WaifuPartyView>();
            if (boardView == null) boardView = FindFirstObjectByType<BoardView>();
            BuildOverlay();
            BuildCastOverlay();
        }

        private void Update()
        {
            if (_player == null) return;

            // Debug: C siempre muestra la cinematica de disparo; V el flash de lista.
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard[debugKey].wasPressedThisFrame) PlayFire(debugSlot);
                if (keyboard[readyDebugKey].wasPressedThisFrame) PlayReady(debugSlot);
            }
        }

        private void LateUpdate()
        {
            if (_playing) FitToView();
            if (_castPlaying) FitCast();
        }

        /// <summary>
        /// Título de la ultimate de facción que castea el líder. Lo inyecta la vista de
        /// party después de construir el runtime (sin Find); si no está lista, la cinemática
        /// sigue mostrando la activa normal.
        /// </summary>
        public void SetUltimate(string title, int leaderSlot, bool ready)
        {
            _ultimateTitle = title ?? string.Empty;
            _ultimateLeaderSlot = leaderSlot;
            _ultimateReady = ready;
        }

        /// <summary>Flash de "quedo lista": 0.7 s, sin skip ni GameplayLock; no bloquea el disparo.</summary>
        public void PlayReady(int slot)
        {
            if (_castPlaying || _playing || CutscenePlayer.IsPlaying) return;
            if (_castRoot == null || party == null || party.Runtime == null) return;

            WaifuDefinitionSO def = party.Runtime[slot];
            if (def == null) return;

            Color element = boardView != null ? boardView.DishColor(def.Element) : def.PortraitColor;
            _castPlaying = true;
            try
            {
                _castWaifu.ApplyIdentity(def.DisplayName, def.Element, element, def.ChibiArt);
                _castWaifu.transform.localPosition = new Vector3(readyEnterFromX, ReadyHome.y, 0f);
                _castWaifu.transform.localScale = Vector3.one * readyScaleFrom;
                _castBody = _castBody != null ? _castBody : _castWaifu.transform.Find("Body")
                    .GetComponent<SpriteRenderer>();
                // Con arte, el chibi conserva sus colores; el flash ya lleva el color del elemento.
                _castBody.color = def.ChibiArt != null ? Color.white : element;

                if (_castTitle != null)
                {
                    _castTitle.text = def.DisplayName + " · LISTA";
                    _castTitle.color = Color.Lerp(Color.white, element, readyTextColorFactor);
                }

                Color flash = element;
                flash.a = readyAlpha;
                _castFlashRenderer.color = flash;

                _castRoot.gameObject.SetActive(true);
                FitCast();
                _castWaifu.PlayClip(quickTrigger);
                StartCastSequence();
            }
            catch
            {
                FinishCast();
                throw;
            }
        }

        private void StartCastSequence()
        {
            float enter = readyDuration * 0.32f;
            float fade = readyDuration * 0.30f;
            float fadeDelay = Mathf.Max(0f, readyDuration - fade);

            _castSeq = Sequence.Create()
                .Group(Tween.LocalPosition(_castWaifu.transform, ReadyHome, enter, Ease.OutCubic))
                .Group(Tween.Scale(_castWaifu.transform, Vector3.one, enter, Ease.OutBack))
                .Group(Tween.Alpha(_castFlashRenderer, 0f, readyDuration, Ease.OutQuad))
                .Group(Tween.Alpha(_castBody, 0f, fade, Ease.InQuad, startDelay: fadeDelay))
                .Group(Tween.Custom(_castTitle, 1f, 0f, fade, FadeTitle, Ease.InQuad,
                    startDelay: fadeDelay))
                .OnComplete(this, _ => FinishCast());
        }

        private static void FadeTitle(TextMesh title, float alpha)
        {
            Color color = title.color;
            color.a = alpha;
            title.color = color;
        }

        /// <summary>Cierra el flash de READY en cualquier salida (no toca el lock).</summary>
        private void FinishCast()
        {
            if (!_castPlaying) return;
            _castPlaying = false;
            if (_castSeq.isAlive) _castSeq.Stop();
            if (_castRoot != null) _castRoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// Cinemática del disparo. El callback corre UNA vez: fin natural, skip o fallo de
        /// arranque. El efecto nunca se ejecuta dentro de la cinemática, sino al cerrarla.
        /// </summary>
        public void PlayFire(int slot, System.Action onFinished = null)
        {
            if (_castPlaying) FinishCast();
            if (_playing || CutscenePlayer.IsPlaying || _player == null ||
                party == null || party.Runtime == null)
            {
                if (onFinished != null) onFinished();
                return;
            }

            WaifuDefinitionSO def = party.Runtime[slot];
            if (def == null)
            {
                if (onFinished != null) onFinished();
                return;
            }

            _fireFinished = onFinished;
            _playing = true;

            Color element = boardView != null ? boardView.DishColor(def.Element) : def.PortraitColor;
            _waifu.ApplyIdentity(def.DisplayName, def.Element, element, def.ChibiArt);
            if (_title != null)
            {
                bool ultimate = _ultimateReady && slot == _ultimateLeaderSlot &&
                                !string.IsNullOrWhiteSpace(_ultimateTitle);
                _title.text = ultimate ? _ultimateTitle : "Activación de " + def.DisplayName;
                _title.color = Color.Lerp(Color.white, element, ultimate ? 0.65f : 0.35f);
            }

            _visual.gameObject.SetActive(true);
            FitToView();
            try
            {
                _player.Play();
            }
            finally
            {
                // Si el player no arranco (o fallo), el overlay no puede quedar colgado
                // ni el efecto comprometido puede perderse.
                if (!_player.IsPlayingNow) CloseFire();
            }
        }

        private void OnFinished() => CloseFire();

        /// <summary>Unico cierre del disparo: apaga el overlay y entrega el callback una vez.</summary>
        private void CloseFire()
        {
            _playing = false;
            if (_visual != null) _visual.gameObject.SetActive(false);
            System.Action finished = _fireFinished;
            _fireFinished = null;
            if (finished != null) finished();
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
            _player.Bind(_waifu, help != null ? help.gameObject : null, duration, skipGrace,
                bigTrigger);
            _player.Finished += OnFinished;

            _visual.gameObject.SetActive(false);
        }

        /// <summary>
        /// Flash de READY (sorting 300, debajo del backdrop 500) con actor chico y texto.
        /// </summary>
        private void BuildCastOverlay()
        {
            _castRoot = new GameObject("WaifuReadyOverlay").transform;
            _castRoot.SetParent(transform, false);

            SpriteRenderer flash = Block(_castRoot, "ReadyFlash", DishSprites.Solid(),
                new Color(1f, 1f, 1f, 0f), Vector3.one, OrderCastFlash);
            _castFlash = flash.transform;
            _castFlashRenderer = flash;

            _castStage = new GameObject("ReadyStage").transform;
            _castStage.SetParent(_castRoot, false);
            _castWaifu = BuildWaifu(_castStage);
            _castTitle = Label(_castStage, "ReadyTitle", "Nombre · LISTA", -2.55f, 0.5f,
                Color.white);

            _castRoot.gameObject.SetActive(false);
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

        /// <summary>El flash copia el view real, con el actor chico para no tapar el tablero.</summary>
        private void FitCast()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null || !cam.orthographic || _castRoot == null) return;

            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            Vector3 center = cam.transform.position;
            center.z = 0f;

            _castRoot.position = center;
            _castFlash.localScale = new Vector3(width * 1.02f, height * 1.02f, 1f);
            _castStage.localScale = Vector3.one *
                (cam.orthographicSize / Mathf.Max(0.01f, designOrtho) * readyActorScale);
        }

        /// <summary>
        /// Destroy: el flash no toca el lock; la cinematica grande lo libera en su propio
        /// CutscenePlayer.OnDestroy. No se ejecuta un efecto a mitad de teardown.
        /// </summary>
        private void OnDestroy()
        {
            if (_castPlaying) FinishCast();
            _fireFinished = null;
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
