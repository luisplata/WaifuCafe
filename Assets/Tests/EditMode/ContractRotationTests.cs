using NUnit.Framework;
using WaifuCafe.Core.Meta;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// E6: rotación del contrato destacado. El modelo recibe sólo ids jugables (los
    /// locked nunca entran) y el cursor persistido; el featured siempre es jugable y
    /// avanza con wrap.
    /// </summary>
    public class ContractRotationTests
    {
        private static readonly string[] TwoPlayables = { "rush-mediodia", "noche-brujas" };
        private static readonly string[] LockedIds = { "hora-te", "desayuno-real" };

        [Test]
        public void FeaturedBelongsToPlayableSet()
        {
            string featured = ContractRotation.NextFeatured(TwoPlayables, "rush-mediodia");

            Assert.AreEqual("noche-brujas", featured);
            CollectionAssert.Contains(TwoPlayables, featured);
            CollectionAssert.DoesNotContain(LockedIds, featured);
        }

        [Test]
        public void NoHistory_ReturnsFirstPlayable_TwiceStable()
        {
            string first = ContractRotation.NextFeatured(TwoPlayables, null);
            string second = ContractRotation.NextFeatured(TwoPlayables, null);

            Assert.AreEqual("rush-mediodia", first);
            Assert.AreEqual(first, second, "Sin historial el resultado no puede cambiar entre llamadas");
        }

        [Test]
        public void TwoPlayables_Alternate()
        {
            Assert.AreEqual("noche-brujas", ContractRotation.NextFeatured(TwoPlayables, "rush-mediodia"));
        }

        [Test]
        public void TwoPlayables_Wrap()
        {
            Assert.AreEqual("rush-mediodia", ContractRotation.NextFeatured(TwoPlayables, "noche-brujas"));
        }

        [Test]
        public void LockedCursor_FallsBackToFirstPlayable()
        {
            Assert.AreEqual("rush-mediodia", ContractRotation.NextFeatured(TwoPlayables, "hora-te"));
            Assert.AreEqual("rush-mediodia", ContractRotation.NextFeatured(TwoPlayables, "desayuno-real"));
        }

        [Test]
        public void UnknownOrBlankCursor_FallsBackToFirst()
        {
            Assert.AreEqual("rush-mediodia", ContractRotation.NextFeatured(TwoPlayables, string.Empty));
            Assert.AreEqual("rush-mediodia", ContractRotation.NextFeatured(TwoPlayables, null));
            Assert.AreEqual("rush-mediodia", ContractRotation.NextFeatured(TwoPlayables, "ghost"));
        }

        [Test]
        public void NoPlayables_ReturnsNull()
        {
            Assert.IsNull(ContractRotation.NextFeatured(null, "rush-mediodia"));
            Assert.IsNull(ContractRotation.NextFeatured(new string[0], "rush-mediodia"));
        }

        [Test]
        public void SinglePlayable_ReturnsItself()
        {
            var single = new[] { "rush-mediodia" };

            Assert.AreEqual("rush-mediodia", ContractRotation.NextFeatured(single, null));
            Assert.AreEqual("rush-mediodia", ContractRotation.NextFeatured(single, "rush-mediodia"));
        }

        [Test]
        public void BlankEntriesIgnored()
        {
            var withBlanks = new[] { null, "  ", "rush-mediodia", string.Empty, "noche-brujas" };

            Assert.AreEqual("rush-mediodia", ContractRotation.NextFeatured(withBlanks, null));
            Assert.AreEqual("noche-brujas", ContractRotation.NextFeatured(withBlanks, "rush-mediodia"));
            Assert.IsNull(ContractRotation.NextFeatured(new[] { null, "", "  " }, "rush-mediodia"),
                "Una lista sólo de blancos no tiene jugables");
        }

        [Test]
        public void CursorIsCaseInsensitive()
        {
            Assert.AreEqual("noche-brujas", ContractRotation.NextFeatured(TwoPlayables, "RUSH-MEDIODIA"));
            Assert.AreEqual("rush-mediodia", ContractRotation.NextFeatured(TwoPlayables, "Noche-Brujas"));
        }
    }
}
