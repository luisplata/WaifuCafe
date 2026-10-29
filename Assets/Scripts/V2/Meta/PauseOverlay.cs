using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// Menú de pausa procedural. El HUD en juego conserva solo lo esencial (progreso,
    /// tiempo, monedas, perdidos y ultimate); todo lo descriptivo del contrato —objetivo,
    /// modificadores y condición de derrota— se lee acá como una lista de filas.
    /// Pausar congela el reloj y el tablero vía <see cref="V2.Helpers.GameplayLock"/>.
    /// </summary>
    public sealed class PauseOverlay
    {
        private static readonly Color BackdropColor = new Color(0.02f, 0.02f, 0.04f, 0.82f);
        private static readonly Color CardColor = new Color(0.12f, 0.10f, 0.17f, 0.98f);
        private static readonly Color RowLineColor = new Color(1f, 1f, 1f, 0.07f);
        private static readonly Color AccentColor = new Color(1f, 0.86f, 0.42f, 1f);
        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color MutedColor = new Color(1f, 1f, 1f, 0.62f);

        private const float RowTop = 0.76f;
        private const float RowHeight = 0.076f;

        private readonly Transform _parent;
        private GameObject _panel;
        private GameObject _actionsGroup;
        private GameObject _confirmGroup;

        public PauseOverlay(Transform parent) => _parent = parent;

        public bool IsVisible => _panel != null && _panel.activeSelf;

        /// <summary>Hay una confirmación de abandono abierta.</summary>
        public bool IsConfirming => _confirmGroup != null && _confirmGroup.activeSelf;

        public void Show(RunConfig config, ContractProgress progress, string ultimateText,
            Action onResume, Action onAbandon)
        {
            Hide();
            if (config == null || config.Contract == null) return;

            ContractDefinition contract = config.Contract;

            _panel = new GameObject("PauseOverlay", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_parent, false);
            Stretch(_panel.GetComponent<RectTransform>());
            _panel.GetComponent<Image>().color = BackdropColor;

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_panel.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(900f, 640f);
            cardRect.anchoredPosition = Vector2.zero;
            card.GetComponent<Image>().color = CardColor;

            Label(card.transform, "Title", "PAUSA", 44f, TextAlignmentOptions.Left,
                new Vector2(0f, 0.88f), new Vector2(1f, 0.99f), 40f, 0f, AccentColor);
            Label(card.transform, "Contract", contract.Title, 24f, TextAlignmentOptions.Left,
                new Vector2(0f, 0.78f), new Vector2(1f, 0.87f), 40f, 0f, MutedColor);

            Row(card.transform, 0, "Objetivo", contract.GoalText);
            Row(card.transform, 1, "Modificadores",
                string.IsNullOrWhiteSpace(contract.ModifierText) ? "Sin modificadores" : contract.ModifierText);
            Row(card.transform, 2, "Derrota", DefeatText(contract));
            Row(card.transform, 3, "Progreso", RunHud.ProgressText(contract, progress));
            Row(card.transform, 4, "Tiempo", TimeText(config, progress));
            Row(card.transform, 5, "Monedas", progress.Coins.ToString());
            Row(card.transform, 6, "Perdidos",
                contract.FailEnabled && contract.MaxLostOrders > 0
                    ? progress.LostOrders + "/" + contract.MaxLostOrders
                    : "no aplica");

            string footer = string.IsNullOrWhiteSpace(ultimateText)
                ? "ESC para seguir"
                : ultimateText + "   ·   ESC para seguir";

            _actionsGroup = Group(card.transform, "Actions");
            Label(_actionsGroup.transform, "Footer", footer, 19f, TextAlignmentOptions.Center,
                new Vector2(0f, 0.155f), new Vector2(1f, 0.215f), 0f, 0f, MutedColor);
            BuildButton(_actionsGroup.transform, "ResumeButton", "SEGUIR", new Vector2(-170f, 34f),
                new Color(0.24f, 0.20f, 0.34f, 1f), TextColor, onResume);
            BuildButton(_actionsGroup.transform, "AbandonButton", "SALIR AL MENÚ", new Vector2(170f, 34f),
                new Color(0.30f, 0.14f, 0.18f, 1f), new Color(1f, 0.78f, 0.80f, 1f), ShowConfirm);

            _confirmGroup = Group(card.transform, "Confirm");
            Label(_confirmGroup.transform, "Warning",
                "¿Abandonar la run? Perdés las monedas ganadas.", 20f,
                TextAlignmentOptions.Center, new Vector2(0f, 0.155f), new Vector2(1f, 0.215f),
                40f, 0f, AccentColor);
            BuildButton(_confirmGroup.transform, "ConfirmAbandon", "SÍ, ABANDONAR", new Vector2(-170f, 34f),
                new Color(0.42f, 0.14f, 0.18f, 1f), new Color(1f, 0.82f, 0.84f, 1f), onAbandon);
            BuildButton(_confirmGroup.transform, "CancelAbandon", "CANCELAR", new Vector2(170f, 34f),
                new Color(0.24f, 0.20f, 0.34f, 1f), TextColor, CancelConfirm);
            _confirmGroup.SetActive(false);
        }

        /// <summary>Pasa de las acciones normales al paso de confirmación.</summary>
        private void ShowConfirm()
        {
            if (_actionsGroup != null) _actionsGroup.SetActive(false);
            if (_confirmGroup != null) _confirmGroup.SetActive(true);
        }

        /// <summary>Vuelve a las acciones normales sin abandonar.</summary>
        public void CancelConfirm()
        {
            if (_confirmGroup == null) return;
            _confirmGroup.SetActive(false);
            if (_actionsGroup != null) _actionsGroup.SetActive(true);
        }

        public void Hide()
        {
            if (_panel == null) return;
            UnityEngine.Object.Destroy(_panel);
            _panel = null;
            _actionsGroup = null;
            _confirmGroup = null;
        }

        public void Dispose() => Hide();

        /// <summary>Una fila = clave a la izquierda, valor a la derecha y separador fino.</summary>
        private static void Row(Transform card, int index, string key, string value)
        {
            float top = RowTop - index * RowHeight;
            float bottom = top - RowHeight;

            Label(card, "Key" + index, key, 19f, TextAlignmentOptions.Left,
                new Vector2(0f, bottom), new Vector2(0.30f, top), 40f, 0f, MutedColor);
            Label(card, "Value" + index, value, 20f, TextAlignmentOptions.Right,
                new Vector2(0.30f, bottom), new Vector2(1f, top), 40f, 0f, TextColor);

            var line = new GameObject("Line" + index, typeof(RectTransform), typeof(Image));
            line.transform.SetParent(card, false);
            var lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, bottom);
            lineRect.anchorMax = new Vector2(1f, bottom);
            lineRect.offsetMin = new Vector2(40f, 0f);
            lineRect.offsetMax = new Vector2(-40f, 1f);
            line.GetComponent<Image>().color = RowLineColor;
            line.GetComponent<Image>().raycastTarget = false;
        }

        private static string DefeatText(ContractDefinition contract)
        {
            if (!contract.FailEnabled)
                return "Modo aprendizaje: el tiempo y los perdidos no derrotan";

            if (!string.IsNullOrWhiteSpace(contract.DefeatText))
                return contract.DefeatText;

            return contract.MaxLostOrders > 0
                ? "Perder " + contract.MaxLostOrders + " clientes"
                : "Se acaba el tiempo";
        }

        private static string TimeText(RunConfig config, ContractProgress progress)
        {
            if (!config.FailEnabled)
                return Format(progress.ElapsedSeconds) + " transcurridos";

            float remaining = Mathf.Max(0f, config.DurationSeconds - progress.ElapsedSeconds);
            return Format(remaining) + " restantes";
        }

        private static string Format(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        private static void BuildButton(Transform parent, string name, string text, Vector2 position,
            Color background, Color textColor, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(300f, 56f);
            rect.anchoredPosition = position;

            var image = go.GetComponent<Image>();
            image.color = background;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            Stretch(labelGo.GetComponent<RectTransform>());
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = textColor;
            label.raycastTarget = false;
        }

        private static GameObject Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            return go;
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
