using UnityEngine;
using UnityEngine.EventSystems;
using WaifuCafe.Core;

namespace V2.Board
{
    /// <summary>
    /// Una celda del tablero en world space. Recibe el input por el EventSystem
    /// (Input System: tap-tap y swipe), nunca por <c>Input.GetMouseButtonDown</c>.
    /// No decide si un swap es valido: solo reenvia el evento a la vista.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class TileView : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private SpriteRenderer _renderer;
        private BoardView _board;
        private Vector3 _baseScale = Vector3.one;

        public BoardPos Pos { get; private set; }

        public void Bind(BoardView board, BoardPos pos, float size)
        {
            _board = board;
            Pos = pos;
            _renderer = GetComponent<SpriteRenderer>();
            _baseScale = Vector3.one * size;
            transform.localScale = _baseScale;
        }

        public void SetDish(Dish dish, Sprite sprite, Color color)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.color = color;
        }

        /// <summary>Marca de seleccion (tap-tap). Solo escala: cero allocations.</summary>
        public void SetSelected(bool selected) => Pulse(selected ? 1.08f : 1f);

        public void Pulse(float factor) => transform.localScale = _baseScale * factor;

        public void OnPointerClick(PointerEventData eventData) => _board.HandleClick(this);

        public void OnBeginDrag(PointerEventData eventData) => _board.HandleBeginDrag(this);

        public void OnDrag(PointerEventData eventData) => _board.HandleDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) => _board.HandleEndDrag();
    }
}
