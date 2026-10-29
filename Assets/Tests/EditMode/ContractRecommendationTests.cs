using System.Collections.Generic;
using NUnit.Framework;
using WaifuCafe.Core.Meta;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// E6.2: el recomendado es del juego al jugador y sólo existe cuando hay fundamento.
    /// Sin mayoría de facción en la party, o sin contrato de esa facción en el pool, no
    /// hay recomendación y el listón no aparece.
    /// </summary>
    public sealed class ContractRecommendationTests
    {
        private static IReadOnlyList<WaifuFaction> Party(params WaifuFaction[] factions) => factions;

        private static IReadOnlyList<ContractCandidate> Pool(params (string Id, WaifuFaction Faction)[] entries)
        {
            var list = new List<ContractCandidate>(entries.Length);
            foreach (var entry in entries) list.Add(new ContractCandidate(entry.Id, entry.Faction));
            return list;
        }

        [Test]
        public void ThreeCafeOneMiticos_DominantIsCafe()
        {
            var dominant = ContractRecommendation.DominantFaction(Party(
                WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Miticos));

            Assert.AreEqual(WaifuFaction.Cafe, dominant);
        }

        [Test]
        public void TwoTwo_SplitHasNoDominantFaction()
        {
            var dominant = ContractRecommendation.DominantFaction(Party(
                WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Miticos, WaifuFaction.Miticos));

            Assert.IsNull(dominant);
        }

        [Test]
        public void EmptyParty_HasNoDominantFaction()
        {
            Assert.IsNull(ContractRecommendation.DominantFaction(new List<WaifuFaction>()));
            Assert.IsNull(ContractRecommendation.DominantFaction(null));
        }

        [Test]
        public void SingleMember_IsItsOwnMajority()
        {
            Assert.AreEqual(WaifuFaction.Miticos,
                ContractRecommendation.DominantFaction(Party(WaifuFaction.Miticos)));
        }

        [Test]
        public void RecommendsTheContractMatchingTheDominantFaction()
        {
            string id = ContractRecommendation.Recommend(
                Pool(("noche-brujas", WaifuFaction.Miticos), ("hora-punta", WaifuFaction.Cafe)),
                Party(WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Miticos));

            Assert.AreEqual("hora-punta", id);
        }

        [Test]
        public void NoMatchingContract_NoRecommendation()
        {
            string id = ContractRecommendation.Recommend(
                Pool(("noche-brujas", WaifuFaction.Miticos), ("sobremesa", WaifuFaction.Miticos)),
                Party(WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Miticos));

            Assert.IsNull(id);
        }

        [Test]
        public void SplitParty_NoRecommendationEvenWithMatch()
        {
            string id = ContractRecommendation.Recommend(
                Pool(("hora-punta", WaifuFaction.Cafe)),
                Party(WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Miticos, WaifuFaction.Miticos));

            Assert.IsNull(id);
        }

        [Test]
        public void FirstMatchWins_Deterministic()
        {
            string id = ContractRecommendation.Recommend(
                Pool(("noche-brujas", WaifuFaction.Miticos), ("sobremesa", WaifuFaction.Miticos)),
                Party(WaifuFaction.Miticos, WaifuFaction.Miticos, WaifuFaction.Miticos, WaifuFaction.Cafe));

            Assert.AreEqual("noche-brujas", id);
        }

        [Test]
        public void EmptyPool_NoRecommendation()
        {
            string id = ContractRecommendation.Recommend(
                new List<ContractCandidate>(),
                Party(WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Cafe));

            Assert.IsNull(id);
        }

        [Test]
        public void BlankId_IsSkipped()
        {
            string id = ContractRecommendation.Recommend(
                Pool(("  ", WaifuFaction.Cafe), ("hora-punta", WaifuFaction.Cafe)),
                Party(WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Cafe, WaifuFaction.Cafe));

            Assert.AreEqual("hora-punta", id);
        }

        [Test]
        public void EspectaculoMajority_RecommendsEspectaculo()
        {
            string id = ContractRecommendation.Recommend(
                Pool(("hora-te", WaifuFaction.Espectaculo), ("hora-punta", WaifuFaction.Cafe)),
                Party(WaifuFaction.Espectaculo, WaifuFaction.Espectaculo,
                    WaifuFaction.Espectaculo, WaifuFaction.Cafe));

            Assert.AreEqual("hora-te", id);
        }
    }
}
