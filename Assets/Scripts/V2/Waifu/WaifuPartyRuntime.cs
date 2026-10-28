using System;
using WaifuCafe.Core;
using WaifuCafe.Core.Meta;

namespace V2.Waifu
{
    /// <summary>
    /// Estado de la party del dia: las definiciones elegidas, la carga de CADA waifu y los
    /// modificadores de sinergia. Un plato acreditado reparte unidades a todos los slots de
    /// ese elemento (con la ganancia y la pasiva ExtraCharge de cada uno), pero cada uno
    /// tiene su propio tanque: disparar una waifu no toca a sus companeras. Las unidades
    /// ganadas en la run nunca bajan: de ahi sale el nivel 1/2/3 del kit.
    /// </summary>
    public sealed class WaifuPartyRuntime
    {
        public const int MaxSlots = 4;

        private readonly WaifuDefinitionSO[] _slots;
        private readonly int[] _cost;
        private readonly int[] _earned;

        public WaifuCharge Charges { get; } = new WaifuCharge();

        /// <summary>Sinergia de la party (neutral en free-play: cero cambios de reglas).</summary>
        public PartyModifiers Modifiers { get; }

        public WaifuPartyRuntime(WaifuDefinitionSO[] party, PartyModifiers modifiers = null)
        {
            Modifiers = modifiers ?? PartyModifiers.Neutral();

            int count = party == null ? 0 : Math.Min(MaxSlots, party.Length);
            _slots = new WaifuDefinitionSO[count];
            _cost = new int[count];
            _earned = new int[count];

            var elements = new Dish[count];
            for (int i = 0; i < count; i++)
            {
                _slots[i] = party[i];
                elements[i] = party[i] != null ? party[i].Element : Dish.Desayuno;
                _cost[i] = party[i] != null ? Modifiers.ChargeCost(party[i].ChargeCost) : 0;
            }

            Charges.Configure(elements);
            ApplyStartCharge();
        }

        public int SlotCount => _slots.Length;

        public WaifuDefinitionSO this[int slot] =>
            slot >= 0 && slot < _slots.Length ? _slots[slot] : null;

        /// <summary>Costo de carga efectivo del slot (facción/contrato ya aplicados).</summary>
        public int ChargeCost(int slot) => slot >= 0 && slot < _cost.Length ? _cost[slot] : 0;

        /// <summary>Unidades acumuladas en el tanque propio de ese slot.</summary>
        public int AmountFor(int slot) => Charges[slot];

        /// <summary>Unidades ganadas en la run (nunca baja; manda el nivel del kit).</summary>
        public int EarnedFor(int slot) => slot >= 0 && slot < _earned.Length ? _earned[slot] : 0;

        /// <summary>Nivel in-run del kit: 1 en 0–5 unidades, 2 en 6–11, 3 desde 12.</summary>
        public int LevelFor(int slot) => WaifuStars.LevelFromUnits(EarnedFor(slot));

        /// <summary>Estrellas actuales del slot (perfil); sin party, las del asset.</summary>
        public int StarsFor(int slot)
        {
            WaifuDefinitionSO def = this[slot];
            return Modifiers.StarsAt(slot, def != null ? def.BaseStars : WaifuStars.Min);
        }

        /// <summary>Potencia efetiva: base + estrellas + nivel + sinergia (Luna incluida).</summary>
        public int PotencyFor(int slot)
        {
            WaifuDefinitionSO def = this[slot];
            if (def == null) return 0;
            return WaifuStars.Potency(def.Potency, StarsFor(slot), LevelFor(slot)) + Modifiers.PotencyBonus;
        }

        public bool IsReady(int slot)
        {
            WaifuDefinitionSO def = this[slot];
            return def != null && Charges.IsReady(slot, ChargeCost(slot));
        }

        /// <summary>
        /// Acredita unidades del plato a cada slot de ese elemento con SU ganancia:
        /// multiplicador de Servicio y +1 de la pasiva ExtraCharge.
        /// </summary>
        public void Charge(Dish dish, int units)
        {
            if (units <= 0) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                WaifuDefinitionSO def = _slots[i];
                if (def == null || def.Element != dish) continue;

                int gain = Modifiers.ChargeGain(units);
                if (def.PassiveId == PassiveId.ExtraCharge) gain += 1;

                Charges.AddToSlot(i, gain);
                _earned[i] += gain;
            }
        }

        public void Charge(ResolveResult result)
        {
            if (result.Units == null) return;
            foreach (var pair in result.Units) Charge(pair.Key, pair.Value);
        }

        /// <summary>Gasta solo el tanque de la waifu que dispara.</summary>
        public int Spend(int slot)
        {
            if (this[slot] == null) return 0;
            return Charges.Spend(slot, ChargeCost(slot));
        }

        public void Reset() => Charges.Reset();

        private void ApplyStartCharge()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) continue;

                int start = Modifiers.StartChargeFor(_cost[i]);
                if (start > 0) Charges.AddToSlot(i, start);
            }
        }
    }
}
