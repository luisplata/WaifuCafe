using System;
using System.Collections.Generic;

namespace WaifuCafe.Core
{
    /// <summary>Una cantidad de unidades de un plato aplicada a un pedido concreto.</summary>
    public readonly struct OrderFill
    {
        public readonly Order Order;
        public readonly Dish Dish;
        public readonly int Units;

        public OrderFill(Order order, Dish dish, int units)
        {
            Order = order;
            Dish = dish;
            Units = units;
        }

        public override string ToString() => $"{Order.Id}:{Dish}+{Units}";
    }

    /// <summary>
    /// Resultado de entregar unidades: a quien se aplicaron, que pedidos se completaron,
    /// que entro al banco y que se descarto. Son datos de salida para que la vista anime
    /// despues; no hay callbacks ni eventos de Unity.
    /// </summary>
    public readonly struct Delivery
    {
        private static readonly IReadOnlyList<OrderFill> NoFills = Array.Empty<OrderFill>();
        private static readonly IReadOnlyList<Order> NoOrders = Array.Empty<Order>();
        private static readonly IReadOnlyDictionary<Dish, int> NoBanked = new Dictionary<Dish, int>();
        private static readonly IReadOnlyDictionary<Dish, int> NoDiscarded = new Dictionary<Dish, int>();

        public static Delivery Empty => new Delivery(NoFills, NoOrders, NoBanked, NoDiscarded);

        /// <summary>Aplicaciones en orden de urgencia (urgente primero, derrame despues).</summary>
        public readonly IReadOnlyList<OrderFill> Used;

        /// <summary>Pedidos que llegaron a 0 con esta entrega, en orden de aparicion.</summary>
        public readonly IReadOnlyList<Order> Completed;

        /// <summary>Unidades que quedaron en el banco, por plato.</summary>
        public readonly IReadOnlyDictionary<Dish, int> Banked;

        /// <summary>Unidades que se perdieron porque el banco estaba lleno, por plato.</summary>
        public readonly IReadOnlyDictionary<Dish, int> Discarded;

        public Delivery(IReadOnlyList<OrderFill> used, IReadOnlyList<Order> completed,
            IReadOnlyDictionary<Dish, int> banked, IReadOnlyDictionary<Dish, int> discarded)
        {
            Used = used ?? NoFills;
            Completed = completed ?? NoOrders;
            Banked = banked ?? NoBanked;
            Discarded = discarded ?? NoDiscarded;
        }

        public int BankedTotal => Sum(Banked);

        public int DiscardedTotal => Sum(Discarded);

        public bool IsEmpty => Used.Count == 0 && Banked.Count == 0 && Discarded.Count == 0;

        private static int Sum(IReadOnlyDictionary<Dish, int> counts)
        {
            int total = 0;
            foreach (KeyValuePair<Dish, int> pair in counts) total += pair.Value;
            return total;
        }
    }
}
