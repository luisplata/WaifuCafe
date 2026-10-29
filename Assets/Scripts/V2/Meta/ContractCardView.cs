using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// Tarjeta de contrato jugable. Muestra título, facción, objetivo, modificadores y
    /// derrota, más un pill <c>RECOMENDADO</c> que sugiere sin obligar. Los contratos
    /// locked ya no llegan acá: se telegrafían en una línea del header del menú.
    /// </summary>
    public sealed class ContractCardView
    {
        private static readonly Color CardColor = new Color(0.11f, 0.09f, 0.17f, 0.96f);
        private static readonly Color SelectedColor = new Color(0.20f, 0.16f, 0.28f, 1f);
        private static readonly Color BadgeTextColor = new Color(0.08f, 0.06f, 0.10f, 1f);

        private readonly ContractSO _contract;
        private readonly Image _background;
        private readonly TextMeshProUGUI _selectLabel;
        private readonly GameObject _featuredBadge;

        public ContractCardView(Transform parent, ContractSO contract, Vector2 topLeft, Vector2 size,
            Action<ContractSO> onSelect)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));

            _contract = contract;

            GameObject root = MetaUi.Panel(parent, "Card_" + contract.Id, CardColor, topLeft, size);
            _background = root.GetComponent<Image>();

            MetaUi.AccentBar(root.transform, new Vector2(0f, 0f), new Vector2(size.x, 5f), MetaUi.Accent);

            MetaUi.Label(root.transform, "Title", contract.Title, 24f, MetaUi.Text,
                TextAlignmentOptions.TopLeft, new Vector2(16f, 16f), new Vector2(size.x - 32f, 58f),
                FontStyles.Bold);

            MetaUi.Label(root.transform, "Faction", FactionName(contract.Faction) + " · Contrato", 17f,
                MetaUi.Accent, TextAlignmentOptions.TopLeft, new Vector2(16f, 76f),
                new Vector2(size.x - 32f, 22f));

            MetaUi.Label(root.transform, "Goal", contract.ToDefinition().GoalText, 19f, MetaUi.Text,
                TextAlignmentOptions.TopLeft, new Vector2(16f, 108f), new Vector2(size.x - 32f, 72f));

            MetaUi.Label(root.transform, "Modifiers", contract.ToDefinition().ModifierText, 16f,
                MetaUi.Muted, TextAlignmentOptions.TopLeft, new Vector2(16f, 190f),
                new Vector2(size.x - 32f, 84f));

            MetaUi.Label(root.transform, "Defeat", "Derrota: " + contract.ToDefinition().DefeatText,
                15f, new Color(1f, 0.62f, 0.62f, 0.9f), TextAlignmentOptions.TopLeft,
                new Vector2(16f, 286f), new Vector2(size.x - 32f, 74f));

            // El pill se crea una sola vez y se togglea desde SetFeatured: el recomendado
            // se deriva por sesión, no por refresh. Relleno accent = sugerencia, no orden.
            Image badge = MetaUi.Image(root.transform, "Featured", MetaUi.Accent,
                new Vector2(size.x - 132f, 74f), new Vector2(116f, 22f));
            badge.raycastTarget = false;
            TextMeshProUGUI badgeLabel = MetaUi.Label(badge.transform, "Label", "RECOMENDADO",
                12f, BadgeTextColor, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero,
                FontStyles.Bold);
            MetaUi.Stretch(badgeLabel.rectTransform);
            _featuredBadge = badge.gameObject;
            _featuredBadge.SetActive(false);

            Button button = MetaUi.Button(root.transform, "Select", "ELEGIR", MetaUi.Accent,
                new Vector2(16f, size.y - 62f), new Vector2(size.x - 32f, 46f), 18f,
                out TextMeshProUGUI selectLabel);
            _selectLabel = selectLabel;
            button.onClick.AddListener(() => onSelect?.Invoke(_contract));

            SetSelected(false);
        }

        public ContractSO Contract => _contract;

        public void SetSelected(bool selected)
        {
            _background.color = selected ? SelectedColor : CardColor;

            if (_selectLabel == null) return;
            _selectLabel.text = selected ? "ELEGIDO" : "ELEGIR";
        }

        public void SetFeatured(bool featured)
        {
            if (_featuredBadge == null) return;
            _featuredBadge.SetActive(featured);
        }

        public static string FactionName(WaifuFaction faction)
        {
            switch (faction)
            {
                case WaifuFaction.Cafe: return "Café";
                case WaifuFaction.Miticos: return "Míticos";
                case WaifuFaction.Espectaculo: return "Espectáculo";
                default: return faction.ToString();
            }
        }
    }
}
