using System;
using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using V2.Helpers;
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
        [Tooltip("Piso del multiplicador global: la cascada no acelera mas que esto")]
        [SerializeField, Range(0.3f, 1f)] private float maxWaveSpeedup = 0.6f;
        [SerializeField] private float cascadeShake = 0.03f;
        [SerializeField] private float cascadePulse = 1.02f;
        [SerializeField] private int cascadeParticleBonus = 3;

        [Header("Beat por ola (telegraph)")]
        [Tooltip("Aviso luminoso antes de explotar, en olas de cascada (s)")]
        [SerializeField] private float telegraphTime = 0.12f;
        [Tooltip("Aviso de la primera ola: mas corto para no frenar la jugada (s)")]
        [SerializeField] private float telegraphFirst = 0.09f;
        [Tooltip("Piso del telegraph: nunca se saltea, ni en cascadas rapidas (s)")]
        [SerializeField] private float telegraphMin = 0.08f;
        [Tooltip("Pausa de lectura entre el aterrizaje y el aviso (s)")]
        [SerializeField] private float settleDelay = 0.06f;
        [Tooltip("Alpha maximo del halo del plato durante el telegraph")]
        [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.85f;
        [Tooltip("Color blanco-caliente del flash del telegraph")]
        [SerializeField] private Color glowColor = new Color(1f, 0.97f, 0.9f);
        [Tooltip("Cuanto sube el pitch del tick por eslabon de cascada")]
        [SerializeField] private float telegraphPitchStep = 0.12f;

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

        public bool IsInteractive => _session != null && !_animating && !_session.IsResolving && !GameplayLock.IsLocked;

        /// <summary>Tablero ocupado: resolviendo o animando. La cinematica espera a que baje.</summary>
        public bool IsBusy => _animating || (_session != null && _session.IsResolving);

        /// <summary>Celda elegida por tap: destino de las skills de tablero (Rika).</summary>
        public BoardPos? SelectedPos => _selected == null ? (BoardPos?)null : _selected.Pos;

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
            // El viaje lo maneja la resolucion (swap o rebote): no devolver a home aca.
            Select(null);
            _session.TryMove(from, to);
        }

        public void HandleEndDrag()
        {
            // Release valido: la ficha viaja con el swap (o rebota) y la animacion manda.
            // Solo vuelve a home si el drag no disparo nada, o si la resolucion ya termino.
            bool animated = _dragConsumed && (_animating || (_session != null && _session.IsResolving));
            if (_dragOrigin != null && !animated) _dragOrigin.ReleaseGrab(swapTime);
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

        /// <summary>
        /// Skill de tablero: sin swap, pero con las mismas olas, telegraph y caida que un
        /// match. La ola 1 son las celdas que toco la skill.
        /// </summary>
        public void PlaySkillResolve(ResolveResult result, Action onDone)
        {
            if (_animating)
            {
                SyncAll();
                onDone?.Invoke();
                return;
            }

            StartCoroutine(ResolveRoutine(null, null, result, onDone));
        }

        /// <summary>Swap rechazado (RF-1): rebote con overshoot + shake + flash rojo tenue.</summary>
        public void PlayReject(BoardPos a, BoardPos b)
        {
            if (_animating) return;
            StartCoroutine(RejectRoutine(a, b));
        }

        private IEnumerator ResolveRoutine(BoardPos? a, BoardPos? b, ResolveResult result, Action onDone)
        {
            _animating = true;
            CancelHint();
            Select(null);
            bool swap = a.HasValue && b.HasValue;
            if (swap) _juice.PlaySwap();
            float startMs = Time.realtimeSinceStartup * 1000f;

            int pending = 0;
            if (swap)
            {
                Vector3 wa = CellToWorld(a.Value);
                Vector3 wb = CellToWorld(b.Value);
                TileView ta = Get(a.Value);
                TileView tb = Get(b.Value);

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

                // 1) ATERRIZAJE de la ola 0: el swap. Nada arranca hasta que las dos fichas llegan.
                while (pending > 0) yield return null;

                // Las fichas viajaron con su sprite: se reasigna la celda fisica, no el dibujo.
                if (ta != null && tb != null)
                {
                    ta.Rebind(b.Value, wb);
                    tb.Rebind(a.Value, wa);
                    _tiles[a.Value.X, a.Value.Y] = tb;
                    _tiles[b.Value.X, b.Value.Y] = ta;
                }
            }

            IReadOnlyList<IReadOnlyList<BoardPos>> waves = result.Waves;

            if (waves.Count == 0)
            {
                SyncAll();
                _animating = false;
                onDone?.Invoke();
                yield break;
            }

            // Estado visible entre olas: la foto que dejo la ola anterior. Nadie la reescribe,
            // asi el telegraph ilumina las fichas que REALMENTE estan en la celda.
            BoardSnapshot current = Snapshot();

            for (int w = 0; w < waves.Count; w++)
            {
                int wave = w + 1;
                IReadOnlyList<BoardPos> cells = waves[w];
                float speed = Mathf.Max(maxWaveSpeedup, Mathf.Pow(cascadeSpeed, wave - 1));
                float telegraph = Mathf.Max(telegraphMin, wave == 1 ? telegraphFirst : telegraphTime * speed);
                float pop = popTime * speed;

                // 2) SETTLE: la ola anterior ya aterrizo (el fall se espera al final del loop).
                float t = Time.realtimeSinceStartup;
                yield return new WaitForSeconds(settleDelay);
                float settleMs = (Time.realtimeSinceStartup - t) * 1000f;

                // 3) TELEGRAPH: todo el grupo se ilumina junto, antes de explotar.
                t = Time.realtimeSinceStartup;
                Dish dish = Dominant(cells);
                for (int i = 0; i < cells.Count; i++)
                {
                    TileView tile = Get(cells[i]);
                    if (tile == null) continue;

                    tile.PlayTelegraph(telegraph, dishColors[(int)tile.Dish], glowColor, glowAlpha);
                }

                _juice.PlayTelegraph(1f + telegraphPitchStep * (wave - 1));

                // 5) SCORE: el +N y el xN entran con el aviso y siguen visibles hasta el pop.
                Vector3 center = Centroid(cells);
                float boardTop = transform.position.y + (Height - 1) * 0.5f * tileSize;
                center.y = Mathf.Min(center.y, boardTop - 0.5f) + 0.15f;
                int units = w < result.WaveUnits.Count ? result.WaveUnits[w] : cells.Count;
                _juice.FloatText(center, "+" + Mathf.Max(1, units), dishColors[(int)dish], 1f);
                if (wave > 1) _juice.FloatText(center + Vector3.up * 0.45f, "x" + wave, Color.white, 0.8f);

                yield return new WaitForSeconds(telegraph);
                float telegraphMs = (Time.realtimeSinceStartup - t) * 1000f;

                // Canario: entre el aviso y el pop ninguna ficha cambia de plato ni de celda.
                for (int i = 0; i < cells.Count; i++)
                {
                    BoardPos p = cells[i];
                    TileView tile = Get(p);
                    if (tile != null && tile.Dish != current.At(p.X, p.Y))
                        Debug.LogError($"[TELEGRAPH] wave={wave}: ficha cambiada en {p}");
                }

                // 4) POP: recien ahora explota, con flash blanco.
                t = Time.realtimeSinceStartup;
                float maxDelay = 0f;
                for (int i = 0; i < cells.Count; i++)
                {
                    BoardPos p = cells[i];
                    TileView tile = Get(p);
                    if (tile == null) continue;

                    float delay = wave == 1 && swap
                        ? (Mathf.Abs(p.X - a.Value.X) + Mathf.Abs(p.Y - a.Value.Y)) * popStagger
                        : i * popStagger;
                    maxDelay = Mathf.Max(maxDelay, delay);

                    tile.PlayPop(delay, pop);
                    _juice.Burst(tile.Home, tile.Color, particlesPerTile + (wave - 1) * cascadeParticleBonus,
                        1f + 0.15f * (wave - 1));
                }

                if (wave == 1) _juice.PlayMatch();
                else _juice.PlayCascade(wave);
                _juice.Shake(cascadeShake * wave, 0.22f + 0.04f * wave);
                if (wave >= 2) _juice.PulseBoard(transform, cascadePulse, 0.14f);

                yield return new WaitForSeconds(maxDelay + pop + 0.01f);
                float popMs = (Time.realtimeSinceStartup - t) * 1000f;

                // 6) CAIDA: cada ficha aterriza en la celda y el plato de la foto de esta ola.
                // La ultima foto ES el tablero final, asi que el SyncAll de cierre no mueve nada.
                t = Time.realtimeSinceStartup;
                BoardSnapshot next = result.Snapshots[w];
                yield return LandPhase(current, next, cells, speed);
                current = next;
                float fallMs = (Time.realtimeSinceStartup - t) * 1000f;

                Debug.Log($"[BEAT] wave={wave} settleMs={settleMs:F0} telegraphMs={telegraphMs:F0} " +
                          $"popMs={popMs:F0} fallMs={fallMs:F0}");
            }

            SyncAll();
            _animating = false;
            Debug.Log($"[BEAT] totalMs={(Time.realtimeSinceStartup * 1000f - startMs):F0} waves={waves.Count}");
            onDone?.Invoke();
        }

        /// <summary>Plato dominante del grupo: el color con el que se avisa el match.</summary>
        private Dish Dominant(IReadOnlyList<BoardPos> cells)
        {
            var count = new int[dishColors.Length];
            for (int i = 0; i < cells.Count; i++)
            {
                TileView tile = Get(cells[i]);
                if (tile != null) count[(int)tile.Dish]++;
            }

            int best = 0;
            for (int d = 1; d < count.Length; d++)
                if (count[d] > count[best]) best = d;
            return (Dish)best;
        }

        /// <summary>Caida con gravedad real, stagger por columna y squash & stretch al aterrizar.</summary>
        private IEnumerator LandPhase(BoardSnapshot from, BoardSnapshot to, IReadOnlyList<BoardPos> cleared,
            float speed)
        {
            // Las celdas limpiadas se animan siempre: su ficha desaparecio en el pop, aunque
            // la foto les devuelva el mismo plato.
            var gone = new bool[Width, Height];
            for (int i = 0; i < cleared.Count; i++) gone[cleared[i].X, cleared[i].Y] = true;

            float maxArrival = 0f;
            bool any = false;
            float fallCap = fallMaxTime * speed;

            for (int x = 0; x < Width; x++)
            {
                int changedBelow = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (!gone[x, y] && to.At(x, y) == from.At(x, y))
                    {
                        changedBelow = 0;
                        continue;
                    }

                    any = true;
                    TileView tile = Get(new BoardPos(x, y));
                    if (tile != null)
                    {
                        int distance = changedBelow + 1;
                        float duration = Mathf.Min(fallCap, fallTimePerCell * distance);
                        float delay = x * fallStagger;
                        Dish dish = to.At(x, y);
                        tile.SetDish(dish, DishSprites.For(dish), dishColors[(int)dish]);
                        tile.PlayFall(tile.Home + Vector3.up * (distance * tileSize * 1.15f), duration, delay,
                            squash, squashTime);
                        maxArrival = Mathf.Max(maxArrival, delay + duration + squashTime);
                    }

                    changedBelow++;
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

        /// <summary>Foto del tablero visible: base para saber que ficha cae en cada celda.</summary>
        private BoardSnapshot Snapshot()
        {
            var grid = new Dish[Width, Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++) grid[x, y] = _tiles[x, y].Dish;
            }

            return BoardSnapshot.Capture(grid);
        }

        private Vector3 Centroid(IReadOnlyList<BoardPos> cells)
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

        // ---- API para el cafe -------------------------------------------------------

        /// <summary>Color del plato, compartido con banco y platos que vuelan.</summary>
        public Color DishColor(Dish dish) =>
            dishColors[Mathf.Clamp((int)dish, 0, dishColors.Length - 1)];

        /// <summary>Origen world de los platos que viajan del tablero al asiento.</summary>
        public Vector3 BoardCenter => transform.position;

        /// <summary>Texto flotante en world space (pago y feedback del suelo).</summary>
        public void ShowFloatText(Vector3 position, string text, Color color, float scale)
        {
            if (_juice != null) _juice.FloatText(position, text, color, scale);
        }
    }
}
