using System;
using System.Collections.Generic;

namespace WaifuCafe.Core
{
    /// <summary>
    /// Lo que deja una resolucion: cuantas fichas se limpiaron y cuantas unidades
    /// de cada plato se entregan. Las unidades NO llevan multiplicador de cascada.
    /// Las olas son datos exactos: la vista anima esas celdas, no una aproximacion.
    /// </summary>
    public readonly struct ResolveResult
    {
        private static readonly IReadOnlyList<IReadOnlyList<BoardPos>> NoWaves =
            Array.Empty<IReadOnlyList<BoardPos>>();

        private static readonly IReadOnlyList<int> NoWaveUnits = Array.Empty<int>();

        private static readonly IReadOnlyList<BoardSnapshot> NoSnapshots = Array.Empty<BoardSnapshot>();

        private static readonly IReadOnlyDictionary<Dish, int> NoClearedByDish =
            new Dictionary<Dish, int>();

        public static ResolveResult Empty => new ResolveResult(0, 0, new Dictionary<Dish, int>(), NoWaves,
            NoWaveUnits, NoSnapshots, NoClearedByDish);

        /// <summary>Niveles de resolucion encadenada. Una sola pasada = 1. Igual a <c>Waves.Count</c>.</summary>
        public readonly int Cascades;

        /// <summary>Fichas distintas limpiadas.</summary>
        public readonly int Cleared;

        /// <summary>Unidades por plato (tope 5 por grupo de linea).</summary>
        public readonly Dictionary<Dish, int> Units;

        /// <summary>
        /// Celdas exactas que se limpiaron en cada paso, en orden de cascada. La ola 1 son
        /// los matches del primer paso; las siguientes, los de cada eslabon encadenado.
        /// Una ola por eslabon: <c>Waves.Count == Cascades</c>.
        /// </summary>
        public readonly IReadOnlyList<IReadOnlyList<BoardPos>> Waves;

        /// <summary>Unidades emitidas por ola (misma cantidad de elementos que <see cref="Waves"/>).</summary>
        public readonly IReadOnlyList<int> WaveUnits;

        /// <summary>
        /// Tablero exacto al cerrar cada ola: ya limpio, con la gravedad aplicada y el refill
        /// hecho. La vista anima la caida hacia esta foto, sin simular nada. La ultima foto es
        /// el tablero final del modelo y <c>Snapshots.Count == Waves.Count</c>.
        /// </summary>
        public readonly IReadOnlyList<BoardSnapshot> Snapshots;

        /// <summary>
        /// Fichas limpiadas por plato, sin el tope de 5 por grupo. Sirve para objetivos que
        /// cuentan celdas (por ejemplo "25 Postre"); <see cref="Units"/> sigue siendo lo
        /// que se entrega a los pedidos.
        /// </summary>
        public readonly IReadOnlyDictionary<Dish, int> ClearedByDish;

        /// <summary>
        /// Multiplicador que corresponde a las monedas (1 = sin cascada). Es SOLO un dato:
        /// la economia vive fuera del core y no entra en este PR; las unidades no lo usan.
        /// </summary>
        public int CoinMultiplier => Cascades;

        public ResolveResult(int cascades, int cleared, Dictionary<Dish, int> units,
            IReadOnlyList<IReadOnlyList<BoardPos>> waves, IReadOnlyList<int> waveUnits,
            IReadOnlyList<BoardSnapshot> snapshots, IReadOnlyDictionary<Dish, int> clearedByDish = null)
        {
            Cascades = cascades;
            Cleared = cleared;
            Units = units;
            Waves = waves ?? NoWaves;
            WaveUnits = waveUnits ?? NoWaveUnits;
            Snapshots = snapshots ?? NoSnapshots;
            ClearedByDish = clearedByDish ?? NoClearedByDish;
        }
    }
}
