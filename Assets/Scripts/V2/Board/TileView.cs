using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using WaifuCafe.Core;

namespace V2.Board
{
    /// <summary>
    /// Una celda del tablero en world space. Todo lo visual (forma, marco redondeado,
    /// sombra y halo) se construye en runtime. No decide si un swap es valido: solo
    /// anima y reenvia el input. Todos los tiempos los dicta <see cref="BoardView"/>.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class TileView : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private SpriteRenderer _renderer;
        private SpriteRenderer _frame;
        private SpriteRenderer _shadow;
        private SpriteRenderer _halo;
        private BoardView _board;
        private Vector3 _baseScale = Vector3.one;
        private Color _color = Color.white;
        private Tween _loop;
        private Sequence _sequence;

        public BoardPos Pos { get; private set; }
        public Vector3 Home { get; private set; }
        public Dish Dish { get; private set; }
        public Color Color => _color;

        public void Bind(BoardView board, BoardPos pos, float size, Vector3 home)
        {
            _board = board;
            Pos = pos;
            Home = home;
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sortingOrder = 2;
            _baseScale = Vector3.one * size;
            transform.position = home;
            transform.localScale = _baseScale;
            BuildDecor();
        }

        /// <summary>Reasigna la celda fisica tras un swap (las fichas viajan con su sprite).</summary>
        public void Rebind(BoardPos pos, Vector3 home)
        {
            Pos = pos;
            Home = home;
            name = $"Tile_{pos.X}_{pos.Y}";
        }

        public void SetDish(Dish dish, Sprite sprite, Color color)
        {
            Dish = dish;
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.color = color;
            _color = color;
            if (_frame != null) _frame.color = new Color(1f, 1f, 1f, 0.34f);
        }

        private void BuildDecor()
        {
            _shadow = NewChild("Shadow", DishSprites.Glow(), new Color(0f, 0f, 0f, 0.32f), 1);
            _shadow.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            _shadow.transform.localScale = new Vector3(0.95f, 0.7f, 1f);

            _halo = NewChild("Halo", DishSprites.Glow(), new Color(1f, 1f, 1f, 0f), 3);
            _halo.transform.localScale = Vector3.one * 1.45f;

            _frame = NewChild("Frame", DishSprites.RoundFrame(), new Color(1f, 1f, 1f, 0.34f), 3);
            _frame.transform.localScale = Vector3.one * 1.01f;
        }

        private SpriteRenderer NewChild(string name, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Deja la ficha limpia en su celda: sin tweens vivos ni restos de animacion.</summary>
        public void PrepareTravel()
        {
            CancelAnimations();
            if (_renderer != null) Tween.StopAll(_renderer);
            _loop = default;
            transform.position = Home;
            transform.localScale = _baseScale;
            _renderer.color = _color;
            _renderer.enabled = true;
            if (_frame != null) _frame.enabled = true;
            if (_shadow != null) _shadow.enabled = true;
            if (_halo != null) _halo.color = new Color(1f, 1f, 1f, 0f);
        }

        private void CancelAnimations()
        {
            _sequence.Stop();
            _sequence = default;
            Tween.StopAll(transform);
        }

        // ---- Seleccion / hint / lift ------------------------------------------------

        public void SetSelected(bool selected, float scale)
        {
            _loop.Stop();
            CancelAnimations();
            if (!selected)
            {
                transform.localScale = _baseScale;
                if (_halo != null) _halo.color = new Color(1f, 1f, 1f, 0f);
                return;
            }

            Tween.Scale(transform, _baseScale * scale, 0.14f, Ease.OutBack);
            if (_halo != null) _loop = Tween.Alpha(_halo, 0.5f, 0.55f, Ease.InOutSine, -1, CycleMode.Yoyo);
        }

        public void SetHint(bool active, float pulse)
        {
            _loop.Stop();
            CancelAnimations();
            if (!active)
            {
                transform.localScale = _baseScale;
                return;
            }

            _loop = Tween.Scale(transform, _baseScale * pulse, 0.5f, Ease.InOutSine, -1, CycleMode.Yoyo);
        }

        public void BeginGrab(float scale)
        {
            CancelAnimations();
            Tween.Scale(transform, _baseScale * scale, 0.1f, Ease.OutBack);
            if (_halo != null) _halo.color = new Color(1f, 1f, 1f, 0.16f);
        }

        public void SetGrabOffset(Vector3 offset) => transform.position = Home + offset;

        public void ReleaseGrab(float time)
        {
            Tween.Scale(transform, _baseScale, time, Ease.OutQuad);
            Tween.Position(transform, Home, time, Ease.OutQuad);
            if (_halo != null) _halo.color = new Color(1f, 1f, 1f, 0f);
        }

        // ---- Animaciones de resolucion ---------------------------------------------

        public void PlaySwap(Vector3 to, float duration, Action done)
        {
            PrepareTravel();
            Tween.Position(transform, to, duration, Ease.OutBack).OnComplete(done);
        }

        public void PlayReject(Vector3 dir, float cell, float duration, float push, float shake, Action done)
        {
            PrepareTravel();
            Vector3 side = new Vector3(-dir.y, dir.x, 0f);
            Tween.Custom(transform, 0f, 1f, duration, (tr, k) =>
            {
                float e = (float)k;
                float offset = e < 0.35f
                    ? Mathf.Lerp(0f, push, e / 0.35f)
                    : e < 0.72f
                        ? Mathf.Lerp(push, -push * 0.5f, (e - 0.35f) / 0.37f)
                        : Mathf.Lerp(-push * 0.5f, 0f, (e - 0.72f) / 0.28f);
                float wobble = Mathf.Sin(e * Mathf.PI * 7f) * (1f - e) * shake;
                tr.position = Home + dir * (cell * offset) + side * (cell * wobble);
            }, Ease.Linear).OnComplete(() =>
            {
                transform.position = Home;
                done?.Invoke();
            });
        }

        public void FlashInvalid()
        {
            Tween.Color(_renderer, new Color(0.95f, 0.34f, 0.40f), 0.07f)
                .OnComplete(() => Tween.Color(_renderer, _color, 0.16f));
        }

        public void PlayPop(float delay, float duration)
        {
            PrepareTravel();
            Sequence sequence = Sequence.Create();
            if (delay > 0f) sequence = sequence.ChainDelay(delay);
            _sequence = sequence
                .ChainCallback(() =>
                {
                    _renderer.color = Color.white;
                    if (_frame != null) _frame.color = new Color(1f, 1f, 1f, 0.9f);
                })
                .Chain(Tween.Scale(transform, _baseScale * 1.15f, duration * 0.4f, Ease.OutQuad))
                .Chain(Tween.Scale(transform, Vector3.zero, duration * 0.6f, Ease.InBack))
                .OnComplete(() =>
                {
                    _renderer.enabled = false;
                    if (_frame != null) _frame.enabled = false;
                    if (_shadow != null) _shadow.enabled = false;
                });
        }

        public void PlayFall(Vector3 from, float duration, float delay, float squash, float squashTime)
        {
            PrepareTravel();
            transform.position = from;
            var squashScale = new Vector3(_baseScale.x * (1f + squash), _baseScale.y * (1f - squash), 1f);
            _sequence = Sequence.Create()
                .Chain(Tween.Position(transform, Home, duration, Ease.InQuad, startDelay: delay))
                .Chain(Tween.Scale(transform, squashScale, squashTime * 0.5f, Ease.OutQuad))
                .Chain(Tween.Scale(transform, _baseScale, squashTime * 0.5f, Ease.OutQuad));
        }

        // ---- Input (EventSystem / Input System) -------------------------------------

        public void OnPointerClick(PointerEventData eventData) => _board.HandleClick(this);

        public void OnBeginDrag(PointerEventData eventData) => _board.HandleBeginDrag(this);

        public void OnDrag(PointerEventData eventData) => _board.HandleDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) => _board.HandleEndDrag();
    }
}
