using NUnit.Framework;
using WaifuCafe.Core;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// T11: la carga vive por elemento. Dos waifus del mismo plato leen el mismo medidor
    /// y cada una se marca lista con su propio costo.
    /// </summary>
    public class ElementChargeMeterTests
    {
        [Test]
        public void T11_SharedChargePerElement_ReadiesByOwnCost_AndDoesNotLeak()
        {
            var meter = new ElementChargeMeter();

            meter.Add(Dish.Almuerzo, 4);
            meter.Add(Dish.Bebida, 3);

            Assert.AreEqual(4, meter[Dish.Almuerzo], "Almuerzo no acumulo sus unidades");
            Assert.AreEqual(3, meter[Dish.Bebida], "Bebida no acumulo sus unidades");
            Assert.AreEqual(0, meter[Dish.Postre], "un elemento sin entregas no debe cargarse");

            // Dos waifus de Bebida: costos distintos sobre el MISMO medidor.
            const int rikaCost = 14;
            const int hanaCost = 12;

            Assert.IsFalse(meter.IsReady(Dish.Bebida, hanaCost), "3 unidades no alcanzan para 12");
            Assert.IsFalse(meter.IsReady(Dish.Bebida, rikaCost), "3 unidades no alcanzan para 14");

            meter.Add(Dish.Bebida, 9); // 12: la de menor costo llega antes
            Assert.AreEqual(12, meter[Dish.Bebida]);
            Assert.IsTrue(meter.IsReady(Dish.Bebida, hanaCost), "Hana (12) debe estar lista primero");
            Assert.IsFalse(meter.IsReady(Dish.Bebida, rikaCost), "Rika (14) todavia no");

            meter.Add(Dish.Almuerzo, 6); // 10: no toca Bebida
            Assert.AreEqual(10, meter[Dish.Almuerzo]);
            Assert.IsFalse(meter.IsReady(Dish.Bebida, rikaCost), "cargar otro elemento no debe sumar a Bebida");
            Assert.AreEqual(0, meter[Dish.Postre], "Postre sigue en cero");
        }
    }
}
