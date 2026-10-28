namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Breakpoints de facción y rol (D3). Tabla pura: la usan igual el menú, el HUD, los
    /// modificadores de party y los tests. Cada línea es "el tier más alto alcanzado
    /// reemplaza al anterior", no una suma acumulativa.
    /// </summary>
    public static class FactionTable
    {
        /// <summary>Slots de party. Una party válida tiene exactamente este tamaño.</summary>
        public const int PartySize = 4;

        public const int CafeBreakpoint2 = 2;
        public const int CafeBreakpoint3 = 3;

        public const int MiticosBreakpoint2 = 2;
        public const int MiticosBreakpoint3 = 3;
        public const int MiticosBreakpoint4 = 4;

        public const int EspectaculoBreakpoint2 = 2;
        public const int EspectaculoBreakpoint3 = 3;

        public const int RoleBreakpoint2 = 2;
        public const int RoleBreakpoint3 = 3;

        /// <summary>Gate T2: 4 Míticos y suma de estrellas ≥ 8 (D4).</summary>
        public const int T2Members = 4;
        public const int T2Stars = 8;

        // ---- Tiers ---------------------------------------------------------------

        public static int CafeTier(int members) =>
            members >= CafeBreakpoint3 ? 3 : members >= CafeBreakpoint2 ? 2 : 0;

        public static int MiticosTier(int members) =>
            members >= MiticosBreakpoint4 ? 4 :
            members >= MiticosBreakpoint3 ? 3 :
            members >= MiticosBreakpoint2 ? 2 : 0;

        public static int EspectaculoTier(int members) =>
            members >= EspectaculoBreakpoint3 ? 3 : members >= EspectaculoBreakpoint2 ? 2 : 0;

        public static int RoleTier(int members) =>
            members >= RoleBreakpoint3 ? 3 : members >= RoleBreakpoint2 ? 2 : 0;

        public static int FactionTier(WaifuFaction faction, int members)
        {
            switch (faction)
            {
                case WaifuFaction.Cafe: return CafeTier(members);
                case WaifuFaction.Miticos: return MiticosTier(members);
                case WaifuFaction.Espectaculo: return EspectaculoTier(members);
                default: return 0;
            }
        }

        // ---- Efectos por tier ----------------------------------------------------

        /// <summary>Café 2 → +1 moneda/pedido; 3 → +2 (reemplaza, no suma).</summary>
        public static int CafeCoinsPerOrder(int tier) => tier >= 3 ? 2 : tier >= 2 ? 1 : 0;

        /// <summary>Café 3 → costo de carga ×0.9.</summary>
        public static float CafeChargeCostMultiplier(int tier) => tier >= 3 ? 0.9f : 1f;

        /// <summary>Míticos 2 → −1 carga; 3 → −2 (reemplaza).</summary>
        public static int MiticosChargeFlat(int tier) => tier >= 3 ? 2 : tier >= 2 ? 1 : 0;

        /// <summary>Míticos 3 → +1 potencia.</summary>
        public static int MiticosPotency(int tier) => tier >= 3 ? 1 : 0;

        /// <summary>Míticos 4 → la party arranca con 50% de la carga.</summary>
        public static bool MiticosHalfStartCharge(int tier) => tier >= 4;

        /// <summary>Espectáculo 2 → +15% paciencia; 3 → +30% (reemplaza).</summary>
        public static float EspectaculoPatience(int tier) => tier >= 3 ? 1.30f : tier >= 2 ? 1.15f : 1f;

        /// <summary>Espectáculo 3 → +1 moneda por cascada de 3 o más.</summary>
        public static int EspectaculoCascadeCoins(int tier) => tier >= 3 ? 1 : 0;

        /// <summary>Cocina 2 → +1 potencia.</summary>
        public static int CocinaPotency(int tier) => tier >= 2 ? 1 : 0;

        /// <summary>Servicio 2 → ganancia de carga ×1.10; 3 → ×1.20 (reemplaza).</summary>
        public static float ServicioChargeGain(int tier) => tier >= 3 ? 1.20f : tier >= 2 ? 1.10f : 1f;

        /// <summary>Apoyo 2 → +1 carga inicial; 3 → +2 (reemplaza).</summary>
        public static int ApoyoStartCharge(int tier) => tier >= 3 ? 2 : tier >= 2 ? 1 : 0;

        /// <summary>Control 2 → −1 cooldown a los miembros Control.</summary>
        public static int ControlCooldown(int tier) => tier >= 2 ? 1 : 0;

        // ---- Texto ---------------------------------------------------------------

        public static string FactionName(WaifuFaction faction)
        {
            switch (faction)
            {
                case WaifuFaction.Cafe: return "Café";
                case WaifuFaction.Miticos: return "Míticos";
                case WaifuFaction.Espectaculo: return "Espectáculo";
                default: return faction.ToString();
            }
        }

        public static string RoleName(WaifuRole role)
        {
            switch (role)
            {
                case WaifuRole.Cocina: return "Cocina";
                case WaifuRole.Servicio: return "Servicio";
                case WaifuRole.Apoyo: return "Apoyo";
                case WaifuRole.Control: return "Control";
                default: return role.ToString();
            }
        }
    }
}
