using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using V2.Board;
using WaifuCafe.Core;

namespace V2.Cafe
{
    /// <summary>
    /// Banco de sobrantes visible: 5 chips en el HUD de la columna ("Almuerzo x2").
    /// Se alimenta de <see cref="OrderBook"/> y parpadea en rojo si una entrega se
    /// descarto por banco lleno.
    /// </summary>
    public sealed class OverflowBankHud : MonoBehaviour
    {
        private const int Chips = 5;
        private const float ChipY = 7.15f;

        private static readonly Color ChipColor = new Color(0.11f, 0.09f, 0.14f, 0.95f);
        private static readonly Color RejectColor = new Color(0.62f, 0.18f, 0.22f, 1f);

        private readonly SpriteRenderer[] _bg = new SpriteRenderer[Chips];
        private readonly TextMeshPro[] _labels = new TextMeshPro[Chips];
        private readonly Sequence[] _pulse = new Sequence[Chips];
        private int _sign = 1;

        public void Build(int sign)
        {
            _sign = sign;

            for (int i = 0; i < Chips; i++)
            {
                var go = new GameObject("Bank_" + (Dish)i);
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(2.35f, 0.44f, 1f);
                _bg[i] = go.AddComponent<SpriteRenderer>();
                _bg[i].sprite = DishSprites.RoundRect();
                _bg[i].sortingOrder = 2;

                _labels[i] = WorldText.Create(transform, string.Empty, Color.white, 0.24f, 31);
                if (_labels[i] != null) _labels[i].alignment = TextAlignmentOptions.Center;
            }

            Layout();
        }

        public void SetSide(int sign)
        {
            if (_sign == sign) return;
            _sign = sign;
            Layout();
        }

        public void Refresh(OrderBook book)
        {
            if (book == null) return;

            for (int i = 0; i < Chips; i++)
            {
                int units = book.Bank((Dish)i);
                _bg[i].color = units > 0 ? ChipColor : new Color(ChipColor.r, ChipColor.g, ChipColor.b, 0.45f);
                if (_labels[i] == null) continue;

                _labels[i].color = new Color(1f, 1f, 1f, units > 0 ? 0.98f : 0.38f);
                _labels[i].text = $"{(Dish)i} x{units}";
            }
        }

        /// <summary>Feedback del descarte: el chip del plato afectado parpadea y late.</summary>
        public void PlayDiscarded(IReadOnlyDictionary<Dish, int> discarded)
        {
            if (discarded == null) return;

            foreach (KeyValuePair<Dish, int> pair in discarded)
            {
                int i = (int)pair.Key;
                _pulse[i].Stop();
                _pulse[i] = Sequence.Create()
                    .Chain(Tween.Color(_bg[i], RejectColor, 0.08f))
                    .Chain(Tween.Scale(_bg[i].transform, new Vector3(2.45f, 0.50f, 1f), 0.08f, Ease.OutQuad))
                    .Chain(Tween.Scale(_bg[i].transform, new Vector3(2.35f, 0.44f, 1f), 0.14f, Ease.OutBack)
                        .Group(Tween.Color(_bg[i], ChipColor, 0.20f)));
            }
        }

        private void Layout()
        {
            for (int i = 0; i < Chips; i++)
            {
                float x = _sign * (1.9f + i * 2.4f);
                _bg[i].transform.localPosition = new Vector3(x, ChipY, 0f);
                if (_labels[i] != null) _labels[i].transform.localPosition = new Vector3(x, ChipY, 0f);
            }
        }
    }
}
