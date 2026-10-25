using System.Collections.Generic;
using NUnit.Framework;
using WaifuCafe.Core;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// Pedidos y banco (RF-6): urgencia por antiguedad, desempate por cercania,
    /// derrame al siguiente, banco de 5 fichas en total y auto-llenado al entrar un pedido.
    /// </summary>
    public class OrderBookTests
    {
        private static Order Make(int id, int seq, Dish dish, int units) =>
            new Order(id, seq, (dish, units));

        [Test]
        public void T7_Deliver_ServesOldestFirst_AndSpillsToNext()
        {
            var book = new OrderBook();
            Order oldest = Make(1, 0, Dish.Almuerzo, 1);
            Order next = Make(2, 1, Dish.Almuerzo, 5);
            book.Add(oldest);
            book.Add(next);

            Delivery delivery = book.Deliver(Dish.Almuerzo, 4);

            Assert.AreEqual(1, oldest.Delivered(Dish.Almuerzo), "el mas antiguo recibe solo lo que le falta");
            Assert.IsTrue(oldest.IsComplete, "el mas urgente se completa con 1 unidad");
            Assert.AreEqual(3, next.Delivered(Dish.Almuerzo), "el sobrante derrama al siguiente pedido");
            Assert.IsFalse(next.IsComplete, "al siguiente todavia le faltan 2");

            CollectionAssert.AreEqual(new[] { oldest }, delivery.Completed, "solo se completa el urgente");
            Assert.AreEqual(2, delivery.Used.Count, "dos aplicaciones: urgente y derrame");
            Assert.AreEqual(0, delivery.BankedTotal, "no sobra nada para el banco");
            Assert.AreEqual(0, delivery.DiscardedTotal, "no se descarta ninguna unidad");
        }

        [Test]
        public void T8_Deliver_TieBreakPrefersOrderAlmostComplete()
        {
            var book = new OrderBook();
            // Misma antiguedad (mismo Seq): gana el que esta mas cerca de completarse.
            Order far = Make(1, 0, Dish.Bebida, 5);
            Order near = Make(2, 0, Dish.Bebida, 1);
            book.Add(far);
            book.Add(near);

            Delivery delivery = book.Deliver(Dish.Bebida, 1);

            Assert.AreEqual(1, near.Delivered(Dish.Bebida), "el desempate va al mas cerca de completar");
            Assert.AreEqual(0, far.Delivered(Dish.Bebida), "el lejano no recibe en el desempate");
            CollectionAssert.AreEqual(new[] { near }, delivery.Completed, "el casi-completo se cierra");
        }

        [Test]
        public void T9_Bank_HoldsFiveAcrossDishes_DiscardsOverflow_AndPrefillsNewOrders()
        {
            var book = new OrderBook();

            Delivery overflow = book.Deliver(Dish.Snack, 6);

            Assert.AreEqual(OrderBook.BankCapacity, book.Bank(Dish.Snack), "el banco guarda como maximo 5");
            Assert.AreEqual(5, overflow.BankedTotal, "5 unidades entraron al banco");
            Assert.AreEqual(1, overflow.DiscardedTotal, "la que no entra se descarta y se reporta");

            Delivery full = book.Deliver(Dish.Postre, 2);

            Assert.AreEqual(0, book.Bank(Dish.Postre), "la capacidad es total, no por plato");
            Assert.AreEqual(2, full.DiscardedTotal, "sin lugar en el banco, se descarta");

            Order order = Make(1, 0, Dish.Snack, 3);
            Delivery prefilled = book.Add(order);

            Assert.AreEqual(3, order.Delivered(Dish.Snack), "el banco llena el pedido nuevo");
            Assert.IsTrue(order.IsComplete, "lo que habia en el banco alcanza");
            Assert.AreEqual(2, book.Bank(Dish.Snack), "el auto-llenado descuenta del banco");
            Assert.AreEqual(1, prefilled.Used.Count, "el auto-llenado se reporta");
        }

        [Test]
        public void T10_DeliverResolveResult_AggregatesUnitsPerDish()
        {
            var book = new OrderBook();
            Order order = new Order(1, 0, (Dish.Desayuno, 2), (Dish.Snack, 2));
            book.Add(order);

            var units = new Dictionary<Dish, int> { { Dish.Snack, 3 }, { Dish.Desayuno, 1 } };
            var result = new ResolveResult(1, 4, units,
                new List<IReadOnlyList<BoardPos>> { new List<BoardPos>() },
                new List<int> { 4 }, System.Array.Empty<BoardSnapshot>());

            Delivery delivery = book.Deliver(result);

            Assert.AreEqual(1, order.Delivered(Dish.Desayuno), "Desayuno recibe su unidad");
            Assert.AreEqual(2, order.Delivered(Dish.Snack), "Snack recibe solo lo que pide");
            Assert.IsFalse(order.IsComplete, "todavia falta 1 Desayuno");
            Assert.AreEqual(1, delivery.BankedTotal, "el Snack sobrante entra al banco");
            Assert.AreEqual(1, book.Bank(Dish.Snack), "el banco refleja el sobrante");
            Assert.AreEqual(0, delivery.DiscardedTotal, "nadie descarta nada");
        }
    }
}
