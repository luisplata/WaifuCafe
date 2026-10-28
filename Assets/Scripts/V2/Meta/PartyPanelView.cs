using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using V2.Waifu;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// Panel de party del menú: cuatro slots, resumen de sinergia y badge de ultimate
    /// como placeholders de E4, y botón JUGAR que sólo se habilita con 4 waifus
    /// distintas y poseídas. El guardado de party lo hace <see cref="MetaCoordinator"/>
    /// contra el perfil.
    /// </summary>
    public sealed class PartyPanelView
    {
        private const int SlotCount = 4;
        private const float PanelWidth = 696f;

        private readonly TextMeshProUGUI _title;
        private readonly TextMeshProUGUI _synergy;
        private readonly TextMeshProUGUI _ultimate;
        private readonly TextMeshProUGUI _startLabel;
        private readonly Button _startButton;
        private readonly SynergyTooltip _tooltip;
        private readonly List<SlotWidget> _slots = new List<SlotWidget>(SlotCount);

        public PartyPanelView(Transform panel, Action<string> onRemove)
        {
            _title = MetaUi.Label(panel, "Title", "PARTY (0/4)", 24f, MetaUi.Accent,
                TextAlignmentOptions.TopLeft, new Vector2(16f, 12f),
                new Vector2(PanelWidth - 32f, 34f), FontStyles.Bold);

            for (int i = 0; i < SlotCount; i++)
            {
                float y = 54f + i * 82f;
                GameObject slotRoot = MetaUi.Panel(panel, "Slot" + (i + 1),
                    new Color(0.09f, 0.08f, 0.14f, 0.96f), new Vector2(16f, y),
                    new Vector2(PanelWidth - 32f, 76f));
                _slots.Add(new SlotWidget(slotRoot, i + 1, onRemove));
            }

            _synergy = MetaUi.Label(panel, "SynergySummary", "Sinergia: —", 15f, MetaUi.Muted,
                TextAlignmentOptions.TopLeft, new Vector2(16f, 386f),
                new Vector2(PanelWidth - 32f, 22f));

            _ultimate = MetaUi.Label(panel, "UltimateBadge", "ULTIMATE: —", 15f, MetaUi.Muted,
                TextAlignmentOptions.TopLeft, new Vector2(16f, 410f),
                new Vector2(PanelWidth - 32f, 22f), FontStyles.Bold);
            _ultimate.raycastTarget = true;

            _startButton = MetaUi.Button(panel, "StartButton", "JUGAR", MetaUi.Accent,
                new Vector2(16f, 460f), new Vector2(PanelWidth - 32f, 54f), 24f,
                out _startLabel);
            _startButton.onClick.AddListener(() => StartRequested?.Invoke());

            _tooltip = new SynergyTooltip(panel, new Vector2(16f, 424f),
                new Vector2(PanelWidth - 32f, 104f));
            _tooltip.Bind(_ultimate.gameObject,
                "Sinergia: sumá facciones o roles repetidos.");
        }

        public event Action StartRequested;

        public void Refresh(IReadOnlyList<string> partyIds, IWaifuCatalog catalog, PlayerProfile profile)
        {
            int distinct = partyIds == null ? 0 : partyIds.Count;

            for (int i = 0; i < _slots.Count; i++)
            {
                string id = partyIds != null && i < partyIds.Count ? partyIds[i] : null;
                WaifuDefinitionSO definition = catalog != null ? catalog.Find(id) : null;
                int stars = StarsOf(profile, definition != null ? definition.Id : null);
                _slots[i].Set(definition, stars);
            }

            _title.text = $"PARTY ({Mathf.Clamp(distinct, 0, SlotCount)}/{SlotCount})";
        }

        public void SetStartEnabled(bool enabled, string disabledReason = null)
        {
            _startButton.interactable = enabled;
            _startLabel.text = enabled ? "JUGAR" : (string.IsNullOrWhiteSpace(disabledReason)
                ? "JUGAR"
                : disabledReason);
        }

        /// <summary>Hook para E4-05: resumen real de facciones/estrellas.</summary>
        public void SetSynergySummary(string text)
        {
            _synergy.text = string.IsNullOrWhiteSpace(text) ? "Sinergia: —" : text;
        }

        /// <summary>Hook para E4-05: badge real de ultimate (3/3 + ★3).</summary>
        public void SetUltimateBadge(string text, bool ready)
        {
            _ultimate.text = string.IsNullOrWhiteSpace(text) ? "ULTIMATE: —" : text;
            _ultimate.color = ready ? MetaUi.Accent : MetaUi.Muted;
        }

        /// <summary>Tooltip real de sinergia/ultimate (<see cref="SynergyModel.Tooltip"/>).</summary>
        public void SetUltimateTooltip(string text)
        {
            _tooltip.SetText(text);
        }

        private static int StarsOf(PlayerProfile profile, string id)
        {
            if (profile == null || profile.owned == null || string.IsNullOrWhiteSpace(id)) return 0;

            foreach (StarterEntry entry in profile.owned)
            {
                if (entry != null && string.Equals(entry.waifuId, id, StringComparison.OrdinalIgnoreCase))
                    return entry.stars;
            }

            return 0;
        }

        private sealed class SlotWidget
        {
            private readonly Image _art;
            private readonly TextMeshProUGUI _name;
            private readonly TextMeshProUGUI _stars;
            private readonly Button _remove;
            private string _waifuId;

            public SlotWidget(GameObject root, int index, Action<string> onRemove)
            {
                _art = MetaUi.Image(root.transform, "Art", Color.white, new Vector2(12f, 8f),
                    new Vector2(60f, 60f));
                _art.preserveAspect = true;
                _art.raycastTarget = false;

                _name = MetaUi.Label(root.transform, "Name", "Vacío", 19f, MetaUi.Muted,
                    TextAlignmentOptions.Left, new Vector2(82f, 10f), new Vector2(380f, 24f),
                    FontStyles.Bold);

                _stars = MetaUi.Label(root.transform, "Stars", string.Empty, 16f, MetaUi.Accent,
                    TextAlignmentOptions.Left, new Vector2(82f, 36f), new Vector2(260f, 20f));

                MetaUi.Label(root.transform, "SlotIndex", (index + 1).ToString(), 14f, MetaUi.Muted,
                    TextAlignmentOptions.Center, new Vector2(560f, 26f), new Vector2(28f, 24f));

                _remove = MetaUi.Button(root.transform, "Remove", "X", new Color(0.36f, 0.18f, 0.22f, 1f),
                    new Vector2(592f, 18f), new Vector2(56f, 40f), 20f, out _);
                _remove.onClick.AddListener(() =>
                {
                    if (!string.IsNullOrWhiteSpace(_waifuId)) onRemove?.Invoke(_waifuId);
                });
            }

            public void Set(WaifuDefinitionSO definition, int stars)
            {
                _waifuId = definition != null ? definition.Id : null;

                bool occupied = definition != null;
                _art.gameObject.SetActive(occupied);
                _remove.gameObject.SetActive(occupied);

                if (!occupied)
                {
                    _name.text = "Vacío";
                    _name.color = MetaUi.Muted;
                    _stars.text = string.Empty;
                    return;
                }

                Sprite sprite = definition.ChibiArt != null ? definition.ChibiArt : definition.CardArt;
                if (sprite != null)
                {
                    _art.sprite = sprite;
                    _art.color = Color.white;
                }
                else
                {
                    _art.sprite = null;
                    _art.color = definition.PortraitColor;
                }

                _name.text = definition.DisplayName;
                _name.color = MetaUi.Text;
                _stars.text = RosterChipView.StarsText(stars, true);
            }
        }
    }
}
