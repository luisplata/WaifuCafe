using NUnit.Framework;
using WaifuCafe.Core.Meta;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// Tutorial guionado (E5): semilla fija, cinco pedidos scripted, derrota desactivada
    /// y victoria por pedidos servidos. Todo puro, sin Unity.
    /// </summary>
    public sealed class TutorialFlowTests
    {
        [Test]
        public void CreateRun_UsesFixedSeedFiveOrdersAndNoFail()
        {
            RunConfig config = TutorialFlow.CreateRun(new[] { "airi", "neko", "luna", "rika" });

            Assert.AreEqual(TutorialFlow.ContractId, config.Contract.Id);
            Assert.AreEqual(424242, config.Seed);
            Assert.IsFalse(config.FailEnabled);
            Assert.AreEqual(5, config.ScriptedOrders.Count);
            Assert.AreEqual(4, config.PartyIds.Count);
            Assert.AreEqual(TutorialFlow.ServedGoal, config.Contract.Goal.servedOrders);
        }

        [Test]
        public void TimeAndLostOrders_NeverDefeatTheTutorial()
        {
            ContractDefinition contract = TutorialFlow.CreateContract();
            var state = new RunState(contract)
            {
                ElapsedSeconds = 999f,
                LostOrders = 99,
            };

            ContractEvaluation evaluation = ContractEvaluator.Evaluate(contract, state);

            Assert.AreEqual(RunStatus.Playing, evaluation.Status);
        }

        [Test]
        public void FiveServedOrders_IsVictory()
        {
            ContractDefinition contract = TutorialFlow.CreateContract();
            var state = new RunState(contract) { ServedOrders = TutorialFlow.ServedGoal };

            ContractEvaluation evaluation = ContractEvaluator.Evaluate(contract, state);

            Assert.AreEqual(RunStatus.Victory, evaluation.Status);
            Assert.AreEqual(RunOutcome.GoalReached, evaluation.Outcome);
        }
    }
}
