using System;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Foto de solo lectura del avance de una run. El evaluador y el HUD consumen esto;
    /// no mutan el estado. <see cref="Cleared"/> devuelve 0 si no hay datos.
    /// </summary>
    public readonly struct ContractProgress
    {
        public const int DishCount = 5;

        private static readonly int[] NoCleared = Array.Empty<int>();

        public readonly int ServedOrders;
        public readonly int SkillsCast;
        public readonly int VipOrders;
        public readonly int LostOrders;
        public readonly int Coins;
        public readonly float ElapsedSeconds;

        private readonly int[] _cleared;

        public ContractProgress(int servedOrders, int skillsCast, int vipOrders, int lostOrders, int coins,
            float elapsedSeconds, int[] clearedByDish)
        {
            ServedOrders = servedOrders;
            SkillsCast = skillsCast;
            VipOrders = vipOrders;
            LostOrders = lostOrders;
            Coins = coins;
            ElapsedSeconds = elapsedSeconds;
            _cleared = clearedByDish;
        }

        /// <summary>Fichas de ese plato limpiadas en la run (todas las resoluciones).</summary>
        public int Cleared(Dish dish)
        {
            int index = (int)dish;
            if (_cleared == null || index < 0 || index >= _cleared.Length || index >= DishCount) return 0;
            return _cleared[index];
        }

        public int ClearedTotal
        {
            get
            {
                if (_cleared == null) return 0;
                int total = 0;
                for (int i = 0; i < _cleared.Length && i < DishCount; i++) total += _cleared[i];
                return total;
            }
        }

        public static ContractProgress Empty =>
            new ContractProgress(0, 0, 0, 0, 0, 0f, NoCleared);
    }
}
