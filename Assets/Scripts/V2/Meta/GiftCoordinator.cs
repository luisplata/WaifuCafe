using System;
using System.Collections.Generic;
using UnityEngine;
using V2.Waifu;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// Regalo inicial y recompensa del tutorial. Muestra los tiers 1★→2★→3★ con
    /// <see cref="GiftOverlay"/> y, al terminar (o saltar), entrega las waifus curadas al
    /// perfil una sola vez. El regalo fija <c>starterClaimed</c> y avanza a
    /// <see cref="TutorialStep.MenuTour"/>.
    /// </summary>
    public sealed class GiftCoordinator : MonoBehaviour
    {
        private static readonly Color TierOne = new Color(0.96f, 0.95f, 1f, 1f);
        private static readonly Color TierTwo = new Color(0.52f, 0.74f, 1f, 1f);
        private static readonly Color TierThree = new Color(1f, 0.84f, 0.34f, 1f);

        [SerializeField] private Transform canvasRoot;
        [SerializeField] private StarterConfigSO starterConfig;

        private GiftOverlay _overlay;

        public bool IsPlaying => _overlay != null && _overlay.IsVisible;

        private Transform Canvas => canvasRoot != null ? canvasRoot : transform;

        private void EnsureOverlay()
        {
            _overlay ??= new GiftOverlay(Canvas);
        }

        /// <summary>Regalo de bienvenida: 4 starters, un tier por pantalla.</summary>
        public void PlayGift(IProfileStore profile, Action onCompleted)
        {
            if (profile == null)
            {
                Debug.LogError("[GiftCoordinator] PlayGift sin perfil.");
                onCompleted?.Invoke();
                return;
            }

            EnsureOverlay();
            _overlay.Play(BuildGroups(starterConfig != null ? starterConfig.Gift : null),
                "REGALO DE BIENVENIDA",
                "Estas cuatro waifus se quedan en tu perfil. Después del tour vas a ganar dos más.",
                () =>
                {
                    GrantStarters(profile);
                    onCompleted?.Invoke();
                });
        }

        /// <summary>Recompensa del tutorial reutilizando la misma cinemática por tiers.</summary>
        public void PlayReward(Action onCompleted)
        {
            EnsureOverlay();
            _overlay.Play(BuildGroups(starterConfig != null ? starterConfig.Reward : null),
                "RECOMPENSA DEL TUTORIAL",
                "Hana 1★ y Yuki 2★ se unen al café. Con Rika 3★ y Luna, tu party Mítica ya puede " +
                "desbloquear Luna de Sangre (3×3 a Postre y +1 potencia).",
                onCompleted);
        }

        /// <summary>Entrega los starters, marca <c>starterClaimed</c> y pasa a MenuTour.</summary>
        private void GrantStarters(IProfileStore profile)
        {
            PlayerProfile current = profile.Current;
            if (current == null) return;

            Grant(current, starterConfig != null ? starterConfig.Gift : null);
            current.starterClaimed = true;
            current.tutorialStep = TutorialStep.MenuTour;
            profile.Save();
        }

        /// <summary>Abre regalo y recompensa al perfil sin duplicar ni bajar estrellas.</summary>
        public static bool Grant(PlayerProfile profile, IReadOnlyList<StarterEntryConfig> entries)
        {
            if (profile == null || entries == null) return false;

            bool changed = false;
            foreach (StarterEntryConfig entry in entries)
            {
                if (entry == null) continue;

                string id = entry.Id;
                if (string.IsNullOrWhiteSpace(id)) continue;

                StarterEntry existing = Find(profile, id);
                int stars = WaifuStars.Clamp(entry.Stars);

                if (existing == null)
                {
                    profile.owned ??= new List<StarterEntry>();
                    profile.owned.Add(new StarterEntry(id, stars));
                    changed = true;
                }
                else if (stars > existing.stars)
                {
                    existing.stars = stars;
                    changed = true;
                }
            }

            return changed;
        }

        private static StarterEntry Find(PlayerProfile profile, string id)
        {
            if (profile == null || profile.owned == null) return null;

            foreach (StarterEntry entry in profile.owned)
            {
                if (entry != null && string.Equals(entry.waifuId, id, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            return null;
        }

        private static GiftGroup[] BuildGroups(IReadOnlyList<StarterEntryConfig> entries)
        {
            if (entries == null || entries.Count == 0) return Array.Empty<GiftGroup>();

            var groups = new List<GiftGroup>(3);
            for (int stars = 1; stars <= 3; stars++)
            {
                var cards = new List<GiftCardData>();
                foreach (StarterEntryConfig entry in entries)
                {
                    if (entry == null || entry.Waifu == null || entry.Stars != stars) continue;

                    WaifuDefinitionSO definition = entry.Waifu;
                    cards.Add(new GiftCardData
                    {
                        Art = entry.Art,
                        Name = definition.DisplayName,
                        Stars = stars,
                        Subtitle = RosterChipView.FactionRole(definition),
                    });
                }

                if (cards.Count == 0) continue;

                groups.Add(new GiftGroup
                {
                    Title = stars + "★ · " + TierName(stars),
                    Color = TierColor(stars),
                    Cards = cards.ToArray(),
                });
            }

            return groups.ToArray();
        }

        private static Color TierColor(int stars)
        {
            switch (stars)
            {
                case 2: return TierTwo;
                case 3: return TierThree;
                default: return TierOne;
            }
        }

        private static string TierName(int stars)
        {
            switch (stars)
            {
                case 2: return "RARA";
                case 3: return "ÉPICA";
                default: return "COMÚN";
            }
        }
    }
}
