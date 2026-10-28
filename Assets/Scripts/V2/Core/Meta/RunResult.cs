namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Resultado final de una run. Se construye una sola vez al cerrar: incluye el estado
    /// (victoria/derrota), el motivo y lo que muestra el resultado. Las monedas son las
    /// ganadas en la run y se conservan siempre (soft fail).
    /// </summary>
    public sealed class RunResult
    {
        public string ContractId = string.Empty;
        public string ContractTitle = string.Empty;
        public RunStatus Status;
        public RunOutcome Outcome;
        public ContractProgress Progress;
        public int Coins;
        public string UltimateUsed = string.Empty;

        public bool IsVictory => Status == RunStatus.Victory;

        public static RunResult From(ContractDefinition contract, RunState state, RunStatus status,
            RunOutcome outcome)
        {
            ContractProgress progress = state != null ? state.Progress : ContractProgress.Empty;
            return new RunResult
            {
                ContractId = contract != null ? contract.Id : string.Empty,
                ContractTitle = contract != null ? contract.Title : string.Empty,
                Status = status,
                Outcome = outcome,
                Progress = progress,
                Coins = progress.Coins,
                UltimateUsed = state != null ? state.UltimateUsed : string.Empty,
            };
        }
    }
}
