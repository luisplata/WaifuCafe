using System;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Un contrato como dato puro: objetivo, modificadores y derrota. La run se evalúa
    /// contra esto; el menú lo arma desde un <c>ContractSO</c>. Sin Unity.
    /// </summary>
    public sealed class ContractDefinition
    {
        public const float DefaultTimeLimitSeconds = 180f;
        public const float DefaultPatienceSeconds = 30f;
        public const int DefaultMaxLostOrders = 3;

        public string Id = string.Empty;
        public string Title = string.Empty;

        /// <summary>Texto ya formateado del objetivo (lo muestra el HUD).</summary>
        public string GoalText = string.Empty;

        /// <summary>Texto ya formateado de los modificadores (lo muestra el HUD).</summary>
        public string ModifierText = string.Empty;

        /// <summary>Texto ya formateado de la derrota (lo muestra el HUD).</summary>
        public string DefeatText = string.Empty;

        public WaifuFaction Faction = WaifuFaction.Cafe;

        /// <summary>Preview bloqueada: el menú la muestra pero no se puede jugar.</summary>
        public bool Locked;

        public ContractGoal Goal = new ContractGoal();
        public ContractModifiers Modifiers = ContractModifiers.Neutral();

        public float TimeLimitSeconds = DefaultTimeLimitSeconds;
        public int MaxLostOrders = DefaultMaxLostOrders;

        /// <summary>False = el tiempo y los perdidos nunca derrotan (run de tutorial).</summary>
        public bool FailEnabled = true;

        public float BasePatienceSeconds = DefaultPatienceSeconds;

        public bool Playable => !Locked;

        /// <summary>Paciencia efectiva de un cliente sentado (base × modificador).</summary>
        public float EffectivePatienceSeconds
        {
            get
            {
                float multiplier = Modifiers != null ? Modifiers.patienceMultiplier : 1f;
                if (multiplier <= 0f) multiplier = 1f;
                return Math.Max(1f, BasePatienceSeconds * multiplier);
            }
        }
    }
}
