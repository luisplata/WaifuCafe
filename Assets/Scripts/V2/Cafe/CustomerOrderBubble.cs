using PrimeTween;
using TMPro;
using UnityEngine;
using V2.Board;
using WaifuCafe.Core;

namespace V2.Cafe
{
    /// <summary>
    /// Asiento visible del cafe: fila, iconos de plato y progreso ("Almuerzo x4 · 2/4").
    /// Solo dibuja el estado de un <see cref="Order"/>; no decide reglas ni toca el libro.
    /// El lado del tablero espeja iconos y alineacion del texto.
    /// </summary>
    public sealed class CustomerOrderBubble : MonoBehaviour
    {
        public const float RowWidth = 12.8f;
        public const float RowHeight = 1.7f;

        private const int MaxLines = 2;
        private const float IconScale = 0.8f;

        private static readonly Color RowColor = new Color(0.21f, 0.17f, 0.27f, 0.95f);
        private static readonly Color EmptyColor = new Color(0.21f, 0.17f, 0.27f, 0.22f);
        private static readonly Color DoneColor = new Color(0.30f, 0.55f, 0.38f, 1f);
        private static readonly Color LostColor = new Color(0.62f, 0.24f, 0.28f, 1f);
        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.86f);

        private static readonly Color[] DishColors =
        {
            new Color(0.95f, 0.75f, 0.30f),
            new Color(0.42f, 0.75f, 0.45f),
            new Color(0.32f, 0.62f, 0.92f),
            new Color(0.91f, 0.45f, 0.68f),
            new Color(0.93f, 0.58f, 0.30f),
        };

        private readonly SpriteRenderer[] _icons = new SpriteRenderer[MaxLines];
        private readonly TextMeshPro[] _texts = new TextMeshPro[MaxLines];

        private SpriteRenderer _bg;
        private SpriteRenderer _customer;
        private Tween _enter;
        private Sequence _receive;
        private Sequence _leave;
        private int _sign = 1;
        private Order _order;

        public void Build(int sign)
        {
            _sign = sign;

            _bg = AddSprite("Row", DishSprites.RoundRect(), 1);
            _bg.transform.localScale = new Vector3(RowWidth, RowHeight, 1f);

            _customer = AddSprite("Customer", null, 2);
            _customer.enabled = false;

            for (int i = 0; i < MaxLines; i++)
            {
                _icons[i] = AddSprite("Icon" + i, DishSprites.For(Dish.Almuerzo), 3);
                _icons[i].transform.localScale = Vector3.one * IconScale;
                _texts[i] = WorldText.Create(transform, string.Empty, TextColor, 0.42f, 30);
            }

            Layout();
            Clear();
        }

        /// <summary>Cliente chibi del asiento. Null oculta la figura (fallback sin arte).</summary>
        public void SetCustomer(Sprite sprite)
        {
            if (_customer == null) return;

            _customer.sprite = sprite;
            _customer.enabled = sprite != null;
            _customer.color = Color.white;
            if (sprite != null)
            {
                float scale = RowHeight * 1.15f / Mathf.Max(0.001f, sprite.bounds.size.y);
                _customer.transform.localScale = Vector3.one * scale;
            }
            Layout();
        }

        public void SetSide(int sign)
        {
            if (_sign == sign) return;
            _sign = sign;
            Layout();
        }

        /// <summary>Pedido nuevo: pinta el checklist y hace un punch de entrada.</summary>
        public void Show(Order order)
        {
            _order = order;
            _leave.Stop();
            _bg.color = RowColor;
            Refresh(order);

            _enter.Stop();
            _receive.Stop();
            transform.localScale = Vector3.one * 0.96f;
            _enter = Tween.Scale(transform, Vector3.one, 0.22f, Ease.OutBack);
        }

        public void Refresh(Order order)
        {
            _order = order;
            int count = order == null ? 0 : Mathf.Min(MaxLines, order.Dishes.Count);

            for (int i = 0; i < MaxLines; i++)
            {
                bool on = i < count;
                _icons[i].gameObject.SetActive(on);
                if (_texts[i] != null) _texts[i].gameObject.SetActive(on);
                if (!on) continue;

                Dish dish = order.Dishes[i];
                _icons[i].sprite = DishSprites.For(dish);
                _icons[i].color = DishColors[(int)dish];
                _texts[i].text =
                    $"{dish} x{order.Required(dish)} · {order.Delivered(dish)}/{order.Required(dish)}";
                _texts[i].color = TextColor;
            }

            Layout();
        }

        /// <summary>Llegaron unidades: refresca el contador y pulsa la fila.</summary>
        public void Receive(Dish dish, int units)
        {
            if (_order == null) return;
            Refresh(_order);

            _enter.Stop();
            _receive.Stop();
            _receive = Sequence.Create()
                .Chain(Tween.Scale(transform, Vector3.one * 1.02f, 0.06f, Ease.OutQuad))
                .Chain(Tween.Scale(transform, Vector3.one, 0.12f, Ease.OutBack));
        }

        /// <summary>Pedido completo: flash verde, pop y fade. <see cref="Clear"/> lo resetea despues.</summary>
        public void Complete()
        {
            _leave.Stop();
            _bg.color = DoneColor;
            Sequence leave = Tween.Scale(transform, Vector3.one * 0.85f, 0.30f, Ease.InQuad)
                .Group(Tween.Alpha(_bg, 0f, 0.30f));
            if (_customer != null && _customer.enabled)
                leave = leave.Group(Tween.Alpha(_customer, 0f, 0.30f));

            _leave = Sequence.Create()
                .Chain(Tween.Scale(transform, Vector3.one * 1.04f, 0.10f, Ease.OutQuad))
                .Chain(leave);
        }

        /// <summary>
        /// Cliente perdido (se le acabó la paciencia): tinte rojizo y fade, sin pop verde.
        /// <see cref="Clear"/> resetea el asiento después.
        /// </summary>
        public void Leave()
        {
            _leave.Stop();
            _bg.color = LostColor;
            Sequence leave = Tween.Scale(transform, Vector3.one * 0.88f, 0.28f, Ease.InQuad)
                .Group(Tween.Alpha(_bg, 0f, 0.28f));
            if (_customer != null && _customer.enabled)
                leave = leave.Group(Tween.Alpha(_customer, 0f, 0.28f));

            _leave = Sequence.Create()
                .Chain(Tween.Scale(transform, Vector3.one * 1.03f, 0.08f, Ease.OutQuad))
                .Chain(leave);
        }

        /// <summary>Asiento libre: slot apagado, sin iconos ni texto.</summary>
        public void Clear()
        {
            _order = null;
            _enter.Stop();
            _receive.Stop();
            _leave.Stop();
            transform.localScale = Vector3.one;
            _bg.color = EmptyColor;

            if (_customer != null)
            {
                _customer.enabled = false;
                _customer.color = Color.white;
            }

            for (int i = 0; i < MaxLines; i++)
            {
                _icons[i].gameObject.SetActive(false);
                _icons[i].transform.localScale = Vector3.one * IconScale;
                if (_texts[i] == null) continue;
                _texts[i].gameObject.SetActive(false);
                _texts[i].text = string.Empty;
            }
        }

        private void Layout()
        {
            int count = _order == null ? 0 : Mathf.Min(MaxLines, _order.Dishes.Count);

            if (_customer != null)
                _customer.transform.localPosition = new Vector3(5.35f * _sign, 0f, 0f);

            for (int i = 0; i < MaxLines; i++)
            {
                float y = count > 1 ? (i == 0 ? 0.38f : -0.38f) : 0f;
                _icons[i].transform.localPosition = new Vector3(-5.35f * _sign, y, 0f);
                if (_texts[i] == null) continue;
                _texts[i].transform.localPosition = new Vector3(-2.42f * _sign, y, 0f);
                _texts[i].alignment = _sign > 0 ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            }
        }

        private SpriteRenderer AddSprite(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
