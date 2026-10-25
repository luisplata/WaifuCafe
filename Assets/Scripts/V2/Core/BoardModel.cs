using System;
using System.Collections.Generic;

namespace WaifuCafe.Core
{
    /// <summary>
    /// Grilla pura de match-3. No conoce sprites, pedidos ni tiempo.
    /// PR1 cubre: generacion sin matches, validacion de swap adyacente y deteccion de
    /// lineas de 3+ (horizontales, verticales y cruces). La gravedad, el refill y la
    /// cascada se enganchan en <see cref="TryResolveSwap"/> cuando llegue PR2.
    /// </summary>
    public sealed class BoardModel
    {
        /// <summary>Tope de unidades por grupo de linea (RF-2: 5 o mas emite 5).</summary>
        public const int MaxUnitsPerMatch = 5;

        private const int CreateAttempts = 100;

        private readonly Dish[,] _cells;
        private readonly bool[,] _filled;
        private readonly IRandom _rng;
        private readonly int _tileTypes;

        public int Width { get; }
        public int Height { get; }

        private BoardModel(int width, int height, int tileTypes, IRandom rng)
        {
            Width = width;
            Height = height;
            _tileTypes = tileTypes;
            _rng = rng;
            _cells = new Dish[width, height];
            _filled = new bool[width, height];
        }

        /// <summary>Genera un tablero sin lineas de 3+ y con al menos una jugada (RF-4).</summary>
        public static BoardModel CreateNoMatch(int width, int height, IRandom rng, int tileTypes = 5)
        {
            if (width < 3 || height < 3)
                throw new ArgumentOutOfRangeException(nameof(width), "El tablero necesita al menos 3x3.");
            if (tileTypes < 3)
                throw new ArgumentOutOfRangeException(nameof(tileTypes), "Hacen falta 3 tipos para no formar lineas.");
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            for (int attempt = 0; attempt < CreateAttempts; attempt++)
            {
                var board = new BoardModel(width, height, tileTypes, rng);
                board.FillWithoutRuns();
                if (board.HasMove()) return board;
            }

            throw new InvalidOperationException("No se pudo generar un tablero con jugada valida.");
        }

        public Dish At(int x, int y) => _cells[x, y];

        /// <summary>Celda vacia: quedo pendiente de la gravedad/refill de PR2.</summary>
        public bool IsEmpty(int x, int y) => !_filled[x, y];

        /// <summary>Hay al menos una linea de 3+ pendiente en la grilla.</summary>
        public bool HasAnyMatch()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (_filled[x, y] && HasRunThrough(x, y, _cells[x, y])) return true;

            return false;
        }

        /// <summary>Existe al menos un swap adyacente que formaria una linea.</summary>
        public bool HasMove()
        {
            for (int d = 0; d < _tileTypes; d++)
                if (HasMoveFor((Dish)d)) return true;

            return false;
        }

        /// <summary>Indicador de drought: hay jugada que produzca una linea de ese plato.</summary>
        public bool HasMoveFor(Dish dish)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (x + 1 < Width && SwapFormsDish(x, y, x + 1, y, dish)) return true;
                    if (y + 1 < Height && SwapFormsDish(x, y, x, y + 1, dish)) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Transaccional: si el swap no forma ninguna linea, la grilla queda identica (rebote).
        /// Si la forma, deja el resultado y limpia las celdas (quedan vacias hasta PR2).
        /// Los swaps diagonales y los pares no adyacentes se rechazan.
        /// </summary>
        public bool TryResolveSwap(int ax, int ay, int bx, int by, out ResolveResult result)
        {
            result = ResolveResult.Empty;

            if (!InBounds(ax, ay) || !InBounds(bx, by)) return false;
            if (Math.Abs(ax - bx) + Math.Abs(ay - by) != 1) return false;
            if (!_filled[ax, ay] || !_filled[bx, by]) return false;
            if (_cells[ax, ay] == _cells[bx, by]) return false;

            SwapCells(ax, ay, bx, by);
            var groups = CollectGroups();
            if (groups.Count == 0)
            {
                SwapCells(ax, ay, bx, by);
                return false;
            }

            result = BuildResult(groups);
            ClearCells(groups);

            // PR2 engancha aca gravedad + refill + cascada, y actualiza Cascades.
            return true;
        }

        private void FillWithoutRuns()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int bannedH = x >= 2 && _cells[x - 1, y] == _cells[x - 2, y] ? (int)_cells[x - 1, y] : -1;
                    int bannedV = y >= 2 && _cells[x, y - 1] == _cells[x, y - 2] ? (int)_cells[x, y - 1] : -1;

                    int allowed = _tileTypes;
                    if (bannedH >= 0) allowed--;
                    if (bannedV >= 0 && bannedV != bannedH) allowed--;

                    int pick = _rng.Next(allowed);
                    for (int d = 0; d < _tileTypes; d++)
                    {
                        if (d == bannedH || d == bannedV) continue;
                        if (pick == 0)
                        {
                            _cells[x, y] = (Dish)d;
                            break;
                        }

                        pick--;
                    }

                    _filled[x, y] = true;
                }
            }
        }

        private bool SwapFormsDish(int ax, int ay, int bx, int by, Dish dish)
        {
            if (!_filled[ax, ay] || !_filled[bx, by]) return false;
            if (_cells[ax, ay] == _cells[bx, by]) return false;

            SwapCells(ax, ay, bx, by);
            bool forms = HasRunThrough(ax, ay, dish) || HasRunThrough(bx, by, dish);
            SwapCells(ax, ay, bx, by);
            return forms;
        }

        private List<MatchGroup> CollectGroups()
        {
            var matched = new bool[Width, Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (_filled[x, y] && HasRunThrough(x, y, _cells[x, y]))
                        matched[x, y] = true;

            var groups = new List<MatchGroup>();
            var visited = new bool[Width, Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (matched[x, y] && !visited[x, y])
                        groups.Add(FloodFill(matched, visited, x, y));

            return groups;
        }

        /// <summary>Un cruce es un solo grupo: las celdas compartidas cuentan una vez.</summary>
        private MatchGroup FloodFill(bool[,] matched, bool[,] visited, int startX, int startY)
        {
            var group = new MatchGroup(_cells[startX, startY]);
            var pending = new Stack<int>();
            pending.Push(startY * Width + startX);
            visited[startX, startY] = true;

            while (pending.Count > 0)
            {
                int index = pending.Pop();
                int x = index % Width;
                int y = index / Width;
                group.Cells.Add(index);

                TryPush(pending, matched, visited, group.Dish, x + 1, y);
                TryPush(pending, matched, visited, group.Dish, x - 1, y);
                TryPush(pending, matched, visited, group.Dish, x, y + 1);
                TryPush(pending, matched, visited, group.Dish, x, y - 1);
            }

            return group;
        }

        private void TryPush(Stack<int> pending, bool[,] matched, bool[,] visited, Dish dish, int x, int y)
        {
            if (!InBounds(x, y) || visited[x, y] || !matched[x, y]) return;
            if (_cells[x, y] != dish) return;

            visited[x, y] = true;
            pending.Push(y * Width + x);
        }

        private static ResolveResult BuildResult(List<MatchGroup> groups)
        {
            int cleared = 0;
            var units = new Dictionary<Dish, int>();

            foreach (var group in groups)
            {
                cleared += group.Cells.Count;
                int unitsInGroup = Math.Min(group.Cells.Count, MaxUnitsPerMatch);
                units.TryGetValue(group.Dish, out int current);
                units[group.Dish] = current + unitsInGroup;
            }

            return new ResolveResult(1, cleared, units);
        }

        private void ClearCells(List<MatchGroup> groups)
        {
            foreach (var group in groups)
                foreach (int index in group.Cells)
                    _filled[index % Width, index / Width] = false;
        }

        private bool HasRunThrough(int x, int y, Dish dish)
        {
            if (!_filled[x, y] || _cells[x, y] != dish) return false;

            return RunLength(x, y, 1, 0) >= 3 || RunLength(x, y, 0, 1) >= 3;
        }

        private int RunLength(int x, int y, int dx, int dy)
        {
            int length = 1;

            for (int step = 1; ; step++)
            {
                int nx = x + dx * step;
                int ny = y + dy * step;
                if (!InBounds(nx, ny) || !_filled[nx, ny] || _cells[nx, ny] != _cells[x, y]) break;
                length++;
            }

            for (int step = 1; ; step++)
            {
                int nx = x - dx * step;
                int ny = y - dy * step;
                if (!InBounds(nx, ny) || !_filled[nx, ny] || _cells[nx, ny] != _cells[x, y]) break;
                length++;
            }

            return length;
        }

        private void SwapCells(int ax, int ay, int bx, int by)
        {
            Dish temp = _cells[ax, ay];
            _cells[ax, ay] = _cells[bx, by];
            _cells[bx, by] = temp;
        }

        private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        private sealed class MatchGroup
        {
            public readonly Dish Dish;
            public readonly List<int> Cells = new List<int>();

            public MatchGroup(Dish dish) => Dish = dish;
        }
    }
}
