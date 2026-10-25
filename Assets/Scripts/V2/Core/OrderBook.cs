using System;
using System.Collections.Generic;

namespace WaifuCafe.Core
{
    /// <summary>
    /// Los pedidos activos (un asiento cada uno) y el banco de sobrantes.
    /// Reglas: urgencia = antiguedad (desempate: el mas cerca de completarse);
    /// el sobrante derrama al siguiente pedido que necesite ese plato; si nadie lo
    /// necesita va al banco, que guarda <see cref="BankCapacity"/> fichas en total.
    /// No conoce Unity, ni la vista, ni la escena.
    /// </summary>
    public sealed class OrderBook
    {
        /// <summary>Tope del banco, sumando todos los platos.</summary>
        public const int BankCapacity = 5;

        private const int DishCount = 5;

        private readonly List<Order> _orders = new List<Order>();
        private readonly int[] _bank = new int[DishCount];

        /// <summary>Pedidos activos, en orden de llegada (los completados siguen hasta que se libere el asiento).</summary>
        public IReadOnlyList<Order> Orders => _orders;

        /// <summary>Fichas guardadas en el banco, todos los platos.</summary>
        public int BankedTotal
        {
            get
            {
                int total = 0;
                for (int i = 0; i < DishCount; i++) total += _bank[i];
                return total;
            }
        }

        /// <summary>Fichas guardadas de un plato.</summary>
        public int Bank(Dish dish) => _bank[(int)dish];

        /// <summary>
        /// Plato con mas unidades pendientes entre los pedidos activos.
        /// Null si no hay pedidos (el core no inventa un plato).
        /// </summary>
        public Dish? MostNeeded
        {
            get
            {
                Dish? best = null;
                int bestPending = 0;

                for (int d = 0; d < DishCount; d++)
                {
                    var dish = (Dish)d;
                    int pending = 0;
                    for (int i = 0; i < _orders.Count; i++) pending += _orders[i].Remaining(dish);

                    if (pending > bestPending)
                    {
                        bestPending = pending;
                        best = dish;
                    }
                }

                return best;
            }
        }

        /// <summary>
        /// Suma un pedido y lo llena con lo que haya en el banco de sus platos.
        /// Devuelve ese auto-llenado para que la vista lo anime. El pedido queda en la
        /// lista aunque el banco lo complete: liberar el asiento es de otra capa.
        /// </summary>
        public Delivery Add(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            var fill = new Builder();
            FillFromBank(order, fill);
            _orders.Add(order);
            return fill.Build();
        }

        public bool Remove(Order order) => order != null && _orders.Remove(order);

        /// <summary>Entrega todas las unidades de una resolucion, plato por plato en orden de enum.</summary>
        public Delivery Deliver(ResolveResult result)
        {
            var delivery = new Builder();

            for (int d = 0; d < DishCount; d++)
            {
                var dish = (Dish)d;
                if (result.Units.TryGetValue(dish, out int units) && units > 0)
                    DeliverCore(dish, units, delivery);
            }

            return delivery.Build();
        }

        /// <summary>Entrega unidades de un plato: urgente primero, derrame, banco y descarte.</summary>
        public Delivery Deliver(Dish dish, int units)
        {
            var delivery = new Builder();
            DeliverCore(dish, units, delivery);
            return delivery.Build();
        }

        private void DeliverCore(Dish dish, int units, Builder delivery)
        {
            if (units <= 0) return;

            int left = units;
            List<Order> candidates = Candidates(dish);

            for (int i = 0; i < candidates.Count && left > 0; i++)
            {
                Order order = candidates[i];
                int accepted = order.Accept(dish, left);
                if (accepted <= 0) continue;

                delivery.AddUsed(order, dish, accepted);
                left -= accepted;
                if (order.IsComplete) delivery.AddCompleted(order);
            }

            if (left <= 0) return;

            int room = BankCapacity - BankedTotal;
            int banked = Math.Min(left, Math.Max(0, room));
            if (banked > 0)
            {
                _bank[(int)dish] += banked;
                delivery.AddBanked(dish, banked);
            }

            int discarded = left - banked;
            if (discarded > 0) delivery.AddDiscarded(dish, discarded);
        }

        /// <summary>Pedidos que aun necesitan el plato, ordenados por urgencia.</summary>
        private List<Order> Candidates(Dish dish)
        {
            var candidates = new List<Order>(_orders.Count);
            for (int i = 0; i < _orders.Count; i++)
            {
                if (_orders[i].Remaining(dish) > 0) candidates.Add(_orders[i]);
            }

            candidates.Sort(CompareUrgency);
            return candidates;
        }

        /// <summary>Antiguedad primero; empate: menos unidades pendientes; empate: Id (determinista).</summary>
        private static int CompareUrgency(Order a, Order b)
        {
            int bySeq = a.Seq.CompareTo(b.Seq);
            if (bySeq != 0) return bySeq;

            int byRemaining = a.RemainingTotal.CompareTo(b.RemainingTotal);
            return byRemaining != 0 ? byRemaining : a.Id.CompareTo(b.Id);
        }

        private void FillFromBank(Order order, Builder delivery)
        {
            IReadOnlyList<Dish> dishes = order.Dishes;

            for (int i = 0; i < dishes.Count; i++)
            {
                Dish dish = dishes[i];
                int available = _bank[(int)dish];
                if (available <= 0) continue;

                int accepted = order.Accept(dish, available);
                if (accepted <= 0) continue;

                _bank[(int)dish] -= accepted;
                delivery.AddUsed(order, dish, accepted);
                if (order.IsComplete) delivery.AddCompleted(order);
            }
        }

        /// <summary>Acumula varias entregas por plato en un solo resultado.</summary>
        private sealed class Builder
        {
            private readonly List<OrderFill> _used = new List<OrderFill>();
            private readonly List<Order> _completed = new List<Order>();
            private readonly Dictionary<Dish, int> _banked = new Dictionary<Dish, int>();
            private readonly Dictionary<Dish, int> _discarded = new Dictionary<Dish, int>();

            public void AddUsed(Order order, Dish dish, int units) =>
                _used.Add(new OrderFill(order, dish, units));

            public void AddCompleted(Order order) => _completed.Add(order);

            public void AddBanked(Dish dish, int units) => Bump(_banked, dish, units);

            public void AddDiscarded(Dish dish, int units) => Bump(_discarded, dish, units);

            public Delivery Build() => new Delivery(_used, _completed, _banked, _discarded);

            private static void Bump(Dictionary<Dish, int> counts, Dish dish, int units)
            {
                counts.TryGetValue(dish, out int current);
                counts[dish] = current + units;
            }
        }
    }
}
