using System.Collections.Generic;

namespace WaifuCafe.Core
{
    /// <summary>
    /// Lo que deja una resolucion: cuantas fichas se limpiaron y cuantas unidades
    /// de cada plato se entregan. Las unidades NO llevan multiplicador de cascada.
    /// </summary>
    public readonly struct ResolveResult
    {
        public static ResolveResult Empty => new ResolveResult(0, 0, new Dictionary<Dish, int>());

        /// <summary>Niveles de resolucion encadenada. Una sola pasada = 1.</summary>
        public readonly int Cascades;

        /// <summary>Fichas distintas limpiadas.</summary>
        public readonly int Cleared;

        /// <summary>Unidades por plato (tope 5 por grupo de linea).</summary>
        public readonly Dictionary<Dish, int> Units;

        public ResolveResult(int cascades, int cleared, Dictionary<Dish, int> units)
        {
            Cascades = cascades;
            Cleared = cleared;
            Units = units;
        }
    }
}
