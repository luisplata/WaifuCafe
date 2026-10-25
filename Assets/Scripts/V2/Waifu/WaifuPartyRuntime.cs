using System;
using WaifuCafe.Core;

namespace V2.Waifu
{
    /// <summary>
    /// Estado de la party del dia: las definiciones elegidas y UN medidor por elemento.
    /// Cargar un plato suma al medidor compartido; cada slot se marca listo con su propio
    /// <c>chargeCost</c>. No ejecuta skills: eso es PR7.
    /// </summary>
    public sealed class WaifuPartyRuntime
    {
        public const int MaxSlots = 4;

        private readonly WaifuDefinitionSO[] _slots;

        public ElementChargeMeter Charges { get; } = new ElementChargeMeter();

        public WaifuPartyRuntime(WaifuDefinitionSO[] party)
        {
            int count = party == null ? 0 : Math.Min(MaxSlots, party.Length);
            _slots = new WaifuDefinitionSO[count];
            for (int i = 0; i < count; i++) _slots[i] = party[i];
        }

        public int SlotCount => _slots.Length;

        public WaifuDefinitionSO this[int slot] =>
            slot >= 0 && slot < _slots.Length ? _slots[slot] : null;

        /// <summary>Unidades acumuladas en el elemento de ese slot.</summary>
        public int AmountFor(int slot)
        {
            WaifuDefinitionSO def = this[slot];
            return def == null ? 0 : Charges[def.Element];
        }

        public bool IsReady(int slot)
        {
            WaifuDefinitionSO def = this[slot];
            return def != null && Charges.IsReady(def.Element, def.ChargeCost);
        }

        public void Charge(Dish dish, int units) => Charges.Add(dish, units);

        public void Charge(ResolveResult result) => Charges.Add(result);

        public void Reset() => Charges.Reset();
    }
}
