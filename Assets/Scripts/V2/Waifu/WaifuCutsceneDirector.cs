using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using V2.Board;
using V2.Helpers;
using V2.Waifu.Animation;

namespace V2.Waifu
{
    /// <summary>
    /// Dos cinemáticas sobre la misma escena: <b>Ready</b> (una vez por waifu, overlay
    /// oscuro de 2 s, corta el input) y <b>Cast</b> (cada skill ejecutada con éxito, flash
    /// translúcido del color del elemento de 0.7 s que deja ver el tablero resolviendo
    /// debajo). El cast no interrumpe un Ready en curso: si ya corre, el pedido se ignora.
    /// C dispara el slot de debug aunque ya se haya visto.
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

        [Header("Cast (skill ejecutada)")]
        [Tooltip("Duracion total del flash de skill (s)")]
        [SerializeField] private float castDuration = 0.7f;
        [Tooltip("Alpha maximo del flash del color del elemento (no es un blackout)")]
        [SerializeField, Range(0.05f, 0.6f)] private float castAlpha = 0.34f;
        [Tooltip("Desde que costado entra el actor, en unidades de diseno")]
        [SerializeField] private float castEnterFromX = 2.6f;
        [Tooltip("Escala inicial del actor antes de asentar (1 = escala final)")]
        [SerializeField] private float castScaleFrom = 0.8f;
        [Tooltip("Escala final del actor: mas chico que el Ready para no tapar el tablero")]
        [SerializeField] private float castActorScale = 0.72f;
        [Tooltip("Cuanto del color del elemento se mezcla al texto (0 = blanco)")]
        [SerializeField, Range(0f, 1f)] private float castTextColorFactor = 0.85f;

        [Header("Debug")]
        [SerializeField] private Key debugKey = Key.C;
        [SerializeField] private int debugSlot;
        private const int OrderBackdrop = 500;
        private const int OrderActor = 502;
        private const int OrderFlash = 503;
        private const int OrderText = 520;
        private const int OrderCastFlash = 300;
        private const float TextHeightPerUnit = 3.5f;
        private static readonly Vector3 CastHome = new Vector3(0f, -0.35f, 0f);

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
            if (session == null) session = FindFirstObjectByType<BoardSession>();
            if (boardView == null) boardView = FindFirstObjectByType<BoardView>();
            BuildOverlay();
            BuildCastOverlay();
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

            if (_playing || _castPlaying || _pending.Count == 0 || IsBoardBusy()) return;
            int next = _pending[0];
            _pending.RemoveAt(0);
            Activate(next);
        }

        private void LateUpdate()
        {
            if (_playing) FitToView();
            if (_castPlaying) FitCast();
        }

        /// <summary>
        /// Flash corto de skill. Corre en paralelo a la resolucion (no la espera) y toma
        /// GameplayLock mientras dura. Si el Ready esta corriendo, el cast se ignora:
        /// interrumpirlo romperia su feel y el lock anidado. El lock se libera siempre
        /// al cerrar (fin natural, error o destroy).
        /// </summary>
        public void PlayCast(int slot)
        {
            if (_castPlaying || _playing || CutscenePlayer.IsPlaying) return;
            if (_castRoot == null || party == null || party.Runtime == null) return;

            WaifuDefinitionSO def = party.Runtime[slot];
            if (def == null) return;

            Color element = boardView != null ? boardView.DishColor(def.Element) : def.PortraitColor;
            _castPlaying = true;
            GameplayLock.Hold();
            try
            {
                _castWaifu.ApplyIdentity(def.DisplayName, def.Element, element);
                _castWaifu.transform.localPosition = new Vector3(castEnterFromX, CastHome.y, 0f);
                _castWaifu.transform.localScale = Vector3.one * castScaleFrom;
                _castBody = _castBody != null ? _castBody : _castWaifu.transform.Find("Body")
                    .GetComponent<SpriteRenderer>();
                _castBody.color = element;

                if (_castTitle != null)
                {
                    _castTitle.text = def.DisplayName + " · " + SkillLabel(def.SkillArchetype);
                    _castTitle.color = Color.Lerp(Color.white, element, castTextColorFactor);
                }

                Color flash = element;
                flash.a = castAlpha;
                _castFlashRenderer.color = flash;

                _castRoot.gameObject.SetActive(true);
                FitCast();
                _castWaifu.Cast();
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
            float enter = castDuration * 0.32f;
            float fade = castDuration * 0.30f;
            float fadeDelay = Mathf.Max(0f, castDuration - fade);

            _castSeq = Sequence.Create()
                .Group(Tween.LocalPosition(_castWaifu.transform, CastHome, enter, Ease.OutCubic))
                .Group(Tween.Scale(_castWaifu.transform, Vector3.one, enter, Ease.OutBack))
                .Group(Tween.Alpha(_castFlashRenderer, 0f, castDuration, Ease.OutQuad))
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

        /// <summary>Cierra el cast en cualquier salida y libera su Hold exacto.</summary>
        private void FinishCast()
        {
            if (!_castPlaying) return;
            _castPlaying = false;
            if (_castSeq.isAlive) _castSeq.Stop();
            if (_castRoot != null) _castRoot.gameObject.SetActive(false);
            GameplayLock.Release();
        }

        /// <summary>Nombre legible del arquetipo para el texto del cast (sin hardcodear waifus).</summary>
        private static string SkillLabel(SkillArchetype archetype)
        {
            switch (archetype)
            {
                case SkillArchetype.ClearCross: return "Cruz";
                case SkillArchetype.ConvertToElement: return "Conversión";
                case SkillArchetype.InstantServeUnits: return "Servicio directo";
                case SkillArchetype.ReshuffleBoard: return "Barajado";
                case SkillArchetype.DelayEviction: return "Retención";
                default: return archetype.ToString();
            }
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

        /// <summary>
        /// Overlay del cast: flash full-screen del color del elemento (translúcido, muy por
        /// debajo del backdrop del Ready) + un actor chico y un texto. Vive separado del
        /// overlay del Ready para que ambos puedan convivir sin compartir estado.
        /// </summary>
        private void BuildCastOverlay()
        {
            _castRoot = new GameObject("WaifuCastOverlay").transform;
            _castRoot.SetParent(transform, false);

            SpriteRenderer flash = Block(_castRoot, "CastFlash", DishSprites.Solid(),
                new Color(1f, 1f, 1f, 0f), Vector3.one, OrderCastFlash);
            _castFlash = flash.transform;
            _castFlashRenderer = flash;

            _castStage = new GameObject("CastStage").transform;
            _castStage.SetParent(_castRoot, false);
            _castWaifu = BuildWaifu(_castStage);
            _castTitle = Label(_castStage, "CastTitle", "Nombre · Skill", -2.55f, 0.5f, Color.white);

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

        /// <summary>Mismo copiado del view real que el Ready, pero el actor del cast entra mas chico.</summary>
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
                (cam.orthographicSize / Mathf.Max(0.01f, designOrtho) * castActorScale);
        }

        /// <summary>Destroy a mitad del cast: el contador del lock no puede quedar tomado.</summary>
        private void OnDestroy()
        {
            if (_castPlaying) FinishCast();
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
