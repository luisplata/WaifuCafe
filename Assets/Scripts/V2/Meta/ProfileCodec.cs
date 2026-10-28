using System;
using System.Collections.Generic;
using UnityEngine;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    [Serializable]
    internal sealed class OwnedEntryDto
    {
        public string waifuId;
        public int stars;
    }

    /// <summary>DTO plano: JsonUtility no serializa diccionarios.</summary>
    [Serializable]
    internal sealed class PlayerProfileDto
    {
        public int schema;
        public bool starterClaimed;
        public int tutorialStep;
        public int coins;
        public string lastContractId;
        public OwnedEntryDto[] owned;
        public string[] party;
    }

    /// <summary>
    /// Traduce entre <see cref="PlayerProfile"/> y el JSON de <c>waifu_profile_v1</c>.
    /// Nunca lanza: un documento vacío, corrupto o de schema futuro cae a defaults.
    /// </summary>
    public static class ProfileCodec
    {
        public static string Encode(PlayerProfile profile)
        {
            profile ??= new PlayerProfile();

            var dto = new PlayerProfileDto
            {
                schema = profile.schema,
                starterClaimed = profile.starterClaimed,
                tutorialStep = (int)profile.tutorialStep,
                coins = profile.coins,
                lastContractId = profile.lastContractId ?? string.Empty,
            };

            List<StarterEntry> owned = profile.owned;
            if (owned != null && owned.Count > 0)
            {
                var entries = new List<OwnedEntryDto>(owned.Count);
                foreach (StarterEntry entry in owned)
                {
                    if (entry == null) continue;
                    entries.Add(new OwnedEntryDto
                    {
                        waifuId = entry.waifuId ?? string.Empty,
                        stars = entry.stars,
                    });
                }
                dto.owned = entries.ToArray();
            }
            else
            {
                dto.owned = Array.Empty<OwnedEntryDto>();
            }

            List<string> party = profile.party;
            dto.party = party != null && party.Count > 0 ? party.ToArray() : Array.Empty<string>();

            return JsonUtility.ToJson(dto);
        }

        /// <summary>
        /// Devuelve true si decodificó un documento con schema soportado. En cualquier
        /// otro caso entrega un perfil default y marca <paramref name="corrupted"/>.
        /// </summary>
        public static bool TryRead(string rawJson, out PlayerProfile profile, out bool corrupted)
        {
            profile = new PlayerProfile();
            corrupted = false;

            if (string.IsNullOrWhiteSpace(rawJson)) return false;

            try
            {
                PlayerProfileDto dto = JsonUtility.FromJson<PlayerProfileDto>(rawJson);
                if (dto == null)
                {
                    corrupted = true;
                    Debug.LogWarning("[ProfileCodec] JSON nulo; se usan defaults.");
                    return false;
                }

                if (dto.schema > PlayerProfile.CurrentSchema)
                {
                    corrupted = true;
                    Debug.LogWarning(
                        $"[ProfileCodec] schema {dto.schema} no soportado (max {PlayerProfile.CurrentSchema}); se usan defaults.");
                    return false;
                }

                profile = ToProfile(dto);
                return true;
            }
            catch (Exception exception)
            {
                corrupted = true;
                Debug.LogWarning($"[ProfileCodec] Perfil corrupto: {exception.Message}. Se usan defaults.");
                return false;
            }
        }

        private static PlayerProfile ToProfile(PlayerProfileDto dto)
        {
            var profile = new PlayerProfile
            {
                schema = dto.schema,
                starterClaimed = dto.starterClaimed,
                tutorialStep = (TutorialStep)dto.tutorialStep,
                coins = dto.coins,
                lastContractId = dto.lastContractId ?? string.Empty,
            };

            if (dto.owned != null)
            {
                foreach (OwnedEntryDto entry in dto.owned)
                {
                    if (entry == null) continue;
                    profile.owned.Add(new StarterEntry(entry.waifuId ?? string.Empty, entry.stars));
                }
            }

            if (dto.party != null)
            {
                foreach (string id in dto.party)
                    if (id != null) profile.party.Add(id);
            }

            return profile;
        }
    }
}
