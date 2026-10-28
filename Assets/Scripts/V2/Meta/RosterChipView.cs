using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using V2.Waifu;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// Ficha del roster. Las poseídas muestran arte, nombre, estrellas del perfil y
    /// facción·rol y se pueden meter/sacar de la party; las no poseídas quedan con
    /// overlay LOCK y no son accionables.
    /// </summary>
    public sealed class RosterChipView
    {
        private static readonly Color ChipColor = new Color(0.12f, 0.10f, 0.18f, 0.96f);
        private static readonly Color PartyColor = new Color(0.17f, 0.14f, 0.27f, 1f);

        private readonly WaifuDefinitionSO _definition;
        private readonly Image _background;
        private readonly GameObject _partyTag;

        public RosterChipView(Transform parent, WaifuDefinitionSO definition, int stars, bool owned,
            bool inParty, Action<WaifuDefinitionSO> onToggle)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            _definition = definition;
            const float width = 200f;
            const float height = 160f;

            GameObject root = MetaUi.Panel(parent, "Chip_" + definition.Id,
                inParty ? PartyColor : ChipColor, Vector2.zero, new Vector2(width, height));
            _background = root.GetComponent<Image>();

            Image art = MetaUi.Image(root.transform, "Art", Color.white, new Vector2(64f, 8f),
                new Vector2(72f, 72f));
            art.preserveAspect = true;
            art.raycastTarget = false;
            Sprite sprite = definition.ChibiArt != null ? definition.ChibiArt : definition.CardArt;
            if (sprite != null)
            {
                art.sprite = sprite;
            }
            else
            {
                art.color = definition.PortraitColor;
            }

            MetaUi.Label(root.transform, "Name", definition.DisplayName, 19f,
                owned ? MetaUi.Text : MetaUi.Muted, TextAlignmentOptions.Center,
                new Vector2(6f, 82f), new Vector2(width - 12f, 24f), FontStyles.Bold);

            MetaUi.Label(root.transform, "Stars", StarsText(stars, owned), 17f,
                owned ? MetaUi.Accent : MetaUi.Muted, TextAlignmentOptions.Center,
                new Vector2(6f, 105f), new Vector2(width - 12f, 20f));

            MetaUi.Label(root.transform, "FactionRole", FactionRole(definition), 12f,
                MetaUi.Muted, TextAlignmentOptions.Center, new Vector2(6f, 127f),
                new Vector2(width - 12f, 18f));

            _partyTag = MetaUi.Label(root.transform, "PartyTag", "EN PARTY", 11f, MetaUi.Accent,
                TextAlignmentOptions.Center, new Vector2(118f, 4f), new Vector2(74f, 18f),
                FontStyles.Bold).gameObject;
            _partyTag.SetActive(inParty);

            Button button = root.AddComponent<Button>();
            button.targetGraphic = _background;

            if (owned)
            {
                button.onClick.AddListener(() => onToggle?.Invoke(_definition));
            }
            else
            {
                button.interactable = false;

                GameObject overlay = MetaUi.Panel(root.transform, "LockOverlay",
                    new Color(0.04f, 0.03f, 0.07f, 0.62f), Vector2.zero, new Vector2(width, height));
                overlay.transform.SetAsLastSibling();
                MetaUi.Label(overlay.transform, "Lock", "LOCK", 18f, MetaUi.Muted,
                    TextAlignmentOptions.Center, new Vector2(0f, 62f), new Vector2(width, 32f),
                    FontStyles.Bold);
                Button overlayButton = overlay.AddComponent<Button>();
                overlayButton.interactable = false;
            }
        }

        public WaifuDefinitionSO Definition => _definition;

        public void SetInParty(bool inParty)
        {
            _background.color = inParty ? PartyColor : ChipColor;
            _partyTag.SetActive(inParty);
        }

        public static string StarsText(int stars, bool owned)
        {
            if (!owned || stars <= 0) return "—";
            int clamped = Mathf.Clamp(stars, WaifuStars.Min, WaifuStars.Max);
            return clamped + "/" + WaifuStars.Max;
        }

        public static string FactionRole(WaifuDefinitionSO definition)
        {
            return ContractCardView.FactionName(definition.Faction) + " · " + RoleName(definition.Role);
        }

        private static string RoleName(WaifuRole role)
        {
            switch (role)
            {
                case WaifuRole.Cocina: return "Cocina";
                case WaifuRole.Servicio: return "Servicio";
                case WaifuRole.Apoyo: return "Apoyo";
                case WaifuRole.Control: return "Control";
                default: return role.ToString();
            }
        }
    }
}
