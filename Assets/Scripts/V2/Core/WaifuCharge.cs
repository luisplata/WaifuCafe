using System.Collections.Generic;

namespace WaifuCafe.Core
{
    /// <summary>
    /// Carga POR WAIFU: cada slot tiene su propio tanque. Un match o delivery de un plato
    /// acredita las mismas unidades a TODOS los slots de ese elemento, pero cada uno las
    /// acumula por separado; disparar una waifu gasta solo su tanque.
    /// Puro dato: no conoce definiciones ni dispara skills.
    /// </summary>
    public sealed class WaifuCharge
    {
        private readonly List<Dish> _elements = new List<Dish>();
        private readonly List<int> _charge = new List<int>();

        /// <summary>Fija el elemento de cada slot. Arranca todos los tanques en cero.</summary>
        public void Configure(IReadOnlyList<Dish> elements)
        {
            _elements.Clear();
            _charge.Clear();
            if (elements == null) return;
            for (int i = 0; i < elements.Count; i++)
            {
                _elements.Add(elements[i]);
                _charge.Add(0);
            }
        }

        /// <summary>Unidades acumuladas en el tanque propio de un slot (0 si no existe).</summary>
        public int this[int slot] => Valid(slot) ? _charge[slot] : 0;

        /// <summary>Reparte unidades a todos los slots de ese elemento, cada uno en su tanque.</summary>
        public void Add(Dish element, int units)
        {
            if (units <= 0) return;
            for (int i = 0; i < _elements.Count; i++)
                if (_elements[i] == element) _charge[i] += units;
        }

        /// <summary>
        /// Suma unidades SOLO al tanque de ese slot (carga inicial, pasivas por waifu). No
        /// reparte por elemento como <see cref="Add(Dish,int)"/>.
        /// </summary>
        public void AddToSlot(int slot, int units)
        {
            if (!Valid(slot) || units <= 0) return;
            _charge[slot] += units;
        }

        /// <summary>Suma las unidades de cada plato de una resolucion con el reparto por elemento.</summary>
        public void Add(ResolveResult result)
        {
            if (result.Units == null) return;
            foreach (KeyValuePair<Dish, int> pair in result.Units) Add(pair.Key, pair.Value);
        }

        public bool IsReady(int slot, int cost) => cost > 0 && this[slot] >= cost;

        /// <summary>Gasta solo el tanque del slot que dispara; los demas quedan intactos.</summary>
        public int Spend(int slot, int units)
        {
            if (!Valid(slot) || units <= 0) return 0;
            int current = _charge[slot];
            int spent = current < units ? current : units;
            if (spent > 0) _charge[slot] = current - spent;
            return spent;
        }

        public void Reset()
        {
            for (int i = 0; i < _charge.Count; i++) _charge[i] = 0;
        }

        private bool Valid(int slot) => slot >= 0 && slot < _elements.Count;
    }
}
