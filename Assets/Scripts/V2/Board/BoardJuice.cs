using PrimeTween;
using UnityEngine;

namespace V2.Board
{
    /// <summary>
    /// Servicio de presentacion efimero: SFX procedurales (sin AudioService ni assets),
    /// particulas, texto flotante, shake de camara y pulso del tablero. Lo crea
    /// <see cref="BoardView"/> en runtime y le pasa todos los numeros desde el inspector.
    /// </summary>
    internal sealed class BoardJuice : MonoBehaviour
    {
        private const int SampleRate = 22050;

        private AudioSource _source;
        private AudioClip _swap, _invalid, _match, _cascade, _fall;
        private Font _font;
        private UnityEngine.Camera _camera;
        private Vector3 _cameraHome;
        private float _shake, _shakeDuration, _shakeTime;
        private bool _muted;
        private float _volume = 0.3f;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            _swap = Make("bc_swap", 0.09f, t =>
                (Mathf.Sin(TwoPi * 880f * t) * 0.55f + Noise(t) * 0.15f) * Mathf.Exp(-t * 45f));
            _invalid = Make("bc_invalid", 0.16f, t =>
                (Mathf.Sin(TwoPi * 110f * t) + 0.4f * Mathf.Sin(TwoPi * 55f * t)) * Mathf.Exp(-t * 20f) * 0.7f);
            _match = Make("bc_match", 0.16f, t =>
                Mathf.Sin(TwoPi * (480f * t + 1300f * t * t)) * Mathf.Exp(-t * 26f));
            _cascade = Make("bc_cascade", 0.22f, t =>
                (Mathf.Sin(TwoPi * 1150f * t) + 0.45f * Mathf.Sin(TwoPi * 1725f * t)) * Mathf.Exp(-t * 15f) * 0.8f);
            _fall = Make("bc_fall", 0.12f, t => Noise(t) * 0.18f * Mathf.Exp(-t * 22f));
        }

        public void Configure(UnityEngine.Camera cam, bool muted, float volume)
        {
            _camera = cam;
            _muted = muted;
            _volume = Mathf.Clamp01(volume);
        }

        // ---- Audio ------------------------------------------------------------------

        public void PlaySwap() => Play(_swap, 1f, 0.8f);

        public void PlayInvalid() => Play(_invalid, 0.9f, 1f);

        public void PlayMatch() => Play(_match, 1f, 0.85f);

        public void PlayCascade(int wave) => Play(_cascade, 1f + 0.12f * (wave - 1), 0.75f);

        public void PlayFall() => Play(_fall, 1f, 0.5f);

        private void Play(AudioClip clip, float pitch, float gain)
        {
            if (_muted || _source == null || clip == null) return;
            _source.pitch = pitch;
            _source.PlayOneShot(clip, _volume * gain);
        }

        private static AudioClip Make(string name, float seconds, System.Func<float, float> wave)
        {
            int count = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[count];
            for (int i = 0; i < count; i++) data[i] = wave(i / (float)SampleRate);
            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private const float TwoPi = Mathf.PI * 2f;

        private static float Noise(float t)
        {
            float x = Mathf.Sin(t * 12789f) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }

        // ---- Particulas -------------------------------------------------------------

        public void Burst(Vector3 center, Color color, int count, float size)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Spark");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = DishSprites.Glow();
                sr.sortingOrder = 10;
                sr.color = color;

                float angle = Random.Range(0f, TwoPi);
                var velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * Random.Range(1.4f, 3.2f)
                               + Vector3.up * 0.6f;
                float startScale = Random.Range(0.08f, 0.16f) * size;
                float duration = Random.Range(0.3f, 0.5f);
                go.transform.position = center;
                go.transform.localScale = Vector3.one * startScale;

                Tween.Custom(sr, 0f, 1f, duration, (target, k) =>
                {
                    float time = (float)k * duration;
                    target.transform.position = center + velocity * time + Vector3.down * (4.5f * time * time);
                    target.transform.localScale = Vector3.one * startScale * (1f - 0.65f * (float)k);
                    var tint = color;
                    tint.a = 1f - (float)k;
                    target.color = tint;
                }, Ease.Linear).OnComplete(() => Destroy(go));
            }
        }

        // ---- Texto flotante ---------------------------------------------------------

        public void FloatText(Vector3 position, string text, Color color, float scale)
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) return;

            var go = new GameObject("FloatText");
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var mesh = go.AddComponent<TextMesh>();
            mesh.font = _font;
            mesh.fontSize = 64;
            mesh.characterSize = 0.075f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontStyle = FontStyle.Bold;
            mesh.text = text;
            mesh.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = _font.material;
                mr.sortingOrder = 20;
            }

            Vector3 from = position;
            Vector3 to = position + Vector3.up * 0.6f;
            Tween.Custom(go.transform, 0f, 1f, 0.75f, (tr, k) =>
            {
                float e = (float)k;
                tr.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, e));
                float pop = e < 0.25f ? Mathf.Lerp(0f, 1.25f, e / 0.25f) : Mathf.Lerp(1.25f, 1f, (e - 0.25f) / 0.75f);
                tr.localScale = Vector3.one * (scale * pop);
                var tint = color;
                tint.a = 1f - Mathf.Clamp01((e - 0.45f) / 0.55f);
                mesh.color = tint;
            }, Ease.Linear).OnComplete(() => Destroy(go));
        }

        // ---- Camara y tablero -------------------------------------------------------

        public void Shake(float amount, float duration)
        {
            if (_camera == null || amount <= 0f) return;
            if (_shakeTime <= 0f) _cameraHome = _camera.transform.position;
            _shake = Mathf.Max(_shake, amount);
            _shakeDuration = Mathf.Max(_shakeDuration, duration);
            _shakeTime = _shakeDuration;
        }

        public void PulseBoard(Transform board, float factor, float duration)
        {
            Tween.StopAll(board);
            Sequence.Create()
                .Chain(Tween.Scale(board, Vector3.one * factor, duration * 0.35f, Ease.OutQuad))
                .Chain(Tween.Scale(board, Vector3.one, duration * 0.65f, Ease.InOutQuad));
        }

        private void LateUpdate()
        {
            if (_camera == null || _shakeTime <= 0f) return;

            _shakeTime -= Time.deltaTime;
            if (_shakeTime <= 0f)
            {
                _camera.transform.position = _cameraHome;
                _shake = 0f;
                _shakeDuration = 0f;
                return;
            }

            float falloff = Mathf.Clamp01(_shakeTime / Mathf.Max(0.0001f, _shakeDuration));
            Vector2 offset = Random.insideUnitCircle * (_shake * falloff);
            _camera.transform.position = _cameraHome + new Vector3(offset.x, offset.y, 0f);
        }
    }
}
