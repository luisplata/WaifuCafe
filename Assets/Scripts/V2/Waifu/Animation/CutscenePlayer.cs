using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace V2.Waifu.Animation
{
    /// <summary>
    /// Corre la cinematica de activacion. Usa el PlayableDirector si esta asignado;
    /// si no, cae a WaifuView.Activate() + duracion de inspector. Mientras corre,
    /// InputBlocked queda encendido para que el gameplay consulte el bloqueo.
    /// Click/tap o Escape la cortan y la dejan en Idle.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutscenePlayer : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private WaifuView waifu;
        [SerializeField] private PlayableDirector director;
        [SerializeField] private GameObject helpRoot;

        [Header("Reproduccion")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private float fallbackDuration = 2f;
        [SerializeField] private float skipGrace = 0.2f;

        [Header("Skip (Input System: mouse, touch o tecla)")]
        [SerializeField] private Key skipKey = Key.Escape;

        [Header("Al terminar")]
        [SerializeField] private UnityEvent onFinished = new UnityEvent();

        private static CutscenePlayer _active;

        public static bool IsPlaying => _active != null;
        public static bool InputBlocked => _active != null;
        public bool IsPlayingNow => _active == this;
        public event Action Finished;

        private float _elapsed;

        private void Awake()
        {
            if (waifu == null) waifu = FindFirstObjectByType<WaifuView>();
            if (director == null) director = GetComponent<PlayableDirector>();
        }

        private void OnEnable()
        {
            if (director != null) director.stopped += HandleDirectorStopped;
        }

        private void OnDisable()
        {
            if (director != null) director.stopped -= HandleDirectorStopped;
        }

        private void Start()
        {
            if (playOnStart) Play();
        }

        private void Update()
        {
            if (!IsPlayingNow) return;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < skipGrace || !SkipPressed()) return;
            Skip();
        }

        /// <summary>Arranca la cinematica y bloquea el input de gameplay.</summary>
        public void Play()
        {
            if (IsPlayingNow) return;
            _active = this;
            _elapsed = 0f;
            if (helpRoot != null) helpRoot.SetActive(true);
            if (waifu != null)
            {
                waifu.SetCooldown(false);
                waifu.Activate();
            }

            if (director != null && director.playableAsset != null) director.Play();
            else StartCoroutine(Fallback());
        }

        /// <summary>Corta la cinematica sin esperar al final y la deja en Idle.</summary>
        public void Skip()
        {
            if (!IsPlayingNow) return;
            Finish();
        }

        private IEnumerator Fallback()
        {
            yield return new WaitForSecondsRealtime(fallbackDuration);
            if (IsPlayingNow) Finish();
        }

        private void HandleDirectorStopped(PlayableDirector played)
        {
            if (IsPlayingNow) Finish();
        }

        private void Finish()
        {
            if (!IsPlayingNow) return;
            _active = null;
            StopAllCoroutines();
            if (helpRoot != null) helpRoot.SetActive(false);
            if (director != null && director.state == PlayState.Playing) director.Stop();
            if (waifu != null) waifu.Idle();
            onFinished?.Invoke();
            Finished?.Invoke();
        }

        private void OnDestroy()
        {
            if (IsPlayingNow) _active = null;
        }

        private bool SkipPressed()
        {
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame) return true;
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[skipKey].wasPressedThisFrame;
        }
    }
}
