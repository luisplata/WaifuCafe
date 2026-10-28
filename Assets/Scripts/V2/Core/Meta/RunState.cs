using System;
using WaifuCafe.Core;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Estado mutable de una run en curso. Acumula lo que reportan las capas de Unity
    /// (pedidos servidos, perdidos, skills, fichas limpias por plato, monedas, tiempo) y
    /// entrega fotos inmutables al evaluador. Sin Unity.
    /// </summary>
    public sealed class RunState
    {
        private static readonly int DishCount = ContractProgress.DishCount;

        private readonly int[] _cleared = new int[DishCount];

        public RunState(ContractDefinition contract)
        {
            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        }

        public ContractDefinition Contract { get; }

        public int ServedOrders;
        public int VipOrders;
        public int LostOrders;
        public int SkillsCast;
        public int Coins;
        public float ElapsedSeconds;

        /// <summary>Id/etiqueta de la ultimate usada (E4 la setea; vacío = ninguna).</summary>
        public string UltimateUsed = string.Empty;

        /// <summary>Suma las fichas limpias por plato de una resolución del tablero.</summary>
        public void AddCleared(ResolveResult result)
        {
            if (result.ClearedByDish == null) return;
            foreach (var pair in result.ClearedByDish) AddCleared(pair.Key, pair.Value);
        }

        public void AddCleared(Dish dish, int units)
        {
            if (units <= 0) return;
            int index = (int)dish;
            if (index < 0 || index >= _cleared.Length) return;
            _cleared[index] += units;
        }

        public int Cleared(Dish dish)
        {
            int index = (int)dish;
            return index >= 0 && index < _cleared.Length ? _cleared[index] : 0;
        }

        public ContractProgress Progress => new ContractProgress(
            ServedOrders, SkillsCast, VipOrders, LostOrders, Coins, ElapsedSeconds, _cleared);
    }
}
