using System.Collections.Generic;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Un contrato candidato del pool: id + facción. El recomendador trabaja con esto y no
    /// con ScriptableObjects, igual que <see cref="SynergyModel"/>.
    /// </summary>
    public readonly struct ContractCandidate
    {
        public ContractCandidate(string id, WaifuFaction faction)
        {
            Id = id;
            Faction = faction;
        }

        public string Id { get; }
        public WaifuFaction Faction { get; }
    }

    /// <summary>
    /// Recomendación del juego al jugador: sugiere el contrato del pool que le sirve a la
    /// facción dominante de la party. Sin mayoría clara, o sin contrato de esa facción en
    /// el pool, no recomienda nada: el listón simplemente no aparece.
    /// </summary>
    public static class ContractRecommendation
    {
        /// <summary>
        /// Facción con mayoría **estricta** en la party, o <c>null</c> si hay empate o la
        /// party está vacía. Con 4 miembros hace falta 3 o más; un 2/2 no recomienda.
        /// </summary>
        public static WaifuFaction? DominantFaction(IReadOnlyList<WaifuFaction> partyFactions)
        {
            if (partyFactions == null || partyFactions.Count == 0) return null;

            int cafe = 0, miticos = 0, espectaculo = 0;
            for (int i = 0; i < partyFactions.Count; i++)
            {
                switch (partyFactions[i])
                {
                    case WaifuFaction.Cafe: cafe++; break;
                    case WaifuFaction.Miticos: miticos++; break;
                    case WaifuFaction.Espectaculo: espectaculo++; break;
                }
            }

            int total = partyFactions.Count;
            if (cafe * 2 > total && cafe >= miticos && cafe >= espectaculo) return WaifuFaction.Cafe;
            if (miticos * 2 > total && miticos >= cafe && miticos >= espectaculo) return WaifuFaction.Miticos;
            if (espectaculo * 2 > total && espectaculo >= cafe && espectaculo >= miticos)
                return WaifuFaction.Espectaculo;

            return null;
        }

        /// <summary>
        /// Primer contrato del pool que comparte la facción dominante de la party. Devuelve
        /// <c>null</c> cuando no hay recomendación que dar.
        /// </summary>
        public static string Recommend(IReadOnlyList<ContractCandidate> pool,
            IReadOnlyList<WaifuFaction> partyFactions)
        {
            if (pool == null || pool.Count == 0) return null;

            WaifuFaction? dominant = DominantFaction(partyFactions);
            if (!dominant.HasValue) return null;

            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Faction != dominant.Value) continue;
                if (string.IsNullOrWhiteSpace(pool[i].Id)) continue;
                return pool[i].Id;
            }

            return null;
        }
    }
}
