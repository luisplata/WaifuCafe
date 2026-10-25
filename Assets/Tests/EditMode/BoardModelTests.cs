using System.Collections.Generic;
using NUnit.Framework;
using WaifuCafe.Core;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// Modelo puro del tablero.
    /// PR1: T1 (RF-4) y T4 (RF-1). PR2: T2 (RF-2), T3 (RF-3) y T5 (RF-3 cascadas).
    /// </summary>
    public class BoardModelTests
    {
        private const int Size = 7;
        private const int Seed = 4242;

        [Test]
        public void T1_GeneratedBoard_HasNoMatchesAndAtLeastOneMove()
        {
            for (int seed = 1; seed <= 25; seed++)
            {
                var board = BoardModel.CreateNoMatch(Size, Size, new SystemRandom(seed));

                Assert.IsFalse(board.HasAnyMatch(), $"seed {seed}: el tablero generado tiene una linea de 3+");
                Assert.IsTrue(board.HasMove(), $"seed {seed}: el tablero generado no tiene ninguna jugada");
            }
        }

        [Test]
        public void T4_InvalidSwap_LeavesGridIdentical()
        {
            var board = BoardModel.CreateNoMatch(Size, Size, new SystemRandom(Seed));
            var pristine = Snapshot(board);

            Assert.IsFalse(board.TryResolveSwap(0, 0, 1, 1, out _), "un swap diagonal debe rechazarse");
            CollectionAssert.AreEqual(pristine, Snapshot(board), "el swap diagonal muto la grilla");

            Assert.IsFalse(board.TryResolveSwap(0, 0, 3, 3, out _), "un swap no adyacente debe rechazarse");
            CollectionAssert.AreEqual(pristine, Snapshot(board), "el swap no adyacente muto la grilla");

            Assert.IsFalse(board.TryResolveSwap(-1, 0, 0, 0, out _), "un swap fuera de rango debe rechazarse");
            Assert.IsFalse(board.TryResolveSwap(0, 0, Size, 0, out _), "un swap fuera de rango debe rechazarse");
            CollectionAssert.AreEqual(pristine, Snapshot(board), "un swap fuera de rango muto la grilla");

            Assert.IsTrue(FindRejectedAdjacentSwap(out int ax, out int ay, out int bx, out int by),
                "el tablero no tiene ningun par adyacente que rebote");

            Assert.IsFalse(board.TryResolveSwap(ax, ay, bx, by, out _),
                $"el swap adyacente ({ax},{ay})->({bx},{by}) deberia rebotar");
            CollectionAssert.AreEqual(pristine, Snapshot(board), "el swap invalido muto la grilla");
        }

        [Test]
        public void T2_LineSizes_EmitUnitsCappedAtFive()
        {
            var three = Board(Grid(5, 5, (0, 2), (1, 2), (2, 3)));
            Assert.IsTrue(three.TryResolveSwap(2, 3, 2, 2, out var r3), "el swap deberia formar la linea de 3");
            Assert.AreEqual(3, UnitsOf(r3, Dish.Snack), "3 en linea deben emitir 3 unidades");

            var four = Board(Grid(5, 5, (0, 2), (1, 2), (2, 2), (3, 3)));
            Assert.IsTrue(four.TryResolveSwap(3, 3, 3, 2, out var r4), "el swap deberia formar la linea de 4");
            Assert.AreEqual(4, UnitsOf(r4, Dish.Snack), "4 en linea deben emitir 4 unidades");

            var six = Board(Grid(6, 6, (0, 2), (1, 2), (2, 2), (3, 2), (4, 2), (5, 3)));
            Assert.IsTrue(six.TryResolveSwap(5, 3, 5, 2, out var r6), "el swap deberia formar la linea de 6");
            Assert.AreEqual(5, UnitsOf(r6, Dish.Snack), "6 en linea deben emitir 5 unidades (tope)");
        }

        [Test]
        public void T3_GravityAndRefill_LeaveNoHolesOrResidualMatches()
        {
            for (int seed = 1; seed <= 10; seed++)
            {
                var board = Board(Grid(5, 5, (0, 2), (1, 2), (2, 3)), new SystemRandom(seed));

                Assert.IsTrue(board.TryResolveSwap(2, 3, 2, 2, out var result), $"seed {seed}: el swap deberia resolver");
                Assert.GreaterOrEqual(result.Cascades, 1, $"seed {seed}: deberia haber al menos un eslabon");

                AssertNoHoles(board, seed);
                Assert.IsFalse(board.HasAnyMatch(), $"seed {seed}: quedaron lineas pendientes");
            }
        }

        [Test]
        public void T5_Cascade_DoesNotMultiplyUnits_AndExposesCoinMultiplier()
        {
            // Cascada 1: linea de 3 Snack; el refill [2,2,2] arma una linea de 3 Bebida arriba.
            // Cascada 2: linea de 3 Bebida; el refill [0,1,3] no forma ninguna linea nueva.
            var rng = new QueueRandom(0, 2, 2, 2, 0, 1, 3);
            var board = Board(Grid(5, 5, (0, 0), (1, 0), (2, 1)), rng);

            Assert.IsTrue(board.TryResolveSwap(2, 1, 2, 0, out var result), "el swap deberia resolver");

            Assert.AreEqual(2, result.Cascades, "deberian contarse 2 eslabones");
            Assert.AreEqual(6, result.Cleared, "3 Snack + 3 Bebida limpiadas");
            Assert.AreEqual(3, UnitsOf(result, Dish.Snack), "la cascada NO multiplica las unidades");
            Assert.AreEqual(3, UnitsOf(result, Dish.Bebida), "las unidades de la cascada se acumulan");
            Assert.AreEqual(result.Cascades, result.CoinMultiplier, "el multiplicador de monedas es la cascada");
        }

        [Test]
        public void T6_Waves_ExposeExactCellsOfEachCascadeStep()
        {
            // Misma cascada que T5: ola 1 = linea de Snack abajo; ola 2 = los 3 Bebida del refill.
            var rng = new QueueRandom(0, 2, 2, 2, 0, 1, 3);
            var board = Board(Grid(5, 5, (0, 0), (1, 0), (2, 1)), rng);

            Assert.IsTrue(board.TryResolveSwap(2, 1, 2, 0, out var result), "el swap deberia resolver");

            Assert.AreEqual(result.Cascades, result.Waves.Count, "debe haber una ola por eslabon");
            Assert.AreEqual(result.Waves.Count, result.WaveUnits.Count, "cada ola declara sus unidades");

            CollectionAssert.AreEquivalent(
                new[] { new BoardPos(0, 0), new BoardPos(1, 0), new BoardPos(2, 0) },
                new List<BoardPos>(result.Waves[0]),
                "la ola 1 debe ser exactamente el primer match");
            Assert.AreEqual(3, result.WaveUnits[0], "la ola 1 emite 3 unidades de Snack");

            CollectionAssert.AreEquivalent(
                new[] { new BoardPos(0, 4), new BoardPos(1, 4), new BoardPos(2, 4) },
                new List<BoardPos>(result.Waves[1]),
                "la ola 2 debe ser exactamente la linea del refill, no celdas repartidas");
        }

        private static bool FindRejectedAdjacentSwap(out int ax, out int ay, out int bx, out int by)
        {
            ax = ay = bx = by = 0;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (x + 1 < Size && RejectsSwap(x, y, x + 1, y, out ax, out ay, out bx, out by)) return true;
                    if (y + 1 < Size && RejectsSwap(x, y, x, y + 1, out ax, out ay, out bx, out by)) return true;
                }
            }

            return false;
        }

        private static bool RejectsSwap(int ax0, int ay0, int bx0, int by0, out int ax, out int ay, out int bx, out int by)
        {
            ax = ax0;
            ay = ay0;
            bx = bx0;
            by = by0;

            var probe = BoardModel.CreateNoMatch(Size, Size, new SystemRandom(Seed));
            return !probe.TryResolveSwap(ax0, ay0, bx0, by0, out _);
        }

        private static List<Dish> Snapshot(BoardModel board)
        {
            var cells = new List<Dish>(Size * Size);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    cells.Add(board.At(x, y));

            return cells;
        }

        /// <summary>
        /// Grilla de fondo sin lineas: (x + y) % 4 nunca repite 3 seguidos en fila ni columna.
        /// Los Snack sembrados son los unicos del tablero; con tileTypes = 4 el refill nunca
        /// devuelve Snack, asi las unidades de ese plato quedan aisladas de las cascadas.
        /// </summary>
        private static BoardModel Board(Dish[,] grid, IRandom rng = null, int tileTypes = 4)
            => BoardModel.CreateFromGrid(grid, rng ?? new SystemRandom(Seed), tileTypes);

        private static Dish[,] Grid(int width, int height, params (int X, int Y)[] snacks)
        {
            var cells = new Dish[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    cells[x, y] = (Dish)((x + y) % 4);

            foreach (var (x, y) in snacks)
                cells[x, y] = Dish.Snack;

            return cells;
        }

        private static int UnitsOf(ResolveResult result, Dish dish)
            => result.Units.TryGetValue(dish, out int units) ? units : 0;

        private static void AssertNoHoles(BoardModel board, int seed)
        {
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    Assert.IsFalse(board.IsEmpty(x, y), $"seed {seed}: hueco en ({x},{y})");
        }

        /// <summary>IRandom con valores prefijados para forzar un refill concreto.</summary>
        private sealed class QueueRandom : IRandom
        {
            private readonly Queue<int> _queued;
            private readonly int _fallback;

            public QueueRandom(int fallback, params int[] values)
            {
                _fallback = fallback;
                _queued = new Queue<int>(values);
            }

            public int Next(int maxExclusive)
            {
                if (maxExclusive <= 1) return 0;

                int value = _queued.Count > 0 ? _queued.Dequeue() : _fallback;
                return value % maxExclusive;
            }
        }
    }
}
