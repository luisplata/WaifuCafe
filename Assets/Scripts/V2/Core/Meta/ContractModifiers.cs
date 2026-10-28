namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Modificadores de un contrato. Multiplicadores 1 = sin cambio. El run los aplica
    /// sobre los valores base del piso y de la party; el evaluador no los interpreta.
    /// </summary>
    public sealed class ContractModifiers
    {
        public float arrivalIntervalMultiplier = 1f;
        public float coinMultiplier = 1f;
        public float chargeCostMultiplier = 1f;
        public float postreWeightMultiplier = 1f;
        public float patienceMultiplier = 1f;
        public int coinsPerCascade;
        public bool ultimateEnabled = true;

        public bool IsNeutral =>
            arrivalIntervalMultiplier == 1f && coinMultiplier == 1f &&
            chargeCostMultiplier == 1f && postreWeightMultiplier == 1f &&
            patienceMultiplier == 1f && coinsPerCascade == 0 && ultimateEnabled;

        public static ContractModifiers Neutral() => new ContractModifiers();
    }
}
