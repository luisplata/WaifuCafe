using System.Collections.Generic;
using NUnit.Framework;
using WaifuCafe.Core;
using WaifuCafe.Core.Meta;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// Reglas puras de victoria/derrota por contrato. No toca Unity: construye el estado
    /// a mano y verifica el orden de decisión (objetivo &gt; perdidos &gt; tiempo).
    /// </summary>
    public sealed class ContractEvaluatorTests
    {
        private static ContractDefinition Rush(int maxLost = 3, float timeLimit = 180f)
        {
            return new ContractDefinition
            {
                Id = "rush-mediodia",
                Title = "Rush de Mediodía",
                Goal = new ContractGoal { servedOrders = 18 },
                FailEnabled = true,
                MaxLostOrders = maxLost,
                TimeLimitSeconds = timeLimit,
            };
        }

        private static ContractDefinition Noche()
        {
            return new ContractDefinition
            {
                Id = "noche-brujas",
                Title = "Noche de Brujas",
                Goal = new ContractGoal
                {
                    skillsCast = 6,
                    clearedDish = Dish.Postre,
                    clearedDishUnits = 25,
                },
                FailEnabled = true,
                MaxLostOrders = 3,
                TimeLimitSeconds = 180f,
            };
        }

        private static RunState State(ContractDefinition contract) => new RunState(contract);

        [Test]
        public void Rush_ThirdLost_IsDefeatImmediately()
        {
            ContractDefinition contract = Rush();
            RunState state = State(contract);

            state.LostOrders = 2;
            Assert.AreEqual(RunStatus.Playing, ContractEvaluator.Evaluate(contract, state).Status,
                "dos perdidos todavia no cierran la run");

            state.LostOrders = 3;
            ContractEvaluation evaluation = ContractEvaluator.Evaluate(contract, state);
            Assert.AreEqual(RunStatus.Defeat, evaluation.Status);
            Assert.AreEqual(RunOutcome.MaxLostOrders, evaluation.Outcome,
                "Rush pierde al tercer cliente perdido");
        }

        [Test]
        public void Rush_TimeoutWithPartialGoal_IsDefeatAndKeepsCoins()
        {
            ContractDefinition contract = Rush();
            RunState state = State(contract);
            state.ServedOrders = 12;
            state.Coins = 37;
            state.ElapsedSeconds = 180f;

            ContractEvaluation evaluation = ContractEvaluator.Evaluate(contract, state);

            Assert.AreEqual(RunStatus.Defeat, evaluation.Status);
            Assert.AreEqual(RunOutcome.TimeExpired, evaluation.Outcome);
            Assert.AreEqual(37, evaluation.Progress.Coins, "el soft fail conserva las monedas parciales");
        }

        [Test]
        public void Rush_EighteenServed_IsVictory()
        {
            ContractDefinition contract = Rush();
            RunState state = State(contract);
            state.ServedOrders = 18;
            state.ElapsedSeconds = 95f;

            ContractEvaluation evaluation = ContractEvaluator.Evaluate(contract, state);

            Assert.AreEqual(RunStatus.Victory, evaluation.Status);
            Assert.AreEqual(RunOutcome.GoalReached, evaluation.Outcome);
        }

        [Test]
        public void GoalBeatsTimeout_WhenBothAreTrue()
        {
            ContractDefinition contract = Rush();
            RunState state = State(contract);
            state.ServedOrders = 18;
            state.ElapsedSeconds = 180f;

            ContractEvaluation evaluation = ContractEvaluator.Evaluate(contract, state);

            Assert.AreEqual(RunStatus.Victory, evaluation.Status,
                "cumplir el objetivo gana aunque el reloj llegue a cero");
        }

        [Test]
        public void Noche_NeedsSkillsAndPostreCleared()
        {
            ContractDefinition contract = Noche();
            RunState state = State(contract);

            state.SkillsCast = 6;
            state.AddCleared(Dish.Postre, 24);
            Assert.AreEqual(RunStatus.Playing, ContractEvaluator.Evaluate(contract, state).Status,
                "faltan fichas de Postre");

            state.AddCleared(Dish.Postre, 1);
            ContractEvaluation evaluation = ContractEvaluator.Evaluate(contract, state);
            Assert.AreEqual(RunStatus.Victory, evaluation.Status);
            Assert.AreEqual(25, evaluation.Progress.Cleared(Dish.Postre));
        }

        [Test]
        public void Noche_TimeoutWithoutGoal_IsDefeat()
        {
            ContractDefinition contract = Noche();
            RunState state = State(contract);
            state.SkillsCast = 6;
            state.AddCleared(Dish.Postre, 10);
            state.ElapsedSeconds = 180f;

            ContractEvaluation evaluation = ContractEvaluator.Evaluate(contract, state);

            Assert.AreEqual(RunStatus.Defeat, evaluation.Status);
            Assert.AreEqual(RunOutcome.TimeExpired, evaluation.Outcome);
        }

        [Test]
        public void HoraDelTe_OneLostVip_IsDefeat()
        {
            var contract = new ContractDefinition
            {
                Id = "hora-te",
                Goal = new ContractGoal { vipOrders = 6 },
                FailEnabled = true,
                MaxLostOrders = 1,
                TimeLimitSeconds = 180f,
            };
            RunState state = State(contract);
            state.VipOrders = 5;
            state.LostOrders = 1;

            ContractEvaluation evaluation = ContractEvaluator.Evaluate(contract, state);

            Assert.AreEqual(RunStatus.Defeat, evaluation.Status);
            Assert.AreEqual(RunOutcome.MaxLostOrders, evaluation.Outcome);
        }

        [Test]
        public void Tutorial_FailDisabled_NeverDefeats()
        {
            var contract = new ContractDefinition
            {
                Id = "tutorial",
                Goal = new ContractGoal { servedOrders = 5 },
                FailEnabled = false,
                MaxLostOrders = 3,
                TimeLimitSeconds = 240f,
            };
            RunState state = State(contract);
            state.LostOrders = 10;
            state.ElapsedSeconds = 999f;

            ContractEvaluation evaluation = ContractEvaluator.Evaluate(contract, state);

            Assert.AreEqual(RunStatus.Playing, evaluation.Status,
                "el run de tutorial no puede perder por tiempo ni por perdidos");
        }

        [Test]
        public void EmptyGoal_NeverWins()
        {
            var contract = new ContractDefinition { FailEnabled = true, MaxLostOrders = 3 };
            RunState state = State(contract);
            state.ServedOrders = 999;
            state.AddCleared(Dish.Postre, 999);

            Assert.AreEqual(RunStatus.Playing, ContractEvaluator.Evaluate(contract, state).Status);
        }

        [Test]
        public void LeaderSlot_HighestStars_TiesGoToLowestSlot()
        {
            Assert.AreEqual(2, ContractEvaluator.LeaderSlot(new[] { 1, 2, 3, 2 }),
                "el 3★ es lider");
            Assert.AreEqual(1, ContractEvaluator.LeaderSlot(new[] { 1, 3, 3, 2 }),
                "empate de estrellas: gana el slot mas bajo");
            Assert.AreEqual(-1, ContractEvaluator.LeaderSlot(new int[0]),
                "party vacia no tiene lider");
        }

        [Test]
        public void TwoDishGoal_RequiresBothDishes()
        {
            var contract = new ContractDefinition
            {
                Id = "desayuno-real",
                Goal = new ContractGoal
                {
                    clearedDish = Dish.Desayuno,
                    clearedDishUnits = 10,
                    clearedDish2 = Dish.Postre,
                    clearedDish2Units = 10,
                },
                FailEnabled = true,
                MaxLostOrders = 3,
                TimeLimitSeconds = 180f,
            };
            RunState state = State(contract);

            state.AddCleared(Dish.Desayuno, 10);
            state.AddCleared(Dish.Postre, 9);
            Assert.AreEqual(RunStatus.Playing, ContractEvaluator.Evaluate(contract, state).Status);

            state.AddCleared(Dish.Postre, 1);
            Assert.AreEqual(RunStatus.Victory, ContractEvaluator.Evaluate(contract, state).Status);
        }

        [Test]
        public void ClearedByDish_CountsEveryClearedCell()
        {
            // Desayuno y Postre con un swap que arma linea de 3 en la fila inferior.
            var cells = new Dish[3, 3];
            cells[0, 0] = Dish.Desayuno;
            cells[1, 0] = Dish.Desayuno;
            cells[2, 0] = Dish.Postre;
            cells[0, 1] = Dish.Postre;
            cells[1, 1] = Dish.Postre;
            cells[2, 1] = Dish.Desayuno;
            cells[0, 2] = Dish.Desayuno;
            cells[1, 2] = Dish.Postre;
            cells[2, 2] = Dish.Postre;

            BoardModel board = BoardModel.CreateFromGrid(cells, new SystemRandom(7), 5);
            Assert.IsTrue(board.TryResolveSwap(2, 0, 2, 1, out ResolveResult result),
                "el swap preparado arma una linea");

            int byDish = 0;
            foreach (var pair in result.ClearedByDish) byDish += pair.Value;

            Assert.AreEqual(result.Cleared, byDish,
                "cada celda limpiada pertenece a un solo plato");
            Assert.Greater(result.ClearedByDish[Dish.Desayuno], 0);
            Assert.Greater(result.ClearedByDish[Dish.Postre], 0);
        }
    }
}
