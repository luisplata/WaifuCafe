using System;

namespace WaifuCafe.Core.Meta
{
    /// <summary>Estado de una run. Playing es el único estado vivo.</summary>
    public enum RunStatus
    {
        Playing = 0,
        Victory = 1,
        Defeat = 2,
    }

    /// <summary>Motivo del cierre de la run. None mientras se juega.</summary>
    public enum RunOutcome
    {
        None = 0,
        GoalReached = 1,
        MaxLostOrders = 2,
        TimeExpired = 3,
    }

    /// <summary>Resultado puro de evaluar un contrato contra el avance.</summary>
    public readonly struct ContractEvaluation
    {
        public readonly RunStatus Status;
        public readonly RunOutcome Outcome;
        public readonly ContractProgress Progress;

        public ContractEvaluation(RunStatus status, RunOutcome outcome, ContractProgress progress)
        {
            Status = status;
            Outcome = outcome;
            Progress = progress;
        }

        public bool IsFinished => Status != RunStatus.Playing;

        public static ContractEvaluation Playing(ContractProgress progress) =>
            new ContractEvaluation(RunStatus.Playing, RunOutcome.None, progress);
    }

    /// <summary>
    /// Reglas puras de victoria/derrota por contrato. Orden de decisión: objetivo cumplido
    /// gana siempre; después derrota por perdidos; después derrota por tiempo. Las monedas
    /// no se tocan acá: el soft fail conserva las parciales.
    /// </summary>
    public static class ContractEvaluator
    {
        public static ContractEvaluation Evaluate(ContractDefinition contract, RunState state)
        {
            ContractProgress progress = state != null ? state.Progress : ContractProgress.Empty;
            if (contract == null) return ContractEvaluation.Playing(progress);

            if (contract.Goal != null && contract.Goal.IsMet(progress))
                return new ContractEvaluation(RunStatus.Victory, RunOutcome.GoalReached, progress);

            if (contract.FailEnabled)
            {
                if (contract.MaxLostOrders > 0 && progress.LostOrders >= contract.MaxLostOrders)
                    return new ContractEvaluation(RunStatus.Defeat, RunOutcome.MaxLostOrders, progress);

                if (contract.TimeLimitSeconds > 0f && progress.ElapsedSeconds >= contract.TimeLimitSeconds)
                    return new ContractEvaluation(RunStatus.Defeat, RunOutcome.TimeExpired, progress);
            }

            return ContractEvaluation.Playing(progress);
        }

        /// <summary>
        /// Líder de la party: mayor cantidad de estrellas; empate gana el slot más bajo.
        /// Devuelve -1 si no hay slots.
        /// </summary>
        public static int LeaderSlot(System.Collections.Generic.IReadOnlyList<int> stars)
        {
            if (stars == null || stars.Count == 0) return -1;

            int best = 0;
            for (int i = 1; i < stars.Count; i++)
                if (stars[i] > stars[best]) best = i;
            return best;
        }
    }
}
