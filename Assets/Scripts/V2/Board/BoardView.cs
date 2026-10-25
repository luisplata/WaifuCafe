using System;
using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using WaifuCafe.Core;

namespace V2.Board
{
    /// <summary>
    /// Vista del tablero en world space: un <see cref="TileView"/> por celda y todo el
    /// "feel" (swap animado, pop, cascada escalada, caida con squash, hint, audio
    /// procedural). La vista no decide reglas: el modelo resuelve y la vista lo refleja.
    /// Todos los knobs viven aca para ajustarlos desde el inspector.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        [Header("Layout (unidades de mundo)")]
        [SerializeField] private float tileSize = 0.98f;

        [Header("Colores por plato: Desayuno, Almuerzo, Bebida, Postre, Snack")]
        [SerializeField] private Color[] dishColors =
        {
            new Color(0.95f, 0.75f, 0.30f),
            new Color(0.42f, 0.75f, 0.45f),
            new Color(0.32f, 0.62f, 0.92f),
            new Color(0.91f, 0.45f, 0.68f),
            new Color(0.93f, 0.58f, 0.30f),
        };

        [Header("Swap")]
        [SerializeField] private float grabScale = 1.12f;
        [Tooltip("Offset maximo al arrastrar, en celdas")]
        [SerializeField] private float grabFollow = 0.3f;
        [SerializeField] private float swapTime = 0.14f;
        [SerializeField] private float rejectTime = 0.18f;
        [Tooltip("Cuanto se acercan las fichas rechazadas, en celdas")]
        [SerializeField] private float rejectPush = 0.16f;
        [SerializeField] private float rejectShake = 0.04f;

        [Header("Match")]
        [SerializeField] private float popTime = 0.14f;
        [SerializeField] private float popStagger = 0.007f;
        [SerializeField] private int particlesPerTile = 5;

        [Header("Cascada")]
        [Tooltip("Cada eslabon multiplica las duraciones por este factor")]
        [SerializeField] private float cascadeSpeed = 0.85f;
        [SerializeField] private float cascadeShake = 0.03f;
        [SerializeField] private float cascadePulse = 1.02f;
        [SerializeField] private int cascadeParticleBonus = 3;

        [Header("Gravedad")]
        [SerializeField] private float fallTimePerCell = 0.05f;
        [SerializeField] private float fallMaxTime = 0.25f;
        [SerializeField] private float fallStagger = 0.01f;
        [SerializeField] private float squash = 0.12f;
        [SerializeField] private float squashTime = 0.08f;

        [Header("Seleccion y hint")]
        [SerializeField] private float selectedScale = 1.08f;
        [SerializeField] private bool hintEnabled = true;
        [SerializeField] private float hintDelay = 5f;
        [SerializeField] private float hintPulse = 1.06f;

        [Header("Audio procedural")]
        [SerializeField] private bool muteAudio;
        [SerializeField, Range(0f, 1f)] private float volume = 0.3f;

        [Header("Rendimiento")]
        [Tooltip("Capacidad de tweens simultaneos de PrimeTween (evita allocs en cascada)")]
        [SerializeField] private int tweenCapacity = 512;

        private TileView[,] _tiles;
        private BoardSession _session;
        private BoardJuice _juice;
        private UnityEngine.Camera _camera;
        private TileView _selected;
        private TileView _dragOrigin;
        private bool _dragConsumed;
        private bool _animating;
        private float _hintTimer;
        private TileView _hintA, _hintB;

        private int Width => _tiles?.GetLength(0) ?? 0;
        private int Height => _tiles?.GetLength(1) ?? 0;

        public bool IsInteractive => _session != null && !_animating && !_session.IsResolving;

        private void Awake()
        {
            PrimeTweenConfig.SetTweensCapacity(tweenCapacity);
            _camera = UnityEngine.Camera.main;
            _juice = GetComponent<BoardJuice>();
            if (_juice == null) _juice = gameObject.AddComponent<BoardJuice>();
        }

        /// <summary>La sesion es la duena del modelo; la vista solo lo refleja.</summary>
        public void Attach(BoardSession session)
        {
            _session = session;
            if (_camera == null) _camera = UnityEngine.Camera.main;
            _juice.Configure(_camera, muteAudio, volume);
            BuildBackdrop(session.Board.Width, session.Board.Height);
            BuildTiles(session.Board.Width, session.Board.Height);
            SyncAll();
        }

        /// <summary>Re-sincroniza las 49 fichas desde el modelo. Barato y sin estado propio.</summary>
        public void SyncAll()
        {
            if (_tiles == null || _session == null) return;
            CancelHint();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var tile = _tiles[x, y];
                    if (tile == null) continue;

                    tile.PrepareTravel();
                    Dish dish = _session.Board.At(x, y);
                    tile.SetDish(dish, DishSprites.For(dish), dishColors[(int)dish]);
                }
            }

            _selected = null;
        }

        /// <summary>
        /// El tablero se movio entero (A/B de lado): los Home son world-space, hay que
        /// recalcularlos para que swap, rebote y caida sigan usando la posicion nueva.
        /// Las celdas locales no cambian.
        /// </summary>
        public void RefreshHomes()
        {
            if (_tiles == null) return;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                    _tiles[x, y].Rebind(new BoardPos(x, y), CellToWorld(new BoardPos(x, y)));
            }
        }

        private void Update()
        {
            if (!hintEnabled || _session == null || _hintA != null) return;
            if (!IsInteractive || _selected != null)
            {
                _hintTimer = 0f;
                return;
            }

            _hintTimer += Time.deltaTime;
            if (_hintTimer < hintDelay) return;
            _hintTimer = 0f;

            if (!TryFindMove(out BoardPos a, out BoardPos b)) return;
            _hintA = Get(a);
            _hintB = Get(b);
            if (_hintA != null) _hintA.SetHint(true, hintPulse);
            if (_hintB != null) _hintB.SetHint(true, hintPulse);
        }

        // ---- Input (EventSystem / Input System) -------------------------------------

        public void HandleClick(TileView tile)
        {
            if (!IsInteractive) return;
            CancelHint();

            if (_selected == null)
            {
                Select(tile);
                return;
            }

            if (_selected == tile)
            {
                Select(null);
                return;
            }

            if (AreAdjacent(_selected.Pos, tile.Pos))
            {
                BoardPos from = _selected.Pos;
                Select(null);
                _session.TryMove(from, tile.Pos);
                return;
            }

            Select(tile);
        }

        public void HandleBeginDrag(TileView tile)
        {
            CancelHint();
            _dragOrigin = IsInteractive ? tile : null;
            _dragConsumed = false;
            if (_dragOrigin == null) return;

            Select(tile);
            tile.BeginGrab(grabScale);
        }

        /// <summary>Swipe: la ficha sigue el dedo y al entrar en una celda adyacente dispara el swap.</summary>
        public void HandleDrag(PointerEventData eventData)
        {
            if (_dragOrigin == null || !IsInteractive) return;

            Vector3 world = ScreenToWorld(eventData.position);
            Vector3 offset = world - _dragOrigin.Home;
            float max = tileSize * grabFollow;
            if (offset.sqrMagnitude > max * max) offset = offset.normalized * max;
            _dragOrigin.SetGrabOffset(offset);

            if (_dragConsumed) return;
            BoardPos? cell = CellAt(world);
            if (cell == null || cell.Value.Equals(_dragOrigin.Pos)) return;
            if (!AreAdjacent(_dragOrigin.Pos, cell.Value)) return;

            _dragConsumed = true;
            BoardPos from = _dragOrigin.Pos;
            BoardPos to = cell.Value;
            _dragOrigin.ReleaseGrab(swapTime);
            Select(null);
            _session.TryMove(from, to);
        }

        public void HandleEndDrag()
        {
            if (_dragOrigin != null) _dragOrigin.ReleaseGrab(swapTime);
            _dragOrigin = null;
            _dragConsumed = false;
        }

        private void Select(TileView tile)
        {
            if (_selected != null) _selected.SetSelected(false, selectedScale);
            _selected = tile;
            if (_selected != null) _selected.SetSelected(true, selectedScale);
        }

        private void CancelHint()
        {
            _hintTimer = 0f;
            if (_hintA != null) _hintA.SetHint(false, hintPulse);
            if (_hintB != null) _hintB.SetHint(false, hintPulse);
            _hintA = null;
            _hintB = null;
        }

        // ---- Resolucion con juice ---------------------------------------------------

        /// <summary>Swap aceptado: viaje, pop por olas de cascada y caida animada. Al final snap al modelo.</summary>
        public void PlayResolve(BoardPos a, BoardPos b, ResolveResult result, Action onDone)
        {
            if (_animating)
            {
                SyncAll();
                onDone?.Invoke();
                return;
            }

            StartCoroutine(ResolveRoutine(a, b, result, onDone));
        }

        /// <summary>Swap rechazado (RF-1): rebote con overshoot + shake + flash rojo tenue.</summary>
        public void PlayReject(BoardPos a, BoardPos b)
        {
            if (_animating) return;
            StartCoroutine(RejectRoutine(a, b));
        }

        private IEnumerator ResolveRoutine(BoardPos a, BoardPos b, ResolveResult result, Action onDone)
        {
            _animating = true;
            CancelHint();
            Select(null);
            _juice.PlaySwap();

            Vector3 wa = CellToWorld(a);
            Vector3 wb = CellToWorld(b);
            TileView ta = Get(a);
            TileView tb = Get(b);

            int pending = 0;
            if (ta != null)
            {
                pending++;
                ta.PlaySwap(wb, swapTime, () => pending--);
            }

            if (tb != null)
            {
                pending++;
                tb.PlaySwap(wa, swapTime, () => pending--);
            }

            while (pending > 0) yield return null;

            // Las fichas viajaron con su sprite: se reasigna la celda fisica, no el dibujo.
            if (ta != null && tb != null)
            {
                ta.Rebind(b, wb);
                tb.Rebind(a, wa);
                _tiles[a.X, a.Y] = tb;
                _tiles[b.X, b.Y] = ta;
            }

            Dish[,] swapped = Snapshot();
            bool[,] firstWave = FindMatches(swapped);
            var cleared = new bool[Width, Height];
            Dish[,] final = FinalGrid();
            int waves = Mathf.Max(1, result.Cascades);
            var rest = new List<BoardPos>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!firstWave[x, y] && !final[x, y].Equals(swapped[x, y]))
                        rest.Add(new BoardPos(x, y));
                }
            }

            for (int wave = 1; wave <= waves; wave++)
            {
                List<BoardPos> cells = wave == 1 ? CellsOf(firstWave) : TakeSlice(rest, wave, waves);
                float speed = Mathf.Pow(cascadeSpeed, wave - 1);
                float duration = popTime * speed;
                float maxDelay = 0f;

                for (int i = 0; i < cells.Count; i++)
                {
                    BoardPos p = cells[i];
                    TileView tile = Get(p);
                    if (tile == null) continue;

                    float delay = wave == 1
                        ? (Mathf.Abs(p.X - a.X) + Mathf.Abs(p.Y - a.Y)) * popStagger
                        : i * popStagger;
                    maxDelay = Mathf.Max(maxDelay, delay);

                    tile.PlayPop(delay, duration);
                    cleared[p.X, p.Y] = true;
                    _juice.Burst(tile.Home, tile.Color, particlesPerTile + (wave - 1) * cascadeParticleBonus,
                        1f + 0.15f * (wave - 1));
                }

                if (cells.Count > 0)
                {
                    Vector3 center = Centroid(cells);
                    float boardTop = transform.position.y + (Height - 1) * 0.5f * tileSize;
                    center.y = Mathf.Min(center.y, boardTop - 0.5f) + 0.15f;
                    Color tint = dishColors[(int)swapped[cells[0].X, cells[0].Y]];
                    _juice.FloatText(center, "+" + UnitsInWave(cells, swapped, result), tint, 1f);
                    if (wave > 1) _juice.FloatText(center + Vector3.up * 0.45f, "x" + wave, Color.white, 0.8f);
                }

                if (wave == 1) _juice.PlayMatch();
                else _juice.PlayCascade(wave);
                _juice.Shake(cascadeShake * wave, 0.22f + 0.04f * wave);
                if (wave >= 2) _juice.PulseBoard(transform, cascadePulse, 0.14f);

                yield return new WaitForSeconds(maxDelay + duration + 0.01f);
            }

            yield return FallPhase(swapped, final, cleared);

            SyncAll();
            _animating = false;
            onDone?.Invoke();
        }

        /// <summary>Caida con gravedad real, stagger por columna y squash & stretch al aterrizar.</summary>
        private IEnumerator FallPhase(Dish[,] oldGrid, Dish[,] final, bool[,] cleared)
        {
            float maxArrival = 0f;
            bool any = false;

            for (int x = 0; x < Width; x++)
            {
                int changedBelow = 0;
                for (int y = 0; y < Height; y++)
                {
                    bool changed = cleared[x, y] || !final[x, y].Equals(oldGrid[x, y]);
                    if (changed)
                    {
                        any = true;
                        TileView tile = Get(new BoardPos(x, y));
                        if (tile != null)
                        {
                            int distance = changedBelow + 1;
                            float duration = Mathf.Min(fallMaxTime, fallTimePerCell * distance);
                            float delay = x * fallStagger;
                            tile.SetDish(final[x, y], DishSprites.For(final[x, y]), dishColors[(int)final[x, y]]);
                            tile.PlayFall(tile.Home + Vector3.up * (distance * tileSize * 1.15f), duration, delay,
                                squash, squashTime);
                            maxArrival = Mathf.Max(maxArrival, delay + duration + squashTime);
                        }

                        changedBelow++;
                    }
                    else
                    {
                        changedBelow = 0;
                    }
                }
            }

            if (!any) yield break;
            _juice.PlayFall();
            yield return new WaitForSeconds(maxArrival + 0.02f);
        }

        private IEnumerator RejectRoutine(BoardPos a, BoardPos b)
        {
            _animating = true;
            CancelHint();
            Select(null);
            _juice.PlayInvalid();

            Vector3 dir = (CellToWorld(b) - CellToWorld(a)).normalized;
            TileView ta = Get(a);
            TileView tb = Get(b);

            int pending = 0;
            if (ta != null)
            {
                pending++;
                ta.PlayReject(dir, tileSize, rejectTime, rejectPush, rejectShake, () => pending--);
                ta.FlashInvalid();
            }

            if (tb != null)
            {
                pending++;
                tb.PlayReject(-dir, tileSize, rejectTime, rejectPush, rejectShake, () => pending--);
                tb.FlashInvalid();
            }

            while (pending > 0) yield return null;
            _animating = false;
        }

        // ---- Deteccion solo para presentacion ---------------------------------------

        private Dish[,] Snapshot()
        {
            var grid = new Dish[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++) grid[x, y] = _tiles[x, y].Dish;
            }

            return grid;
        }

        private Dish[,] FinalGrid()
        {
            var grid = new Dish[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++) grid[x, y] = _session.Board.At(x, y);
            }

            return grid;
        }

        /// <summary>Lineas de 3+ en una grilla (solo para ubicar la primera ola de pop).</summary>
        private bool[,] FindMatches(Dish[,] grid)
        {
            var hit = new bool[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Dish dish = grid[x, y];
                    int run = 1;
                    while (x + run < Width && grid[x + run, y].Equals(dish)) run++;
                    if (run >= 3) for (int i = 0; i < run; i++) hit[x + i, y] = true;

                    run = 1;
                    while (y + run < Height && grid[x, y + run].Equals(dish)) run++;
                    if (run >= 3) for (int i = 0; i < run; i++) hit[x, y + i] = true;
                }
            }

            return hit;
        }

        private List<BoardPos> CellsOf(bool[,] flags)
        {
            var cells = new List<BoardPos>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (flags[x, y]) cells.Add(new BoardPos(x, y));
                }
            }

            return cells;
        }

        /// <summary>Reparte las celdas restantes entre los eslabones 2..N de la cascada.</summary>
        private static List<BoardPos> TakeSlice(List<BoardPos> rest, int wave, int waves)
        {
            var cells = new List<BoardPos>();
            if (rest.Count == 0 || waves <= 1) return cells;

            int links = waves - 1;
            int perLink = Mathf.CeilToInt(rest.Count / (float)links);
            int start = (wave - 2) * perLink;
            int end = Mathf.Min(rest.Count, start + perLink);
            for (int i = start; i < end; i++) cells.Add(rest[i]);
            return cells;
        }

        private int UnitsInWave(List<BoardPos> cells, Dish[,] grid, ResolveResult result)
        {
            var counts = new Dictionary<Dish, int>();
            foreach (BoardPos p in cells)
            {
                Dish dish = grid[p.X, p.Y];
                counts.TryGetValue(dish, out int n);
                counts[dish] = n + 1;
            }

            int units = 0;
            foreach (KeyValuePair<Dish, int> pair in counts)
            {
                if (result.Units.TryGetValue(pair.Key, out int total)) units += Mathf.Min(pair.Value, total);
            }

            return Mathf.Max(1, units);
        }

        private Vector3 Centroid(List<BoardPos> cells)
        {
            Vector3 sum = Vector3.zero;
            foreach (BoardPos p in cells) sum += CellToWorld(p);
            return sum / Mathf.Max(1, cells.Count);
        }

        /// <summary>Jugada valida para el hint: prueba swaps sobre una copia (cero mutacion del modelo).</summary>
        private bool TryFindMove(out BoardPos a, out BoardPos b)
        {
            var grid = new Dish[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++) grid[x, y] = _session.Board.At(x, y);
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (x + 1 < Width && TrySwap(grid, x, y, x + 1, y))
                    {
                        a = new BoardPos(x, y);
                        b = new BoardPos(x + 1, y);
                        return true;
                    }

                    if (y + 1 < Height && TrySwap(grid, x, y, x, y + 1))
                    {
                        a = new BoardPos(x, y);
                        b = new BoardPos(x, y + 1);
                        return true;
                    }
                }
            }

            a = default;
            b = default;
            return false;
        }

        private static bool TrySwap(Dish[,] grid, int ax, int ay, int bx, int by)
        {
            (grid[ax, ay], grid[bx, by]) = (grid[bx, by], grid[ax, ay]);
            bool valid = HasLine(grid, ax, ay) || HasLine(grid, bx, by);
            (grid[ax, ay], grid[bx, by]) = (grid[bx, by], grid[ax, ay]);
            return valid;
        }

        private static bool HasLine(Dish[,] grid, int x, int y)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            Dish dish = grid[x, y];
            int run = 1;

            for (int i = x - 1; i >= 0 && grid[i, y].Equals(dish); i--) run++;
            for (int i = x + 1; i < width && grid[i, y].Equals(dish); i++) run++;
            if (run >= 3) return true;

            run = 1;
            for (int i = y - 1; i >= 0 && grid[x, i].Equals(dish); i--) run++;
            for (int i = y + 1; i < height && grid[x, i].Equals(dish); i++) run++;
            return run >= 3;
        }

        // ---- Construccion -----------------------------------------------------------

        private void BuildTiles(int width, int height)
        {
            _tiles = new TileView[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var go = new GameObject($"Tile_{x}_{y}");
                    go.transform.SetParent(transform, false);

                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 2;

                    var collider = go.AddComponent<BoxCollider2D>();
                    collider.size = Vector2.one * 0.96f;

                    var tile = go.AddComponent<TileView>();
                    tile.Bind(this, new BoardPos(x, y), tileSize, CellToWorld(new BoardPos(x, y)));
                    _tiles[x, y] = tile;
                }
            }
        }

        private void BuildBackdrop(int width, int height)
        {
            float size = width * tileSize + 0.55f;

            var shadow = new GameObject("BoardShadow");
            shadow.transform.SetParent(transform, false);
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = DishSprites.Glow();
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.5f);
            shadowRenderer.sortingOrder = -1;
            shadow.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            shadow.transform.localScale = new Vector3(size * 1.16f, size * 1.16f, 1f);

            var panel = new GameObject("BoardPanel");
            panel.transform.SetParent(transform, false);
            var panelRenderer = panel.AddComponent<SpriteRenderer>();
            panelRenderer.sprite = DishSprites.RoundRect();
            panelRenderer.color = new Color(0.16f, 0.12f, 0.22f);
            panelRenderer.sortingOrder = 0;
            panel.transform.localScale = new Vector3(size, size, 1f);
        }

        private Vector3 CellToWorld(BoardPos pos) =>
            transform.position + new Vector3(
                (pos.X - (Width - 1) * 0.5f) * tileSize,
                (pos.Y - (Height - 1) * 0.5f) * tileSize,
                0f);

        /// <summary>Y = 0 es la fila inferior: la gravedad del modelo cae hacia abajo en pantalla.</summary>
        private BoardPos? CellAt(Vector3 world)
        {
            if (_tiles == null) return null;

            Vector3 local = world - transform.position;
            int x = Mathf.RoundToInt(local.x / tileSize + (Width - 1) * 0.5f);
            int y = Mathf.RoundToInt(local.y / tileSize + (Height - 1) * 0.5f);

            if (x < 0 || y < 0 || x >= Width || y >= Height) return null;
            return new BoardPos(x, y);
        }

        private Vector3 ScreenToWorld(Vector2 screen)
        {
            UnityEngine.Camera cam = _camera != null ? _camera : UnityEngine.Camera.main;
            if (cam == null) return transform.position;

            Vector3 world = cam.ScreenToWorldPoint(
                new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z)));
            world.z = transform.position.z;
            return world;
        }

        private TileView Get(BoardPos pos) =>
            pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height
                ? _tiles[pos.X, pos.Y]
                : null;

        private static bool AreAdjacent(BoardPos a, BoardPos b) =>
            Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y) == 1;
    }
}
