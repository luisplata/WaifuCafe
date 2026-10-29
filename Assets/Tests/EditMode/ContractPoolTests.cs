using System.Collections.Generic;
using NUnit.Framework;
using WaifuCafe.Core.Meta;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// E6/contracts-pool: composición del pool visible. El modelo recibe sólo ids
    /// jugables y el cursor persistido; sin cursor arranca en el primero, con cursor
    /// avanza al siguiente con wrap y nunca repite ids cuando el tamaño cubre el ciclo.
    /// </summary>
    public class ContractPoolTests
    {
        private static readonly string[] Five =
        {
            "rush-mediodia", "noche-brujas", "hora-punta", "antojo-dulce", "cafe-de-barrio",
        };

        [Test]
        public void NoHistory_ReturnsFirstN()
        {
            var pool = new List<string>(ContractPool.Build(Five, null, 3));

            CollectionAssert.AreEqual(new[] { "rush-mediodia", "noche-brujas", "hora-punta" }, pool);
        }

        [Test]
        public void KnownCursor_StartsAfterCursor()
        {
            var pool = new List<string>(ContractPool.Build(Five, "noche-brujas", 3));

            CollectionAssert.AreEqual(new[] { "hora-punta", "antojo-dulce", "cafe-de-barrio" }, pool);
        }

        [Test]
        public void KnownCursor_WrapsAtEnd()
        {
            var pool = new List<string>(ContractPool.Build(Five, "cafe-de-barrio", 3));

            CollectionAssert.AreEqual(new[] { "rush-mediodia", "noche-brujas", "hora-punta" }, pool);
        }

        [Test]
        public void SizeAtLeastCount_ReturnsWholeCycleOnce()
        {
            var exact = new List<string>(ContractPool.Build(Five, null, Five.Length));
            var bigger = new List<string>(ContractPool.Build(Five, "antojo-dulce", 12));

            CollectionAssert.AreEquivalent(Five, exact);
            Assert.AreEqual(Five.Length, exact.Count);
            CollectionAssert.AreEquivalent(Five, bigger);
            Assert.AreEqual(Five.Length, bigger.Count);
            CollectionAssert.AllItemsAreUnique(bigger);
        }

        [Test]
        public void UnknownCursor_ReturnsFirstN()
        {
            CollectionAssert.AreEqual(
                new List<string>(ContractPool.Build(Five, null, 2)),
                new List<string>(ContractPool.Build(Five, "ghost", 2)));
            CollectionAssert.AreEqual(
                new[] { "rush-mediodia", "noche-brujas" },
                new List<string>(ContractPool.Build(Five, string.Empty, 2)));
        }

        [Test]
        public void EmptyOrNullList_ReturnsEmpty()
        {
            Assert.IsEmpty(ContractPool.Build(null, "rush-mediodia", 3));
            Assert.IsEmpty(ContractPool.Build(new string[0], "rush-mediodia", 3));
            Assert.IsEmpty(ContractPool.Build(new[] { null, "  " }, "rush-mediodia", 3));
        }

        [Test]
        public void NonPositiveSize_ReturnsEmpty()
        {
            Assert.IsEmpty(ContractPool.Build(Five, null, 0));
            Assert.IsEmpty(ContractPool.Build(Five, null, -4));
        }

        [Test]
        public void BlankIdsIgnored()
        {
            var withBlanks = new[] { null, "  ", "rush-mediodia", string.Empty, "noche-brujas" };

            CollectionAssert.AreEqual(
                new[] { "rush-mediodia", "noche-brujas" },
                new List<string>(ContractPool.Build(withBlanks, null, 3)));
            CollectionAssert.AreEqual(
                new[] { "noche-brujas", "rush-mediodia" },
                new List<string>(ContractPool.Build(withBlanks, "rush-mediodia", 3)));
        }

        [Test]
        public void CursorIsCaseInsensitive()
        {
            CollectionAssert.AreEqual(
                new[] { "noche-brujas", "hora-punta" },
                new List<string>(ContractPool.Build(Five, "RUSH-MEDIODIA", 2)));
        }

        [Test]
        public void DeterministicBetweenCalls()
        {
            var first = new List<string>(ContractPool.Build(Five, "hora-punta", 3));
            var second = new List<string>(ContractPool.Build(Five, "hora-punta", 3));

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void SinglePlayable_ReturnsItself()
        {
            CollectionAssert.AreEqual(new[] { "rush-mediodia" },
                new List<string>(ContractPool.Build(new[] { "rush-mediodia" }, "rush-mediodia", 3)));
        }
    }
}
