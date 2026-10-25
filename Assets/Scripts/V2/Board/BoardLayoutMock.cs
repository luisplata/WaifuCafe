using UnityEngine;
using UnityEngine.InputSystem;
using WaifuCafe.Core;

namespace V2.Board
{
    /// <summary>
    /// Mockup visual del layout landscape 50/50: el tablero a un lado y la columna del
    /// cafe al otro. Todo procedural, sin logica ni colliders: no intercepta el input.
    /// El lado es un A/B de dato: <see cref="boardSide"/> espeja todo respecto de x=0.
    /// </summary>
    public sealed class BoardLayoutMock : MonoBehaviour
    {
        public enum BoardSide { Left, Right }

        [SerializeField] private BoardSide boardSide = BoardSide.Left;
        [SerializeField] private bool showMock = true;
        [Tooltip("Raiz del tablero. Si queda vacia se busca el BoardView de la escena.")]
        [SerializeField] private Transform board;

        private const float BoardCenterX = 6.72f;
        private const float Left = 0.3f;    // borde interior de la columna del cafe
        private const float Width = 12.8f;  // 0.3 -> 13.1 (camara: +-13.45)
        private const float CenterX = Left + Width * 0.5f;

        private static readonly Color HudColor = new Color(0.15f, 0.12f, 0.19f, 0.95f);
        private static readonly Color RowColor = new Color(0.21f, 0.17f, 0.27f, 0.95f);
        private static readonly Color CardColor = new Color(0.18f, 0.15f, 0.24f, 0.95f);
        private static readonly Color BarColor = new Color(0.06f, 0.05f, 0.08f, 1f);
        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.78f);

        // TextMesh: el alto visible ronda characterSize * 3.5; los labels piden altura en unidades.
        private const float TextHeightPerUnit = 3.5f;

        private static readonly Color[] DishColors =
        {
            new Color(0.95f, 0.75f, 0.30f),
            new Color(0.42f, 0.75f, 0.45f),
            new Color(0.32f, 0.62f, 0.92f),
            new Color(0.91f, 0.45f, 0.68f),
            new Color(0.93f, 0.58f, 0.30f),
        };

        private static Font _font;
        private Transform _root;
        private BoardView _view;
        private bool _builtShow;
        private int _builtSide = -1;

        private float Sign => boardSide == BoardSide.Left ? 1f : -1f;
        private float Mirror(float x) => x * Sign;

        private void Start() => Apply();

        // Toggle en vivo (inspector o tecla) sin reiniciar el play mode.
        private void Update()
        {
            if (Keyboard.current != null &&
                (Keyboard.current.lKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame) &&
                (_view == null || _view.IsInteractive))
            {
                boardSide = boardSide == BoardSide.Left ? BoardSide.Right : BoardSide.Left;
                Apply();
                return;
            }

            if (showMock != _builtShow || (int)boardSide != _builtSide) Apply();
        }

        private void OnValidate()
        {
            // En play, Update detecta el cambio y reconstruye; en edit, solo se mueve el tablero.
            if (!Application.isPlaying) ApplyBoardSide();
        }

        private void Apply()
        {
            _builtShow = showMock;
            _builtSide = (int)boardSide;
            ApplyBoardSide();

            if (_root != null) Destroy(_root.gameObject);
            _root = null;
            if (!showMock) return;

            _root = new GameObject("MockBlocks").transform;
            _root.SetParent(transform, false);

            BuildHud();
            for (int i = 0; i < 5; i++) BuildCustomer(i);
            for (int i = 0; i < 4; i++) BuildWaifu(i);
            BuildDivider();
        }

        /// <summary>Espeja el tablero entero respecto de x=0 y refresca los Home de las fichas.</summary>
        private void ApplyBoardSide()
        {
            if (board == null)
            {
                _view = FindFirstObjectByType<BoardView>();
                if (_view != null) board = _view.transform;
            }

            if (board == null) return;
            if (_view == null) _view = board.GetComponent<BoardView>();

            Vector3 p = board.position;
            board.position = new Vector3(boardSide == BoardSide.Left ? -BoardCenterX : BoardCenterX, p.y, p.z);
            if (_view != null) _view.RefreshHomes();
        }

        private void BuildHud()
        {
            Block("HUD", Mirror(CenterX), 7.15f, Width, 0.6f, HudColor, 1);
            Label("HUD / cola de pedidos - 12.8 x 0.6u", Mirror(Left + 0.25f), 7.15f, 0.30f);
        }

        private void BuildCustomer(int i)
        {
            float y = 6.4f - 0.85f - i * 1.85f; // 5 filas de 1.7u con 0.15u de gap
            Block("Cliente_" + (i + 1), Mirror(CenterX), y, 12.8f, 1.7f, RowColor, 1);
            Label($"Cliente {i + 1} - 12.8 x 1.7u", Mirror(Left + 0.3f), y, 0.32f);

            // Pedido simulado: 3 cuadraditos de colores de plato.
            for (int k = 0; k < 3; k++)
                Block("Pedido", Mirror(12.75f - k * 0.5f), y, 0.34f, 0.34f, DishColors[(i + k) % 5], 2);
        }

        private void BuildWaifu(int i)
        {
            float x = CenterX - 6.375f + 1.5f + i * 3.25f; // 4 cartas de 3u con gap 0.25u
            Block("Waifu_" + (i + 1), Mirror(x), -5f, 3.0f, 4.0f, CardColor, 1);
            Label($"Waifu {i + 1} · 3x4u", Mirror(x - 1.35f), -3.32f, 0.20f);

            // Retrato simulado (circulo) + barra de cooldown vacia.
            Block("Retrato", Mirror(x), -4.85f, 1.7f, 1.7f, DishColors[i % 5], 2,
                DishSprites.For(Dish.Almuerzo));
            Block("Cooldown_bg", Mirror(x), -6.35f, 2.4f, 0.28f, BarColor, 2);
            Block("Cooldown_frame", Mirror(x), -6.35f, 2.4f, 0.28f, new Color(1f, 1f, 1f, 0.35f), 3,
                DishSprites.RoundFrame());
        }

        private void BuildDivider()
        {
            Block("Mitad", 0f, 0f, 0.03f, 15.1f, new Color(1f, 1f, 1f, 0.30f), 5,
                DishSprites.Solid());
            Label("tablero 50% · tile 1.87u · ~136px @1080p", Mirror(Left + 0.15f), -7.25f, 0.22f);
        }

        private SpriteRenderer Block(string name, float x, float y, float w, float h, Color color,
            int order, Sprite sprite = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : DishSprites.RoundRect();
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private void Label(string text, float x, float y, float size)
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) return;

            var go = new GameObject("Label");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(x, y, 0f);

            var mesh = go.AddComponent<TextMesh>();
            mesh.font = _font;
            mesh.fontSize = 64;
            mesh.characterSize = size / TextHeightPerUnit;
            mesh.anchor = Sign > 0f ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            mesh.alignment = Sign > 0f ? TextAlignment.Left : TextAlignment.Right;
            mesh.fontStyle = FontStyle.Bold;
            mesh.text = text;
            mesh.color = TextColor;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = _font.material;
                renderer.sortingOrder = 30;
            }
        }
    }
}
