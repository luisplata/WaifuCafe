using System;
using System.Collections.Generic;
using System.Globalization;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Reglas puras de sinergia: cuenta facciones y roles de la party, aplica la tabla de
    /// breakpoints (D3) y resuelve el gate de ultimate T1/T2. No conoce ScriptableObjects:
    /// recibe <see cref="WaifuKit"/> ya resueltos (con las estrellas del perfil).
    /// </summary>
    public static class SynergyModel
    {
        /// <summary>
        /// Calcula todos los modificadores de la party. El contrato sólo aporta su
        /// multiplicador de carga, sus monedas por cascada y el apagado de ultimates.
        /// </summary>
        public static PartyModifiers Build(IReadOnlyList<WaifuKit> party, ContractModifiers contract = null)
        {
            var mods = new PartyModifiers { Party = Clean(party) };
            IReadOnlyList<WaifuKit> kits = mods.Party;

            Count(kits, out int cafe, out int miticos, out int espectaculo,
                out int cocina, out int servicio, out int apoyo, out int control);

            int cafeTier = FactionTable.CafeTier(cafe);
            int miticosTier = FactionTable.MiticosTier(miticos);
            int espectaculoTier = FactionTable.EspectaculoTier(espectaculo);

            mods.OrderCoinBonus = FactionTable.CafeCoinsPerOrder(cafeTier);
            mods.ChargeCostMultiplier = FactionTable.CafeChargeCostMultiplier(cafeTier);
            mods.ChargeCostFlat = FactionTable.MiticosChargeFlat(miticosTier);
            mods.PotencyBonus = FactionTable.MiticosPotency(miticosTier);
            mods.StartChargeHalf = FactionTable.MiticosHalfStartCharge(miticosTier);
            mods.PatienceMultiplier = FactionTable.EspectaculoPatience(espectaculoTier);
            mods.CascadeCoinBonus = FactionTable.EspectaculoCascadeCoins(espectaculoTier);

            mods.PotencyBonus += FactionTable.CocinaPotency(FactionTable.RoleTier(cocina));
            mods.ChargeGainMultiplier = FactionTable.ServicioChargeGain(FactionTable.RoleTier(servicio));
            mods.StartCharge = FactionTable.ApoyoStartCharge(FactionTable.RoleTier(apoyo));
            mods.ControlCooldownReduction = FactionTable.ControlCooldown(FactionTable.RoleTier(control));

            if (contract != null)
            {
                if (contract.chargeCostMultiplier > 0f)
                    mods.ChargeCostMultiplier *= contract.chargeCostMultiplier;
                if (contract.coinsPerCascade > 0)
                    mods.CascadeCoinBonus += contract.coinsPerCascade;
                mods.UltimateEnabled = contract.ultimateEnabled;
            }

            mods.Ultimate = EvaluateUltimate(kits, mods.UltimateEnabled);

            // Zorro de Fuego (T2) es también una sinergia pasiva: +2 carga inicial y +1 potencia.
            if (mods.Ultimate.Tier >= 2)
            {
                mods.StartCharge += 2;
                mods.PotencyBonus += 1;
            }

            return mods;
        }

        /// <summary>
        /// Gate de ultimate. T2 (4 Míticos y Σ★≥8) gana sobre T1. T1 exige 3 miembros de
        /// una facción y al menos una ★3 actual en esa facción.
        /// </summary>
        public static UltimateInfo EvaluateUltimate(IReadOnlyList<WaifuKit> party, bool enabled = true)
        {
            if (!enabled) return UltimateInfo.None;

            IReadOnlyList<WaifuKit> kits = Clean(party);
            Count(kits, out int cafe, out int miticos, out int espectaculo,
                out int cocina, out int servicio, out int apoyo, out int control);

            if (miticos >= 3)
            {
                int miticosStars = SumStars(kits, WaifuFaction.Miticos);
                if (miticos >= FactionTable.T2Members && miticosStars >= FactionTable.T2Stars)
                {
                    return new UltimateInfo(UltimateId.ZorroDeFuego, 2, WaifuFaction.Miticos,
                        LeaderSlotOf(kits, WaifuFaction.Miticos), miticos);
                }

                if (HasThreeStar(kits, WaifuFaction.Miticos))
                {
                    return new UltimateInfo(UltimateId.LunaDeSangre, 1, WaifuFaction.Miticos,
                        LeaderSlotOf(kits, WaifuFaction.Miticos), miticos);
                }

                return UltimateInfo.None;
            }

            if (cafe >= 3 && HasThreeStar(kits, WaifuFaction.Cafe))
            {
                return new UltimateInfo(UltimateId.LibroMayor, 1, WaifuFaction.Cafe,
                    LeaderSlotOf(kits, WaifuFaction.Cafe), cafe);
            }

            if (espectaculo >= 3 && HasThreeStar(kits, WaifuFaction.Espectaculo))
            {
                return new UltimateInfo(UltimateId.Encore, 1, WaifuFaction.Espectaculo,
                    LeaderSlotOf(kits, WaifuFaction.Espectaculo), espectaculo);
            }

            return UltimateInfo.None;
        }

        /// <summary>Líder de la party: más estrellas; empate gana el slot más bajo.</summary>
        public static int LeaderSlot(IReadOnlyList<WaifuKit> party)
        {
            IReadOnlyList<WaifuKit> kits = Clean(party);
            if (kits.Count == 0) return -1;

            int best = 0;
            for (int i = 1; i < kits.Count; i++)
                if (WaifuStars.Clamp(kits[i].Stars) > WaifuStars.Clamp(kits[best].Stars)) best = i;
            return best;
        }

        /// <summary>Resumen de una línea para el panel de party: "Míticos 3 · Cocina 2".</summary>
        public static string Summary(IReadOnlyList<WaifuKit> party)
        {
            IReadOnlyList<WaifuKit> kits = Clean(party);
            Count(kits, out int cafe, out int miticos, out int espectaculo,
                out int cocina, out int servicio, out int apoyo, out int control);

            var parts = new List<string>(7);
            if (cafe >= FactionTable.CafeBreakpoint2)
                parts.Add(FactionTable.FactionName(WaifuFaction.Cafe) + " " + cafe);
            if (miticos >= FactionTable.MiticosBreakpoint2)
                parts.Add(FactionTable.FactionName(WaifuFaction.Miticos) + " " + miticos);
            if (espectaculo >= FactionTable.EspectaculoBreakpoint2)
                parts.Add(FactionTable.FactionName(WaifuFaction.Espectaculo) + " " + espectaculo);

            if (cocina >= FactionTable.RoleBreakpoint2) parts.Add(FactionTable.RoleName(WaifuRole.Cocina) + " " + cocina);
            if (servicio >= FactionTable.RoleBreakpoint2) parts.Add(FactionTable.RoleName(WaifuRole.Servicio) + " " + servicio);
            if (apoyo >= FactionTable.RoleBreakpoint2) parts.Add(FactionTable.RoleName(WaifuRole.Apoyo) + " " + apoyo);
            if (control >= FactionTable.RoleBreakpoint2) parts.Add(FactionTable.RoleName(WaifuRole.Control) + " " + control);

            return parts.Count == 0 ? "— sin breakpoints" : string.Join(" · ", parts);
        }

        /// <summary>Texto del badge de ultimate del menú y del HUD.</summary>
        public static string BadgeText(UltimateInfo ultimate, IReadOnlyList<WaifuKit> party)
        {
            if (ultimate.IsReady)
            {
                return ultimate.Live
                    ? "ULTIMATE: " + ultimate.Title + " — LISTA"
                    : "ULTIMATE: " + ultimate.Title + " — PRÓXIMAMENTE";
            }

            IReadOnlyList<WaifuKit> kits = Clean(party);
            if (kits.Count < FactionTable.PartySize) return "ULTIMATE: — party incompleta";

            Count(kits, out int cafe, out int miticos, out int espectaculo,
                out int cocina, out int servicio, out int apoyo, out int control);

            int miticosStars = SumStars(kits, WaifuFaction.Miticos);
            if (miticos >= FactionTable.T2Members && miticosStars < FactionTable.T2Stars)
                return "ULTIMATE: Zorro de Fuego — falta Σ★ " + FactionTable.T2Stars;

            if (miticos >= 3) return "ULTIMATE: Míticos 3/3 — falta una ★3";
            if (cafe >= 3) return "ULTIMATE: Café 3/3 — falta una ★3";
            if (espectaculo >= 3) return "ULTIMATE: Espectáculo 3/3 — falta una ★3";

            int bestMembers = Math.Max(cafe, Math.Max(miticos, espectaculo));
            if (bestMembers == 2)
                return "ULTIMATE: — 2+2 no alcanza (hacen falta 3)";

            return "ULTIMATE: — sin 3 de una facción";
        }

        /// <summary>Tooltip de sinergia: breakpoints activos y estado del gate.</summary>
        public static string Tooltip(IReadOnlyList<WaifuKit> party)
        {
            IReadOnlyList<WaifuKit> kits = Clean(party);
            Count(kits, out int cafe, out int miticos, out int espectaculo,
                out int cocina, out int servicio, out int apoyo, out int control);

            var lines = new List<string>(6);

            int cafeTier = FactionTable.CafeTier(cafe);
            if (cafeTier >= 2)
            {
                string line = FactionTable.FactionName(WaifuFaction.Cafe) + ": +" +
                              FactionTable.CafeCoinsPerOrder(cafeTier) + " moneda/pedido";
                if (cafeTier >= 3) line += " · costo de carga ×0.9";
                lines.Add(line);
            }

            int miticosTier = FactionTable.MiticosTier(miticos);
            if (miticosTier >= 2)
            {
                string line = FactionTable.FactionName(WaifuFaction.Miticos) + ": −" +
                              FactionTable.MiticosChargeFlat(miticosTier) + " costo de carga";
                if (miticosTier >= 3) line += " · +1 potencia";
                if (miticosTier >= 4) line += " · 50% carga inicial";
                lines.Add(line);
            }

            int espectaculoTier = FactionTable.EspectaculoTier(espectaculo);
            if (espectaculoTier >= 2)
            {
                int percent = (int)Math.Round(
                    (FactionTable.EspectaculoPatience(espectaculoTier) - 1f) * 100f,
                    MidpointRounding.AwayFromZero);
                string line = FactionTable.FactionName(WaifuFaction.Espectaculo) + ": +" + percent + "% paciencia";
                if (espectaculoTier >= 3) line += " · +1 moneda por cascada";
                lines.Add(line);
            }

            int cocinaTier = FactionTable.RoleTier(cocina);
            if (cocinaTier >= 2)
                lines.Add(FactionTable.RoleName(WaifuRole.Cocina) + ": +1 potencia");

            int servicioTier = FactionTable.RoleTier(servicio);
            if (servicioTier >= 2)
                lines.Add(FactionTable.RoleName(WaifuRole.Servicio) + ": carga ×" +
                          FactionTable.ServicioChargeGain(servicioTier)
                              .ToString("0.00", CultureInfo.InvariantCulture));

            int apoyoTier = FactionTable.RoleTier(apoyo);
            if (apoyoTier >= 2)
                lines.Add(FactionTable.RoleName(WaifuRole.Apoyo) + ": +" +
                          FactionTable.ApoyoStartCharge(apoyoTier) + " carga inicial");

            if (FactionTable.RoleTier(control) >= 2)
                lines.Add(FactionTable.RoleName(WaifuRole.Control) + ": −1 cooldown");

            UltimateInfo ultimate = EvaluateUltimate(kits);
            if (ultimate.IsReady)
            {
                lines.Add(ultimate.Live
                    ? "Ultimate lista: " + ultimate.Title
                    : ultimate.Title + ": contenido de persecución");
            }
            else if (kits.Count >= FactionTable.PartySize)
            {
                lines.Add("Ultimate: 3 de una facción con una ★3.");
            }

            if (lines.Count == 0) lines.Add("Sinergia: sumá facciones o roles repetidos.");

            const int maxLines = 4;
            if (lines.Count > maxLines) lines.RemoveRange(maxLines, lines.Count - maxLines);
            return string.Join("\n", lines);
        }

        // ---- Helpers -------------------------------------------------------------

        private static IReadOnlyList<WaifuKit> Clean(IReadOnlyList<WaifuKit> party)
        {
            if (party == null || party.Count == 0) return Array.Empty<WaifuKit>();

            var clean = new List<WaifuKit>(party.Count);
            for (int i = 0; i < party.Count; i++)
                if (party[i] != null) clean.Add(party[i]);
            return clean;
        }

        private static void Count(IReadOnlyList<WaifuKit> kits, out int cafe, out int miticos,
            out int espectaculo, out int cocina, out int servicio, out int apoyo, out int control)
        {
            cafe = miticos = espectaculo = 0;
            cocina = servicio = apoyo = control = 0;

            for (int i = 0; i < kits.Count; i++)
            {
                WaifuKit kit = kits[i];
                switch (kit.Faction)
                {
                    case WaifuFaction.Cafe: cafe++; break;
                    case WaifuFaction.Miticos: miticos++; break;
                    case WaifuFaction.Espectaculo: espectaculo++; break;
                }

                switch (kit.Role)
                {
                    case WaifuRole.Cocina: cocina++; break;
                    case WaifuRole.Servicio: servicio++; break;
                    case WaifuRole.Apoyo: apoyo++; break;
                    case WaifuRole.Control: control++; break;
                }
            }
        }

        private static int SumStars(IReadOnlyList<WaifuKit> kits, WaifuFaction faction)
        {
            int total = 0;
            for (int i = 0; i < kits.Count; i++)
                if (kits[i].Faction == faction) total += WaifuStars.Clamp(kits[i].Stars);
            return total;
        }

        private static bool HasThreeStar(IReadOnlyList<WaifuKit> kits, WaifuFaction faction)
        {
            for (int i = 0; i < kits.Count; i++)
                if (kits[i].Faction == faction && WaifuStars.Clamp(kits[i].Stars) >= WaifuStars.Max)
                    return true;
            return false;
        }

        private static int LeaderSlotOf(IReadOnlyList<WaifuKit> kits, WaifuFaction faction)
        {
            int best = -1;
            for (int i = 0; i < kits.Count; i++)
            {
                if (kits[i].Faction != faction) continue;
                if (best < 0 || WaifuStars.Clamp(kits[i].Stars) > WaifuStars.Clamp(kits[best].Stars))
                    best = i;
            }

            return best;
        }
    }
}
