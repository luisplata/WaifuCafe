using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// Tarjeta de selección de contrato. Muestra título, facción, objetivo,
    /// modificadores y derrota; los contratos bloqueados quedan como preview inerte con
    /// su motivo visible y sin ningún <see cref="Button"/> (Hora del Té / Desayuno Real).
    /// </summary>
    public sealed class ContractCardView
    {
        private const string DefaultLockReason = "PREVIEW BLOQUEADA";

        private static readonly Color CardColor = new Color(0.11f, 0.09f, 0.17f, 0.96f);
        private static readonly Color SelectedColor = new Color(0.20f, 0.16f, 0.28f, 1f);

        private readonly ContractSO _contract;
        private readonly Image _background;
        private readonly Button _button;
        private readonly TextMeshProUGUI _selectLabel;
        private readonly GameObject _featuredBadge;

        public ContractCardView(Transform parent, ContractSO contract, Vector2 topLeft, Vector2 size,
            Action<ContractSO> onSelect)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));

            _contract = contract;

            GameObject root = MetaUi.Panel(parent, "Card_" + contract.Id, CardColor, topLeft, size);
            _background = root.GetComponent<Image>();

            MetaUi.AccentBar(root.transform, new Vector2(0f, 0f), new Vector2(size.x, 5f),
                contract.Locked ? MetaUi.Muted : MetaUi.Accent);

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

            // El badge se crea una sola vez y se togglea desde SetFeatured: el destacado
            // se deriva por sesión, no por refresh.
            _featuredBadge = MetaUi.Label(root.transform, "Featured", "DESTACADO", 12f, MetaUi.Accent,
                TextAlignmentOptions.TopRight, new Vector2(size.x - 104f, 76f), new Vector2(88f, 22f),
                FontStyles.Bold).gameObject;
            _featuredBadge.SetActive(false);

            if (contract.Locked)
            {
                // Sin Button ni raycast de acción: el CTA se reemplaza por el motivo.
                _button = null;
                _selectLabel = null;

                GameObject overlay = MetaUi.Panel(root.transform, "LockOverlay",
                    new Color(0.04f, 0.03f, 0.07f, 0.66f), Vector2.zero, size);
                overlay.GetComponent<Image>().raycastTarget = false;
                overlay.transform.SetAsLastSibling();
                MetaUi.Label(overlay.transform, "Locked", LockReasonText(contract), 13f,
                    MetaUi.Muted, TextAlignmentOptions.Center, new Vector2(16f, size.y - 62f),
                    new Vector2(size.x - 32f, 46f), FontStyles.Bold);
            }
            else
            {
                Button button = MetaUi.Button(root.transform, "Select", "ELEGIR", MetaUi.Accent,
                    new Vector2(16f, size.y - 62f), new Vector2(size.x - 32f, 46f), 18f,
                    out TextMeshProUGUI selectLabel);
                _button = button;
                _selectLabel = selectLabel;
                _button.onClick.AddListener(() => onSelect?.Invoke(_contract));
            }

            SetSelected(false);
        }

        public ContractSO Contract => _contract;

        public void SetSelected(bool selected)
        {
            _background.color = selected ? SelectedColor : CardColor;

            // Locked: no hay CTA que actualizar (y nunca puede ser la seleccionada).
            if (_button == null || _selectLabel == null) return;
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

        private static string LockReasonText(ContractSO contract)
        {
            string reason = contract != null ? contract.LockReason : null;
            return string.IsNullOrWhiteSpace(reason) ? DefaultLockReason : reason.Trim();
        }
    }
}
