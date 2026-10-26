using System;
using WaifuCafe.Core;

namespace V2.Waifu
{
    /// <summary>
    /// Estado de la party del dia: las definiciones elegidas y la carga de CADA waifu.
    /// Un plato acreditado reparte unidades a todos los slots de ese elemento, pero cada
    /// uno tiene su propio tanque: disparar una waifu no toca a sus companeras.
    /// </summary>
    public sealed class WaifuPartyRuntime
    {
        public const int MaxSlots = 4;

        private readonly WaifuDefinitionSO[] _slots;

        public WaifuCharge Charges { get; } = new WaifuCharge();

        public WaifuPartyRuntime(WaifuDefinitionSO[] party)
        {
            int count = party == null ? 0 : Math.Min(MaxSlots, party.Length);
            _slots = new WaifuDefinitionSO[count];
            var elements = new Dish[count];
            for (int i = 0; i < count; i++)
            {
                _slots[i] = party[i];
                elements[i] = party[i] != null ? party[i].Element : Dish.Desayuno;
            }

            Charges.Configure(elements);
        }

        public int SlotCount => _slots.Length;

        public WaifuDefinitionSO this[int slot] =>
            slot >= 0 && slot < _slots.Length ? _slots[slot] : null;

        /// <summary>Unidades acumuladas en el tanque propio de ese slot.</summary>
        public int AmountFor(int slot) => Charges[slot];

        public bool IsReady(int slot)
        {
            WaifuDefinitionSO def = this[slot];
            return def != null && Charges.IsReady(slot, def.ChargeCost);
        }

        public void Charge(Dish dish, int units) => Charges.Add(dish, units);

        public void Charge(ResolveResult result) => Charges.Add(result);

        /// <summary>Gasta solo el tanque de la waifu que dispara.</summary>
        public int Spend(int slot)
        {
            WaifuDefinitionSO def = this[slot];
            return def == null ? 0 : Charges.Spend(slot, def.ChargeCost);
        }

        public void Reset() => Charges.Reset();
    }
}
