namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Objetivo de victoria de un contrato. Es data pura (sin Unity): cada umbral positivo
    /// debe cumplirse. Un contrato puede combinar pedidos servidos, skills lanzadas, dos
    /// platos limpios del tablero y clientes VIP.
    /// </summary>
    public sealed class ContractGoal
    {
        public int servedOrders;
        public int skillsCast;
        public Dish clearedDish = Dish.Postre;
        public int clearedDishUnits;
        public Dish clearedDish2 = Dish.Desayuno;
        public int clearedDish2Units;
        public int vipOrders;

        /// <summary>Sin umbrales: el objetivo nunca se cumple (contrato mal configurado).</summary>
        public bool IsEmpty =>
            servedOrders <= 0 && skillsCast <= 0 && clearedDishUnits <= 0 &&
            clearedDish2Units <= 0 && vipOrders <= 0;

        public bool IsMet(ContractProgress progress)
        {
            if (IsEmpty) return false;
            if (servedOrders > 0 && progress.ServedOrders < servedOrders) return false;
            if (skillsCast > 0 && progress.SkillsCast < skillsCast) return false;
            if (clearedDishUnits > 0 && progress.Cleared(clearedDish) < clearedDishUnits) return false;
            if (clearedDish2Units > 0 && progress.Cleared(clearedDish2) < clearedDish2Units) return false;
            if (vipOrders > 0 && progress.VipOrders < vipOrders) return false;
            return true;
        }
    }
}
