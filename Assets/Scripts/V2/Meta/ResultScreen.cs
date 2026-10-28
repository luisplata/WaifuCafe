using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// Pantalla de resultado procedural: victoria/derrota, motivo, objetivo con progreso,
    /// monedas conservadas (soft fail), ultimate usada y botón de volver al menú.
    /// </summary>
    public sealed class ResultScreen
    {
        private static readonly Color BackdropColor = new Color(0.02f, 0.02f, 0.04f, 0.78f);
        private static readonly Color CardColor = new Color(0.12f, 0.10f, 0.17f, 0.98f);
        private static readonly Color VictoryColor = new Color(0.55f, 0.90f, 0.62f, 1f);
        private static readonly Color DefeatColor = new Color(0.96f, 0.48f, 0.52f, 1f);
        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color MutedColor = new Color(1f, 1f, 1f, 0.62f);

        private readonly Transform _parent;
        private GameObject _panel;

        public ResultScreen(Transform parent) => _parent = parent;

        public bool IsVisible => _panel != null && _panel.activeSelf;

        public void Show(RunResult result, ContractDefinition contract, Action onBack)
        {
            Hide();
            if (result == null) return;

            _panel = new GameObject("ResultScreen", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_parent, false);
            var root = _panel.GetComponent<RectTransform>();
            Stretch(root);
            _panel.GetComponent<Image>().color = BackdropColor;

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_panel.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(820f, 560f);
            cardRect.anchoredPosition = Vector2.zero;
            card.GetComponent<Image>().color = CardColor;

            Color statusColor = result.IsVictory ? VictoryColor : DefeatColor;
            string titleText = result.IsVictory ? "¡Victoria!" : "Derrota";

            Label(card.transform, "Title", titleText, 56f, TextAlignmentOptions.Center,
                new Vector2(0f, 0.80f), new Vector2(1f, 1f), 0f, 0f, statusColor);
            Label(card.transform, "Contract", result.ContractTitle, 30f, TextAlignmentOptions.Center,
                new Vector2(0f, 0.70f), new Vector2(1f, 0.82f), 0f, 0f, TextColor);
            Label(card.transform, "Reason", ReasonText(result.Outcome), 26f, TextAlignmentOptions.Center,
                new Vector2(0f, 0.60f), new Vector2(1f, 0.71f), 0f, 0f, MutedColor);

            string objective = contract != null ? contract.GoalText : string.Empty;
            Label(card.transform, "Objective", objective, 24f, TextAlignmentOptions.Center,
                new Vector2(0f, 0.50f), new Vector2(1f, 0.61f), 0f, 0f, MutedColor);
            Label(card.transform, "Progress", RunHud.ProgressText(contract, result.Progress), 32f,
                TextAlignmentOptions.Center, new Vector2(0f, 0.39f), new Vector2(1f, 0.51f), 0f, 0f, TextColor);

            Label(card.transform, "Coins", "Monedas ganadas: " + result.Coins, 30f,
                TextAlignmentOptions.Center, new Vector2(0f, 0.28f), new Vector2(1f, 0.39f), 0f, 0f,
                new Color(1f, 0.86f, 0.42f, 1f));

            string ultimate = string.IsNullOrWhiteSpace(result.UltimateUsed)
                ? "Ultimate: ninguna"
                : "Ultimate: " + result.UltimateUsed;
            Label(card.transform, "Ultimate", ultimate, 24f, TextAlignmentOptions.Center,
                new Vector2(0f, 0.19f), new Vector2(1f, 0.29f), 0f, 0f, MutedColor);

            BuildBackButton(card.transform, onBack);
        }

        public void Hide()
        {
            if (_panel == null) return;
            UnityEngine.Object.Destroy(_panel);
            _panel = null;
        }

        public void Dispose() => Hide();

        private static void BuildBackButton(Transform parent, Action onBack)
        {
            var go = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(320f, 66f);
            rect.anchoredPosition = new Vector2(0f, 32f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.24f, 0.20f, 0.34f, 1f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (onBack != null) button.onClick.AddListener(() => onBack());

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            Stretch(labelGo.GetComponent<RectTransform>());
            var text = labelGo.GetComponent<TextMeshProUGUI>();
            text.text = "Volver al menú";
            text.fontSize = 28f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = TextColor;
            text.raycastTarget = false;
        }

        private static string ReasonText(RunOutcome outcome)
        {
            switch (outcome)
            {
                case RunOutcome.GoalReached: return "Objetivo cumplido.";
                case RunOutcome.MaxLostOrders: return "Se perdieron demasiados clientes.";
                case RunOutcome.TimeExpired: return "Se acabó el tiempo sin cumplir el objetivo.";
                default: return string.Empty;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, float horizontalPadding,
            float height, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(horizontalPadding, 0f);
            rect.offsetMax = new Vector2(-horizontalPadding, 0f);
            if (height > 0f) rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }
    }
}
