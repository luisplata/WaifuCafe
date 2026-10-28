using System;
using System.Collections.Generic;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Normaliza un perfil contra el catálogo. Es puro (sin ScriptableObjects ni Unity):
    /// el llamador pasa los ids conocidos. Nunca lanza y no inventa contenido.
    /// Reglas: schema futuro → defaults; estrellas 1..3; duplicados colapsados; ids
    /// desconocidos fuera; party ≤4 distintas y poseídas; contrato desconocido → "".
    /// </summary>
    public static class ProfileSanitizer
    {
        public const int MaxPartySize = 4;

        /// <summary>Devuelve true si mutó el perfil (útil para decidir si re-guardar).</summary>
        public static bool Sanitize(
            PlayerProfile profile,
            IReadOnlyCollection<string> knownWaifuIds,
            IReadOnlyCollection<string> knownContractIds = null)
        {
            if (profile == null) return false;

            if (profile.schema > PlayerProfile.CurrentSchema)
            {
                Reset(profile);
                return true;
            }

            bool changed = false;

            if (profile.schema != PlayerProfile.CurrentSchema)
            {
                profile.schema = PlayerProfile.CurrentSchema;
                changed = true;
            }

            if (profile.coins < 0)
            {
                profile.coins = 0;
                changed = true;
            }

            if (!Enum.IsDefined(typeof(TutorialStep), (int)profile.tutorialStep))
            {
                profile.tutorialStep = TutorialStep.Gift;
                changed = true;
            }

            HashSet<string> knownWaifus = ToSet(knownWaifuIds);
            var owned = new List<StarterEntry>();
            var byId = new Dictionary<string, StarterEntry>(StringComparer.OrdinalIgnoreCase);

            if (profile.owned == null)
            {
                changed = true;
            }
            else
            {
                foreach (StarterEntry entry in profile.owned)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.waifuId))
                    {
                        changed = true;
                        continue;
                    }

                    string id = entry.waifuId.Trim();
                    if (!knownWaifus.Contains(id))
                    {
                        changed = true;
                        continue;
                    }

                    int stars = WaifuStars.Clamp(entry.stars);
                    if (stars != entry.stars || id != entry.waifuId) changed = true;

                    if (byId.TryGetValue(id, out StarterEntry existing))
                    {
                        changed = true;
                        if (stars > existing.stars)
                        {
                            int index = owned.IndexOf(existing);
                            var upgraded = new StarterEntry(existing.waifuId, stars);
                            owned[index] = upgraded;
                            byId[id] = upgraded;
                        }
                        continue;
                    }

                    var normalized = new StarterEntry(id, stars);
                    owned.Add(normalized);
                    byId[id] = normalized;
                }

                if (!SameOwned(profile.owned, owned)) changed = true;
            }

            profile.owned = owned;

            var party = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (profile.party == null)
            {
                changed = true;
            }
            else
            {
                foreach (string raw in profile.party)
                {
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        changed = true;
                        continue;
                    }

                    string id = raw.Trim();
                    if (!byId.ContainsKey(id))
                    {
                        changed = true;
                        continue;
                    }

                    if (!seen.Add(id))
                    {
                        changed = true;
                        continue;
                    }

                    if (party.Count >= MaxPartySize)
                    {
                        changed = true;
                        continue;
                    }

                    if (id != raw) changed = true;
                    party.Add(byId[id].waifuId);
                }

                if (!SameStrings(profile.party, party)) changed = true;
            }

            profile.party = party;

            string contract = profile.lastContractId == null ? string.Empty : profile.lastContractId.Trim();
            if (contract.Length > 0 && knownContractIds != null)
            {
                HashSet<string> knownContracts = ToSet(knownContractIds);
                if (!knownContracts.Contains(contract)) contract = string.Empty;
            }

            if (!string.Equals(contract, profile.lastContractId, StringComparison.Ordinal))
                changed = true;
            profile.lastContractId = contract;

            return changed;
        }

        private static HashSet<string> ToSet(IReadOnlyCollection<string> values)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (values == null) return set;
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value)) set.Add(value.Trim());
            }
            return set;
        }

        private static bool SameOwned(List<StarterEntry> a, List<StarterEntry> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] == null || b[i] == null) return false;
                if (!string.Equals(a[i].waifuId, b[i].waifuId, StringComparison.OrdinalIgnoreCase)) return false;
                if (a[i].stars != b[i].stars) return false;
            }
            return true;
        }

        private static bool SameStrings(List<string> a, List<string> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        private static void Reset(PlayerProfile profile)
        {
            profile.schema = PlayerProfile.CurrentSchema;
            profile.starterClaimed = false;
            profile.tutorialStep = TutorialStep.Gift;
            profile.coins = 0;
            profile.lastContractId = string.Empty;
            profile.owned = new List<StarterEntry>();
            profile.party = new List<string>();
        }
    }
}
