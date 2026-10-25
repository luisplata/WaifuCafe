using System.Collections.Generic;
using NUnit.Framework;
using WaifuCafe.Core;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// PR1: modelo puro del tablero. T1 = tablero inicial (RF-4), T4 = swap invalido (RF-1).
    /// Gravedad/refill/cascada se testean en PR2 (T2/T3/T5).
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
    }
}
