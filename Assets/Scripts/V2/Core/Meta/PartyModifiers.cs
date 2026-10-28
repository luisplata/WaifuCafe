using System;
using System.Collections.Generic;

namespace WaifuCafe.Core.Meta
{
    /// <summary>Ultimates de facción. Sólo una está viva en V1 (ver <see cref="UltimateTable.IsLive"/>).</summary>
    public enum UltimateId
    {
        None = 0,
        LibroMayor = 1,
        LunaDeSangre = 2,
        Encore = 3,
        ZorroDeFuego = 4,
    }

    /// <summary>
    /// Resultado del gate de ultimate: identidad, facción que lo habilita, caster y cuántos
    /// miembros aportó. <see cref="IsReady"/> es el gate T1/T2; <see cref="Live"/> dice si
    /// V1 lo expone al jugador o si sigue siendo contenido de persecución/debug.
    /// </summary>
    public readonly struct UltimateInfo
    {
        public readonly UltimateId Id;
        public readonly int Tier;
        public readonly WaifuFaction Faction;
        public readonly int LeaderSlot;
        public readonly int Members;

        public UltimateInfo(UltimateId id, int tier, WaifuFaction faction, int leaderSlot, int members)
        {
            Id = id;
            Tier = tier;
            Faction = faction;
            LeaderSlot = leaderSlot;
            Members = members;
        }

        public bool IsReady => Id != UltimateId.None;
        public string Title => UltimateTable.Title(Id);

        /// <summary>V1 sólo expone Luna de Sangre; el resto es chase/debug (FU2).</summary>
        public bool Live => UltimateTable.IsLive(Id);

        public static readonly UltimateInfo None =
            new UltimateInfo(UltimateId.None, 0, WaifuFaction.Cafe, -1, 0);
    }

    /// <summary>Tabla estática de ultimates: título, unidades que entrega y si está viva.</summary>
    public static class UltimateTable
    {
        public static string Title(UltimateId id)
        {
            switch (id)
            {
                case UltimateId.LibroMayor: return "Libro Mayor";
                case UltimateId.LunaDeSangre: return "Luna de Sangre";
                case UltimateId.Encore: return "Encore";
                case UltimateId.ZorroDeFuego: return "Zorro de Fuego";
                default: return string.Empty;
            }
        }

        /// <summary>Libro Mayor entrega 1 unidad a cada pedido; Encore entrega 2.</summary>
        public static int DeliverUnits(UltimateId id)
        {
            switch (id)
            {
                case UltimateId.LibroMayor: return 1;
                case UltimateId.Encore: return 2;
                default: return 0;
            }
        }

        public static bool IsLive(UltimateId id) => id == UltimateId.LunaDeSangre;
    }

    /// <summary>
    /// Modificadores de party calculados por <see cref="SynergyModel"/>: breakpoints de
    /// facción/rol, pasivas y gate de ultimate. Es lo que inyecta el runtime en la party,
    /// la carga, los cooldowns y el piso del café. Sin Unity.
    /// </summary>
    public sealed class PartyModifiers
    {
        /// <summary>Party ordenada por slot (estrellas incluidas) usada para calcular esto.</summary>
        public IReadOnlyList<WaifuKit> Party = Array.Empty<WaifuKit>();

        /// <summary>Potencia extra: Míticos 3, Cocina 2, Zorro T2 y el +1 de Luna al castear.</summary>
        public int PotencyBonus;

        /// <summary>Carga extra: Míticos 2/3 (−1/−2).</summary>
        public int ChargeCostFlat;

        /// <summary>Carga ×: Café 3 (0.9) y el modificador del contrato (Noche 0.8).</summary>
        public float ChargeCostMultiplier = 1f;

        /// <summary>Ganancia de carga ×: Servicio 2/3 (1.10/1.20).</summary>
        public float ChargeGainMultiplier = 1f;

        /// <summary>Unidades de carga con las que arranca cada slot (Apoyo 2/3 y Zorro T2).</summary>
        public int StartCharge;

        /// <summary>Míticos 4: cada slot arranca con la mitad de su costo.</summary>
        public bool StartChargeHalf;

        /// <summary>Monedas extra por pedido servido (Café 2/3).</summary>
        public int OrderCoinBonus;

        /// <summary>Monedas extra por resolución con 3+ cascadas (Espectáculo 3 + contrato).</summary>
        public int CascadeCoinBonus;

        /// <summary>Paciencia × de la party (Espectáculo 2/3). El contrato lo aplica el piso.</summary>
        public float PatienceMultiplier = 1f;

        /// <summary>Reducción de cooldown a los miembros Control (Control 2).</summary>
        public int ControlCooldownReduction;

        /// <summary>Gate de ultimate ya resuelto (None si no hay).</summary>
        public UltimateInfo Ultimate = UltimateInfo.None;

        /// <summary>False cuando el contrato apaga los ultimates (Desayuno Real).</summary>
        public bool UltimateEnabled = true;

        public static PartyModifiers Neutral() => new PartyModifiers();

        /// <summary>Costo de carga: max(4, round((base − flat) × multiplicadores)).</summary>
        public int ChargeCost(int baseCost)
        {
            double raw = (baseCost - ChargeCostFlat) * ChargeCostMultiplier;
            int cost = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
            return cost < 4 ? 4 : cost;
        }

        /// <summary>Ganancia de carga: round(unidades × multiplicador de Servicio).</summary>
        public int ChargeGain(int units)
        {
            if (units <= 0) return 0;
            double raw = units * ChargeGainMultiplier;
            int gain = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
            return gain < 1 ? 1 : gain;
        }

        /// <summary>Estrellas del slot según la party que originó estos modificadores.</summary>
        public int StarsAt(int slot, int fallback = WaifuStars.Min)
        {
            if (slot < 0 || Party == null || slot >= Party.Count || Party[slot] == null) return fallback;
            return WaifuStars.Clamp(Party[slot].Stars);
        }

        /// <summary>Carga inicial del slot según sus estrellas y su costo efectivo.</summary>
        public int StartChargeFor(int chargeCost)
        {
            int start = StartCharge;
            if (StartChargeHalf && chargeCost > 0) start += chargeCost / 2;
            return start < 0 ? 0 : start;
        }

        /// <summary>Luna de Sangre: la party gana +1 potencia por el resto de la run.</summary>
        public void AddPotency(int amount)
        {
            if (amount == 0) return;
            PotencyBonus += amount;
            if (PotencyBonus < 0) PotencyBonus = 0;
        }
    }
}
