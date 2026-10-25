using System;
using System.Collections.Generic;

namespace WaifuCafe.Core
{
    /// <summary>
    /// Un pedido sentado: que platos pide y cuanto falta de cada uno. Dato puro, sin Unity.
    /// <see cref="Seq"/> es el indice de llegada (menor = mas antiguo = mas urgente).
    /// </summary>
    public sealed class Order
    {
        private const int DishCount = 5;

        private readonly int[] _required = new int[DishCount];
        private readonly int[] _delivered = new int[DishCount];
        private readonly List<Dish> _dishes = new List<Dish>(2);

        public int Id { get; }

        /// <summary>Antiguedad: lo asigna quien crea el pedido segun el orden de llegada.</summary>
        public int Seq { get; }

        /// <summary>Platos pedidos, en orden de enum (determinista).</summary>
        public IReadOnlyList<Dish> Dishes => _dishes;

        /// <summary>Unidades pendientes de todos los platos.</summary>
        public int RemainingTotal
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _dishes.Count; i++) total += Remaining(_dishes[i]);
                return total;
            }
        }

        public bool IsComplete => RemainingTotal == 0;

        public Order(int id, int seq, params (Dish Dish, int Units)[] needs)
        {
            if (needs == null || needs.Length == 0)
                throw new ArgumentException("Un pedido necesita al menos un plato.", nameof(needs));

            Id = id;
            Seq = seq;

            foreach ((Dish dish, int units) in needs)
            {
                if (units <= 0)
                    throw new ArgumentOutOfRangeException(nameof(needs), "Las unidades pedidas deben ser positivas.");

                if (_required[(int)dish] == 0) _dishes.Add(dish);
                _required[(int)dish] += units;
            }

            _dishes.Sort();
        }

        public int Required(Dish dish) => _required[(int)dish];

        public int Delivered(Dish dish) => _delivered[(int)dish];

        public int Remaining(Dish dish) => _required[(int)dish] - _delivered[(int)dish];

        /// <summary>
        /// Suma unidades al pedido. Devuelve las que acepto: nunca mas de lo que falta
        /// de ese plato, asi el sobrante queda libre para el siguiente o el banco.
        /// </summary>
        public int Accept(Dish dish, int units)
        {
            if (units <= 0) return 0;

            int accepted = Math.Min(units, Remaining(dish));
            _delivered[(int)dish] += accepted;
            return accepted;
        }

        public override string ToString() => $"Order#{Id}(seq {Seq}, {RemainingTotal} restantes)";
    }
}
