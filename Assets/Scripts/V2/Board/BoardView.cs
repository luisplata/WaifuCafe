using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using WaifuCafe.Core;

namespace V2.Board
{
    /// <summary>
    /// Vista del tablero en world space: un <see cref="TileView"/> por celda, sprites
    /// generados en runtime. La gravedad NO se anima por celda: clear + pulse + snap
    /// desde el modelo (mitigacion del design). La vista no decide validez de jugadas.
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

        private const float PulseTime = 0.12f;
        private const float RejectTime = 0.18f;
        private const float RejectPush = 0.22f;

        private TileView[,] _tiles;
        private BoardSession _session;
        private UnityEngine.Camera _camera;
        private TileView _selected;
        private TileView _dragOrigin;
        private bool _dragConsumed;
        private bool _animating;

        private int Width => _tiles?.GetLength(0) ?? 0;
        private int Height => _tiles?.GetLength(1) ?? 0;

        public bool IsInteractive => _session != null && !_animating && !_session.IsResolving;

        private void Awake() => _camera = UnityEngine.Camera.main;

        /// <summary>La sesion es la duena del modelo; la vista solo lo refleja.</summary>
        public void Attach(BoardSession session)
        {
            _session = session;
            if (_camera == null) _camera = UnityEngine.Camera.main;
            BuildBackdrop(session.Board.Width, session.Board.Height);
            BuildTiles(session.Board.Width, session.Board.Height);
            SyncAll();
        }

        /// <summary>Re-sincroniza las 49 fichas desde el modelo. Barato y sin estado propio.</summary>
        public void SyncAll()
        {
            if (_tiles == null || _session == null) return;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var tile = _tiles[x, y];
                    if (tile == null) continue;

                    Dish dish = _session.Board.At(x, y);
                    tile.SetDish(dish, DishSprites.For(dish), dishColors[(int)dish]);
                    tile.SetSelected(false);
                }
            }

            _selected = null;
        }

        // ---- Input (EventSystem / Input System) -------------------------------------

        public void HandleClick(TileView tile)
        {
            if (!IsInteractive) return;

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
            _dragOrigin = IsInteractive ? tile : null;
            _dragConsumed = false;
            if (_dragOrigin != null) Select(tile);
        }

        /// <summary>Swipe: al entrar en una celda adyacente se dispara el swap y se corta el drag.</summary>
        public void HandleDrag(PointerEventData eventData)
        {
            if (_dragOrigin == null || _dragConsumed || !IsInteractive) return;

            BoardPos? cell = CellAt(ScreenToWorld(eventData.position));
            if (cell == null || cell.Value.Equals(_dragOrigin.Pos)) return;
            if (!AreAdjacent(_dragOrigin.Pos, cell.Value)) return;

            _dragConsumed = true;
            BoardPos from = _dragOrigin.Pos;
            Select(null);
            _session.TryMove(from, cell.Value);
        }

        public void HandleEndDrag()
        {
            _dragOrigin = null;
            _dragConsumed = false;
        }

        private void Select(TileView tile)
        {
            if (_selected != null) _selected.SetSelected(false);
            _selected = tile;
            if (_selected != null) _selected.SetSelected(true);
        }

        // ---- Feedback ---------------------------------------------------------------

        /// <summary>Swap aceptado: pulse corto de las dos fichas y snap del modelo resuelto.</summary>
        public void PlayResolve(BoardPos a, BoardPos b, Action onDone) =>
            StartCoroutine(PulseThenSync(a, b, onDone));

        /// <summary>Swap rechazado (RF-1): rebote de las dos fichas, grilla intacta.</summary>
        public void PlayReject(BoardPos a, BoardPos b)
        {
            TileView ta = Get(a);
            TileView tb = Get(b);
            if (ta != null && tb != null) StartCoroutine(Nudge(ta, tb));
        }

        private IEnumerator PulseThenSync(BoardPos a, BoardPos b, Action onDone)
        {
            _animating = true;
            TileView ta = Get(a);
            TileView tb = Get(b);

            float elapsed = 0f;
            while (elapsed < PulseTime)
            {
                elapsed += Time.deltaTime;
                float factor = 1f + 0.25f * Mathf.Sin(Mathf.Clamp01(elapsed / PulseTime) * Mathf.PI);
                if (ta != null) ta.Pulse(factor);
                if (tb != null) tb.Pulse(factor);
                yield return null;
            }

            if (ta != null) ta.Pulse(1f);
            if (tb != null) tb.Pulse(1f);

            SyncAll();
            _animating = false;
            onDone?.Invoke();
        }

        private IEnumerator Nudge(TileView a, TileView b)
        {
            _animating = true;
            Vector3 posA = a.transform.position;
            Vector3 posB = b.transform.position;
            Vector3 push = (posB - posA).normalized * (tileSize * RejectPush);
            float half = RejectTime * 0.5f;

            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / half);
                a.transform.position = posA + push * k;
                b.transform.position = posB - push * k;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / half);
                a.transform.position = posA + push * (1f - k);
                b.transform.position = posB - push * (1f - k);
                yield return null;
            }

            a.transform.position = posA;
            b.transform.position = posB;
            _animating = false;
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
                    renderer.sortingOrder = 1;

                    var collider = go.AddComponent<BoxCollider2D>();
                    collider.size = Vector2.one * 0.96f;

                    var tile = go.AddComponent<TileView>();
                    tile.Bind(this, new BoardPos(x, y), tileSize);
                    go.transform.position = CellToWorld(new BoardPos(x, y));
                    _tiles[x, y] = tile;
                }
            }
        }

        private void BuildBackdrop(int width, int height)
        {
            var go = new GameObject("BoardBackdrop");
            go.transform.SetParent(transform, false);
            go.transform.position = transform.position + Vector3.forward * 0.5f;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = DishSprites.Solid();
            renderer.color = new Color(0.10f, 0.08f, 0.12f);
            renderer.sortingOrder = 0;
            go.transform.localScale = new Vector3(width * tileSize + 0.35f, height * tileSize + 0.35f, 1f);
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
