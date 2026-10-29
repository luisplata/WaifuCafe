using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace V2.Meta
{
    /// <summary>Datos de una carta del regalo/recompensa (arte, nombre, estrellas).</summary>
    public sealed class GiftCardData
    {
        public Sprite Art;
        public string Name = string.Empty;
        public int Stars = 1;
        public string Subtitle = string.Empty;
    }

    /// <summary>Un grupo por tier (1★/2★/3★) con su color de banner.</summary>
    public sealed class GiftGroup
    {
        public string Title = string.Empty;
        public Color Color = Color.white;
        public GiftCardData[] Cards = Array.Empty<GiftCardData>();
    }

    /// <summary>
    /// Cinemática de regalo por tiers: un grupo de estrellas por pantalla, cartas de waifu
    /// reutilizando el arte del roster, siguiente y saltar. Es sólo presentación: quién
    /// entra al perfil lo decide <see cref="GiftCoordinator"/>.
    /// </summary>
    public sealed class GiftOverlay
    {
        private static readonly Color BackdropColor = new Color(0.02f, 0.02f, 0.05f, 0.96f);
        private static readonly Color CardColor = new Color(0.12f, 0.10f, 0.18f, 0.98f);

        private readonly Transform _parent;
        private GameObject _root;
        private RectTransform _cardsRoot;
        private TextMeshProUGUI _heading;
        private TextMeshProUGUI _tierLabel;
        private TextMeshProUGUI _footer;
        private TextMeshProUGUI _counter;
        private TextMeshProUGUI _nextLabel;
        private IReadOnlyList<GiftGroup> _groups;
        private Action _onFinished;
        private int _index;

        public GiftOverlay(Transform parent) => _parent = parent;

        public bool IsVisible => _root != null;

        public void Play(IReadOnlyList<GiftGroup> groups, string heading, string footer, Action onFinished)
        {
            Hide();

            _groups = groups ?? Array.Empty<GiftGroup>();
            _onFinished = onFinished;
            _index = 0;

            if (_groups.Count == 0)
            {
                Finish();
                return;
            }

            BuildShell(heading, footer);
            ShowGroup(_index);
        }

        public void Hide()
        {
            if (_root == null) return;
            UnityEngine.Object.Destroy(_root);
            _root = null;
            _cardsRoot = null;
            _groups = null;
            _onFinished = null;
        }

        private void BuildShell(string heading, string footer)
        {
            _root = MetaUi.Panel(_parent, "GiftOverlay", BackdropColor, Vector2.zero, new Vector2(1920f, 1080f));
            MetaUi.Stretch(_root.GetComponent<RectTransform>());
            _root.transform.SetAsLastSibling();

            _heading = MetaUi.Label(_root.transform, "Heading", heading, 44f, MetaUi.Accent,
                TextAlignmentOptions.Center, new Vector2(0f, 54f), new Vector2(1920f, 60f),
                FontStyles.Bold);

            _tierLabel = MetaUi.Label(_root.transform, "Tier", string.Empty, 26f, MetaUi.Text,
                TextAlignmentOptions.Center, new Vector2(0f, 126f), new Vector2(1920f, 38f),
                FontStyles.Bold);

            _cardsRoot = new GameObject("Cards", typeof(RectTransform)).GetComponent<RectTransform>();
            _cardsRoot.SetParent(_root.transform, false);
            MetaUi.SetTopLeft(_cardsRoot, new Vector2(0f, 200f), new Vector2(1920f, 360f));

            _footer = MetaUi.Label(_root.transform, "Footer", footer, 22f, MetaUi.Muted,
                TextAlignmentOptions.Center, new Vector2(120f, 640f), new Vector2(1680f, 110f));

            _counter = MetaUi.Label(_root.transform, "Counter", string.Empty, 18f, MetaUi.Muted,
                TextAlignmentOptions.Center, new Vector2(0f, 780f), new Vector2(1920f, 28f));

            Button next = MetaUi.Button(_root.transform, "Next", "SIGUIENTE", MetaUi.Accent,
                new Vector2(760f, 840f), new Vector2(400f, 70f), 26f, out _nextLabel);
            next.onClick.AddListener(Next);

            Button skip = MetaUi.Button(_root.transform, "Skip", "SALTAR", new Color(0.22f, 0.19f, 0.30f, 1f),
                new Vector2(1706f, 40f), new Vector2(170f, 48f), 18f, out _);
            skip.onClick.AddListener(Finish);
        }

        private void ShowGroup(int index)
        {
            if (_cardsRoot == null || _groups == null || index < 0 || index >= _groups.Count) return;

            for (int i = _cardsRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = _cardsRoot.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }

            GiftGroup group = _groups[index];
            _tierLabel.text = group.Title;
            _tierLabel.color = group.Color;
            _counter.text = (index + 1) + " / " + _groups.Count;
            _nextLabel.text = index == _groups.Count - 1 ? "RECLAMAR" : "SIGUIENTE";

            GiftCardData[] cards = group.Cards ?? Array.Empty<GiftCardData>();
            const float cardWidth = 250f;
            const float cardHeight = 340f;
            const float gap = 28f;
            float total = cards.Length * cardWidth + Mathf.Max(0, cards.Length - 1) * gap;
            float startX = (1920f - total) * 0.5f;

            for (int i = 0; i < cards.Length; i++)
            {
                BuildCard(cards[i], group.Color, new Vector2(startX + i * (cardWidth + gap), 10f),
                    new Vector2(cardWidth, cardHeight));
            }
        }

        private void BuildCard(GiftCardData card, Color accent, Vector2 topLeft, Vector2 size)
        {
            GameObject root = MetaUi.Panel(_cardsRoot, "Card", CardColor, topLeft, size);
            MetaUi.AccentBar(root.transform, new Vector2(0f, 0f), new Vector2(size.x, 6f), accent);

            Image art = MetaUi.Image(root.transform, "Art", Color.white, new Vector2(20f, 24f),
                new Vector2(size.x - 40f, size.x - 40f));
            art.preserveAspect = true;
            art.raycastTarget = false;
            if (card.Art != null) art.sprite = card.Art;

            MetaUi.Label(root.transform, "Name", card.Name, 28f, MetaUi.Text,
                TextAlignmentOptions.Center, new Vector2(10f, 240f), new Vector2(size.x - 20f, 34f),
                FontStyles.Bold);

            MetaUi.Label(root.transform, "Stars", card.Stars + "★", 24f, accent,
                TextAlignmentOptions.Center, new Vector2(10f, 276f), new Vector2(size.x - 20f, 30f),
                FontStyles.Bold);

            MetaUi.Label(root.transform, "Subtitle", card.Subtitle, 14f, MetaUi.Muted,
                TextAlignmentOptions.Center, new Vector2(10f, 308f), new Vector2(size.x - 20f, 22f));
        }

        private void Next()
        {
            if (_groups == null) return;

            if (_index >= _groups.Count - 1)
            {
                Finish();
                return;
            }

            _index++;
            ShowGroup(_index);
        }

        private void Finish()
        {
            Action callback = _onFinished;
            Hide();
            callback?.Invoke();
        }
    }
}
