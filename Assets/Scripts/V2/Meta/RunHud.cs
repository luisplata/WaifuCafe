using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WaifuCafe.Core;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// HUD procedural de la run (sin tocar el YAML de la escena): objetivo del contrato,
    /// progreso en vivo, perdidos, monedas y tiempo restante. Vive en un Canvas propio
    /// que también usa el <see cref="ResultScreen"/>.
    /// </summary>
    public sealed class RunHud
    {
        private static readonly Color PanelColor = new Color(0.06f, 0.05f, 0.09f, 0.82f);
        private static readonly Color PillColor = new Color(0.06f, 0.05f, 0.09f, 0.72f);
        private static readonly Color AccentColor = new Color(1f, 0.86f, 0.42f, 1f);
        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color MutedColor = new Color(1f, 1f, 1f, 0.62f);

        private readonly Canvas _canvas;
        private GameObject _panel;
        private GameObject _ultimatePill;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _progress;
        private TextMeshProUGUI _lost;
        private TextMeshProUGUI _ultimate;
        private TextMeshProUGUI _timer;
        private TextMeshProUGUI _coins;
        private string _ultimateText = string.Empty;
        private bool _ultimateReady;

        public RunHud(Transform parent)
        {
            var go = new GameObject("RunUi", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);

            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        public Transform CanvasTransform => _canvas.transform;

        public void Build(RunConfig config)
        {
            Hide();
            if (config == null || config.Contract == null) return;

            _panel = new GameObject("RunHud", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_canvas.transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 96f);
            _panel.GetComponent<Image>().color = PanelColor;

            // Columnas fijas con gutter: nada se solapa aunque el título o el progreso crezcan.
            // El detalle descriptivo del contrato vive en el menú de pausa, no acá.
            _title = Label(_panel.transform, "Title", 24f, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0.27f, 1f),
                new Vector2(24f, 0f), new Vector2(-12f, 0f), AccentColor);
            _title.text = config.Contract.Title;

            _progress = Label(_panel.transform, "Progress", 30f, TextAlignmentOptions.Center,
                new Vector2(0.27f, 0f), new Vector2(0.63f, 1f),
                new Vector2(8f, 0f), new Vector2(-8f, 0f), TextColor);

            _lost = Label(_panel.transform, "Lost", 21f, TextAlignmentOptions.Center,
                new Vector2(0.63f, 0f), new Vector2(0.80f, 1f),
                new Vector2(8f, 0f), new Vector2(-8f, 0f), MutedColor);

            _coins = Label(_panel.transform, "Coins", 26f, TextAlignmentOptions.Right,
                new Vector2(0.80f, 0.55f), new Vector2(1f, 1f),
                new Vector2(0f, 0f), new Vector2(-24f, -10f), AccentColor);

            _timer = Label(_panel.transform, "Timer", 24f, TextAlignmentOptions.Right,
                new Vector2(0.80f, 0f), new Vector2(1f, 0.45f),
                new Vector2(0f, 10f), new Vector2(-24f, 0f), TextColor);

            _ultimatePill = new GameObject("UltimatePill", typeof(RectTransform), typeof(Image));
            _ultimatePill.transform.SetParent(_panel.transform, false);
            var pillRect = _ultimatePill.GetComponent<RectTransform>();
            pillRect.anchorMin = new Vector2(0.5f, 1f);
            pillRect.anchorMax = new Vector2(0.5f, 1f);
            pillRect.pivot = new Vector2(0.5f, 1f);
            pillRect.sizeDelta = new Vector2(620f, 38f);
            pillRect.anchoredPosition = new Vector2(0f, -104f);
            _ultimatePill.GetComponent<Image>().color = PillColor;

            _ultimate = Label(_ultimatePill.transform, "Ultimate", 20f, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, MutedColor);

            ApplyUltimateText();
            Refresh(ContractProgress.Empty, config);
        }

        /// <summary>
        /// Estado de la ultimate en el HUD (badge del gate y, al castearla, "usada"). Se
        /// llama después del <see cref="Build"/>; el HUD no decide reglas.
        /// </summary>
        public void SetUltimate(string text, bool ready)
        {
            _ultimateText = text ?? string.Empty;
            _ultimateReady = ready;
            ApplyUltimateText();
        }

        private void ApplyUltimateText()
        {
            if (_ultimate == null) return;
            _ultimate.text = _ultimateText;
            _ultimate.color = _ultimateReady ? AccentColor : MutedColor;
            if (_ultimatePill != null)
                _ultimatePill.SetActive(!string.IsNullOrWhiteSpace(_ultimateText));
        }

        public void Refresh(ContractProgress progress, RunConfig config)
        {
            if (_panel == null || config == null || config.Contract == null) return;

            _progress.text = ProgressText(config.Contract, progress);
            _coins.text = "Monedas " + progress.Coins;

            if (config.Contract.FailEnabled && config.Contract.MaxLostOrders > 0)
            {
                _lost.gameObject.SetActive(true);
                _lost.text = $"Perdidos {progress.LostOrders}/{config.Contract.MaxLostOrders}";
            }
            else
            {
                _lost.gameObject.SetActive(false);
            }

            float remaining = Mathf.Max(0f, config.DurationSeconds - progress.ElapsedSeconds);
            if (!config.FailEnabled)
            {
                _timer.text = "Tiempo " + Format(progress.ElapsedSeconds);
            }
            else
            {
                int total = Mathf.CeilToInt(remaining);
                _timer.text = "Tiempo " + (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
            }
        }

        public void SetVisible(bool visible)
        {
            if (_panel != null) _panel.SetActive(visible);
        }

        public void Hide() => DisposePanel();

        public void Dispose()
        {
            DisposePanel();
            if (_canvas != null) Object.Destroy(_canvas.gameObject);
        }

        private void DisposePanel()
        {
            if (_panel == null) return;
            Object.Destroy(_panel);
            _panel = null;
            _ultimatePill = null;
        }

        public static string ProgressText(ContractDefinition contract, ContractProgress progress)
        {
            if (contract == null || contract.Goal == null) return string.Empty;

            ContractGoal goal = contract.Goal;
            var parts = new List<string>(4);

            if (goal.servedOrders > 0)
                parts.Add($"Pedidos {progress.ServedOrders}/{goal.servedOrders}");
            if (goal.skillsCast > 0)
                parts.Add($"Skills {progress.SkillsCast}/{goal.skillsCast}");
            if (goal.clearedDishUnits > 0)
                parts.Add($"{DishName(goal.clearedDish)} {progress.Cleared(goal.clearedDish)}/{goal.clearedDishUnits}");
            if (goal.clearedDish2Units > 0)
                parts.Add($"{DishName(goal.clearedDish2)} {progress.Cleared(goal.clearedDish2)}/{goal.clearedDish2Units}");
            if (goal.vipOrders > 0)
                parts.Add($"VIP {progress.VipOrders}/{goal.vipOrders}");

            return string.Join("   ", parts);
        }

        public static string DishName(Dish dish)
        {
            switch (dish)
            {
                case Dish.Desayuno: return "Desayuno";
                case Dish.Almuerzo: return "Almuerzo";
                case Dish.Bebida: return "Bebida";
                case Dish.Postre: return "Postre";
                case Dish.Snack: return "Snack";
                default: return dish.ToString();
            }
        }

        private static string Format(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        private static TextMeshProUGUI Label(Transform parent, string name, float size,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.text = string.Empty;
            text.raycastTarget = false;
            return text;
        }
    }
}
