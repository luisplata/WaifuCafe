namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Reglas puras de estrellas. Las estrellas son ascensión por jugador (dupes), no un
    /// dato del asset: el asset solo declara <c>baseStars</c>/<c>maxStars</c>/<c>rarity</c>.
    /// </summary>
    public static class WaifuStars
    {
        public const int Min = 1;
        public const int Max = 3;

        /// <summary>Nivel de skill in-run: 1 en 0–5 unidades, 2 en 6–11, 3 desde 12.</summary>
        public static int LevelFromUnits(int units)
        {
            if (units >= 12) return 3;
            if (units >= 6) return 2;
            return 1;
        }

        /// <summary>Potencia = base + 2(★−1) + 2(nivel−1).</summary>
        public static int Potency(int basePotency, int stars, int level)
        {
            int starBonus = 2 * (Clamp(stars) - 1);
            int levelBonus = 2 * (ClampLevel(level) - 1);
            return basePotency + starBonus + levelBonus;
        }

        public static int Clamp(int stars)
        {
            if (stars < Min) return Min;
            return stars > Max ? Max : stars;
        }

        public static int ClampLevel(int level)
        {
            if (level < 1) return 1;
            return level > 3 ? 3 : level;
        }
    }
}
