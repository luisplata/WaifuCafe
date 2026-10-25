using System;
using System.Collections.Generic;

namespace WaifuCafe.Core
{
    /// <summary>
    /// Grilla pura de match-3. No conoce sprites, pedidos ni tiempo.
    /// Cubre generacion sin matches, validacion de swap adyacente, deteccion de lineas
    /// de 3+ (horizontales, verticales y cruces), gravedad, refill por <see cref="IRandom"/>
    /// y cascadas encadenadas. La fila Y = 0 es la inferior: la gravedad empuja hacia abajo.
    /// </summary>
    public sealed class BoardModel
    {
        /// <summary>Tope de unidades por grupo de linea (RF-2: 5 o mas emite 5).</summary>
        public const int MaxUnitsPerMatch = 5;

        private const int CreateAttempts = 100;
        private const int DishCount = 5;

        /// <summary>Tope de pasadas encadenadas: evita un loop con un IRandom degenerado.</summary>
        private const int MaxCascades = 64;

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

        /// <summary>
        /// Construye un tablero desde celdas conocidas (tests y herramientas). No valida
        /// que la grilla ya tenga matches: eso lo decide quien llama.
        /// </summary>
        public static BoardModel CreateFromGrid(Dish[,] cells, IRandom rng, int tileTypes = DishCount)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            int width = cells.GetLength(0);
            int height = cells.GetLength(1);
            if (width < 3 || height < 3)
                throw new ArgumentOutOfRangeException(nameof(cells), "El tablero necesita al menos 3x3.");
            if (tileTypes < 3 || tileTypes > DishCount)
                throw new ArgumentOutOfRangeException(nameof(tileTypes), "Los tipos de ficha van de 3 a 5.");

            var board = new BoardModel(width, height, tileTypes, rng);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    board._cells[x, y] = cells[x, y];
                    board._filled[x, y] = true;
                }
            }

            return board;
        }

        public Dish At(int x, int y) => _cells[x, y];

        /// <summary>Celda vacia. Es transitorio: al terminar una resolucion no quedan huecos.</summary>
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
        /// Si la forma, resuelve limpieza + gravedad + refill + cascadas hasta que no queden
        /// lineas. Los swaps diagonales y los pares no adyacentes se rechazan.
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

            result = Resolve(groups);
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

        /// <summary>
        /// Limpia el primer grupo y encadena mientras el refill genere lineas nuevas.
        /// Las unidades se acumulan por grupo conectado y NO llevan multiplicador.
        /// Cada eslabon deja su ola con las celdas exactas que limpio y la foto del tablero
        /// ya caido: la ultima foto coincide con el estado final.
        /// </summary>
        private ResolveResult Resolve(List<MatchGroup> firstGroups)
        {
            int cascades = 0;
            int cleared = 0;
            var units = new Dictionary<Dish, int>();
            var waves = new List<IReadOnlyList<BoardPos>>();
            var waveUnits = new List<int>();
            var snapshots = new List<BoardSnapshot>();
            var groups = firstGroups;

            while (groups.Count > 0 && cascades < MaxCascades)
            {
                cascades++;
                waves.Add(CellsOf(groups));
                waveUnits.Add(Accumulate(groups, units, ref cleared));
                ClearCells(groups);
                ApplyGravityAndRefill();
                snapshots.Add(BoardSnapshot.Capture(_cells));
                groups = CollectGroups();
            }

            return new ResolveResult(cascades, cleared, units, waves, waveUnits, snapshots);
        }

        /// <summary>Celdas de todos los grupos como coordenadas de tablero (para la vista).</summary>
        private List<BoardPos> CellsOf(List<MatchGroup> groups)
        {
            var cells = new List<BoardPos>();
            foreach (var group in groups)
            {
                foreach (int index in group.Cells)
                    cells.Add(new BoardPos(index % Width, index / Width));
            }

            return cells;
        }

        /// <summary>Acumula la ola y devuelve cuantas unidades emitio.</summary>
        private static int Accumulate(List<MatchGroup> groups, Dictionary<Dish, int> units, ref int cleared)
        {
            int waveUnits = 0;

            foreach (var group in groups)
            {
                cleared += group.Cells.Count;
                int unitsInGroup = Math.Min(group.Cells.Count, MaxUnitsPerMatch);
                units.TryGetValue(group.Dish, out int current);
                units[group.Dish] = current + unitsInGroup;
                waveUnits += unitsInGroup;
            }

            return waveUnits;
        }

        /// <summary>
        /// Compacta cada columna hacia Y = 0 y rellena los huecos de arriba desde el RNG.
        /// El orden de refill es por columna y, dentro de la columna, de abajo hacia arriba.
        /// </summary>
        private void ApplyGravityAndRefill()
        {
            for (int x = 0; x < Width; x++)
            {
                int writeY = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (!_filled[x, y]) continue;

                    if (writeY != y)
                    {
                        _cells[x, writeY] = _cells[x, y];
                        _filled[x, writeY] = true;
                    }

                    writeY++;
                }

                for (int y = writeY; y < Height; y++)
                {
                    _cells[x, y] = (Dish)_rng.Next(_tileTypes);
                    _filled[x, y] = true;
                }
            }
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
