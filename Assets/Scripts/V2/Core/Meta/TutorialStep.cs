using System.Collections.Generic;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Flujo del tutorial persistido en el perfil. Cada transición se guarda de inmediato.
    /// </summary>
    public enum TutorialStep
    {
        Gift = 0,
        MenuTour = 1,
        ScriptedRun = 2,
        Reward = 3,
        Done = 4,
    }

    /// <summary>
    /// Tutorial guionado del primer día: mismo tablero y mismo pipeline que un contrato,
    /// con semilla fija, cinco pedidos guionados y derrota desactivada. Vive en el core
    /// (sin Unity) para que el menú y los tests compartan exactamente los mismos números.
    /// </summary>
    public static class TutorialFlow
    {
        public const string ContractId = "tutorial";
        public const string ContractTitle = "Primer día";
        public const int Seed = 424242;
        public const int ServedGoal = 5;
        public const float TimeLimitSeconds = 240f;
        public const float PatienceSeconds = 600f;

        /// <summary>Contrato del tutorial: enseña cambiar, igualar, cargar y usar skill.</summary>
        public static ContractDefinition CreateContract()
        {
            return new ContractDefinition
            {
                Id = ContractId,
                Title = ContractTitle,
                GoalText = "Cambiá · Igualá · Cargá · Usá tu skill · Serví 5 pedidos",
                ModifierText = "Modo aprendizaje: el tiempo y los perdidos no derrotan",
                DefeatText = string.Empty,
                Faction = WaifuFaction.Miticos,
                Locked = false,
                Goal = new ContractGoal { servedOrders = ServedGoal },
                Modifiers = ContractModifiers.Neutral(),
                TimeLimitSeconds = TimeLimitSeconds,
                MaxLostOrders = 0,
                FailEnabled = false,
                BasePatienceSeconds = PatienceSeconds,
            };
        }

        /// <summary>Cinco clientes guionados: uno por cada paso de la enseñanza.</summary>
        public static ScriptedOrder[] CreateOrders()
        {
            return new[]
            {
                new ScriptedOrder(2f, (Dish.Almuerzo, 3)),
                new ScriptedOrder(16f, (Dish.Bebida, 3)),
                new ScriptedOrder(30f, (Dish.Snack, 3)),
                new ScriptedOrder(44f, (Dish.Postre, 3)),
                new ScriptedOrder(60f, (Dish.Almuerzo, 2), (Dish.Bebida, 2)),
            };
        }

        /// <summary>Run lista para el tablero con la party de starters.</summary>
        public static RunConfig CreateRun(IEnumerable<string> partyIds)
        {
            return RunConfig.Create(CreateContract(), Seed, partyIds, CreateOrders());
        }
    }
}
