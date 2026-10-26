using NUnit.Framework;
using WaifuCafe.Core;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// T11: la fuente es compartida, el tanque es por waifu. Un delivery de un plato sube
    /// a TODAS las waifus de ese elemento en su propia barra; disparar una solo gasta la
    /// suya y cada una se marca lista con su propio costo.
    /// </summary>
    public class WaifuChargeTests
    {
        [Test]
        public void T11_ChargeIsPerWaifu_SharedSourceSeparateTanks_SpendOnlyOwn()
        {
            var charge = new WaifuCharge();
            charge.Configure(new[] { Dish.Bebida, Dish.Bebida, Dish.Almuerzo });

            const int hanaCost = 12;
            const int rikaCost = 14;

            // Un delivery de Bebida acredita a las DOS waifus de Bebida, no a Almuerzo.
            charge.Add(Dish.Bebida, 3);
            Assert.AreEqual(3, charge[0], "Rika debe recibir su parte de Bebida");
            Assert.AreEqual(3, charge[1], "Hana debe recibir su parte de Bebida");
            Assert.AreEqual(0, charge[2], "Almuerzo no debe cargarse con Bebida");

            charge.Add(Dish.Bebida, 9); // 12 unidades para ambas
            Assert.AreEqual(12, charge[0]);
            Assert.AreEqual(12, charge[1]);
            Assert.IsTrue(charge.IsReady(1, hanaCost), "Hana (12) llega a READY antes");
            Assert.IsFalse(charge.IsReady(0, rikaCost), "Rika (14) todavia no");

            // Disparar Hana gasta SOLO su tanque: Rika queda intacta.
            Assert.AreEqual(12, charge.Spend(1, hanaCost), "Hana debio gastar sus 12");
            Assert.AreEqual(0, charge[1], "El tanque de Hana queda vacio");
            Assert.AreEqual(12, charge[0], "El tanque de Rika NO se toca");

            // Un delivery posterior reacredita parejo a las dos.
            charge.Add(Dish.Bebida, 4);
            Assert.AreEqual(16, charge[0]);
            Assert.AreEqual(4, charge[1]);
            Assert.IsTrue(charge.IsReady(0, rikaCost), "Rika ahora si esta lista");

            charge.Reset();
            Assert.AreEqual(0, charge[0]);
            Assert.AreEqual(0, charge[2]);
        }
    }
}
