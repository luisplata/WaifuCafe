using System.Collections.Generic;

namespace WaifuCafe.Core
{
    /// <summary>
    /// Carga compartida por elemento: un medidor por plato, no por waifu. Dos waifus del
    /// mismo elemento leen el mismo valor; cada una se considera lista contra su propio
    /// <c>chargeCost</c>. Puro dato: no dispara skills ni conoce definiciones.
    /// </summary>
    public sealed class ElementChargeMeter
    {
        private readonly Dictionary<Dish, int> _charge = new Dictionary<Dish, int>();

        /// <summary>Unidades acumuladas para un elemento (0 si nunca se entrego).</summary>
        public int this[Dish element] => _charge.TryGetValue(element, out int value) ? value : 0;

        public void Add(Dish element, int units)
        {
            if (units <= 0) return;
            _charge[element] = this[element] + units;
        }

        /// <summary>Suma las unidades de cada plato de una resolucion.</summary>
        public void Add(ResolveResult result)
        {
            if (result.Units == null) return;
            foreach (KeyValuePair<Dish, int> pair in result.Units) Add(pair.Key, pair.Value);
        }

        public bool IsReady(Dish element, int cost) => cost > 0 && this[element] >= cost;

        public void Reset() => _charge.Clear();
    }
}
