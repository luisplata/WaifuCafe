using System;
using UnityEngine;
using V2.Helpers;
using WaifuCafe.Core;

namespace V2.Board
{
    /// <summary>De donde salio la resolucion que se esta entregando.</summary>
    public enum ResolutionSource { Swap, Skill }

    /// <summary>
    /// Unica orquestadora de una jugada: modelo puro -> vista -> entrega de unidades.
    /// Un solo camino para todo lo que produce unidades: swaps y skills de tablero.
    /// Sin economia ni pedidos: eso vive fuera.
    /// </summary>
    public sealed class BoardSession : MonoBehaviour
    {
        [SerializeField] private BoardView view;

        [Header("Tablero")]
        [SerializeField] private int width = 7;
        [SerializeField] private int height = 7;
        [SerializeField] private int tileTypes = 5;

        [Tooltip("0 = semilla aleatoria por partida")]
        [SerializeField] private int seed;

        public BoardModel Board { get; private set; }

        /// <summary>Una jugada esta animandose: el input se ignora.</summary>
        public bool IsResolving { get; private set; }

        /// <summary>Lock de fin de dia / selector (se usa en PR posteriores).</summary>
        public bool Locked { get; set; }

        /// <summary>Unidades por plato de la ultima jugada entregada.</summary>
        public event Action<ResolveResult> Delivered;

        /// <summary>Origen de la resolucion que se esta entregando (cooldowns, feedback).</summary>
        public ResolutionSource LastSource { get; private set; }

        /// <summary>El tablero cambio: la vista ya se re-sincronizo.</summary>
        public event Action BoardChanged;

        private void Start()
        {
            if (view == null) view = GetComponentInChildren<BoardView>();
            Board = NewBoard();
            view.Attach(this);
        }

        /// <summary>
        /// Configura una run antes de <c>Start</c> (RunCoordinator usa orden -150): semilla
        /// y dimensiones del tablero. Si ya hay tablero, lo regenera en caliente. Sin
        /// llamada, el comportamiento es el free-play de siempre.
        /// </summary>
        public void Configure(int boardSeed, int boardWidth, int boardHeight, int boardTileTypes)
        {
            seed = boardSeed;
            width = Mathf.Max(3, boardWidth);
            height = Mathf.Max(3, boardHeight);
            tileTypes = Mathf.Clamp(boardTileTypes, 3, 5);

            if (Board == null) return;

            if (view == null) view = GetComponentInChildren<BoardView>();
            Board = NewBoard();
            if (view != null) view.SyncAll();
            BoardChanged?.Invoke();
        }

        /// <summary>Intento de swap adyacente. Devuelve false si se rechazo (rebote).</summary>
        public bool TryMove(BoardPos a, BoardPos b)
        {
            if (Locked || IsResolving || Board == null) return false;

            if (!AreAdjacent(a, b) || !Board.TryResolveSwap(a.X, a.Y, b.X, b.Y, out ResolveResult result))
            {
                view.PlayReject(a, b);
                return false;
            }

            BeginResolve();
            view.PlayResolve(a, b, result, () => CompleteMove(result));
            return true;
        }

        /// <summary>
        /// Unico camino de entrega de unidades. Hoy solo notifica; pedidos, party y monedas
        /// se engancharan aca sin tocar el modelo ni la vista.
        /// </summary>
        public void Deliver(ResolveResult result)
        {
            if (result.Cleared <= 0) return;
            Delivered?.Invoke(result);
        }

        /// <summary>
        /// Resolucion de una skill que YA muto el modelo: entra por las mismas olas,
        /// telegraph, cascadas y entrega que un swap. Sin olas (conversion sin match),
        /// solo re-sincroniza la vista.
        /// </summary>
        public bool ResolveSkill(ResolveResult result)
        {
            if (Locked || IsResolving || Board == null || view == null) return false;

            if (result.Cascades <= 0)
            {
                view.SyncAll();
                BoardChanged?.Invoke();
                return true;
            }

            BeginResolve();
            view.PlaySkillResolve(result, () => CompleteSkill(result));
            return true;
        }

        /// <summary>Re-sincroniza la vista tras un cambio sin resolucion (reshuffle).</summary>
        public void SyncBoard()
        {
            if (view == null) return;
            view.SyncAll();
            BoardChanged?.Invoke();
        }

        private void CompleteMove(ResolveResult result)
        {
            EnsurePlayable();
            LastSource = ResolutionSource.Swap;
            EndResolve();
            Deliver(result);
            BoardChanged?.Invoke();
        }

        private void CompleteSkill(ResolveResult result)
        {
            EnsurePlayable();
            LastSource = ResolutionSource.Skill;
            EndResolve();
            Deliver(result);
            BoardChanged?.Invoke();
        }

        /// <summary>
        /// Un solo Hold por resolucion (swap, cascada y skill comparten camino). Mientras
        /// dure, CafeFloor no acumula llegadas ni rampa; la vista sigue animando. El guard
        /// evita un segundo Hold si el mismo evento entra dos veces.
        /// </summary>
        private void BeginResolve()
        {
            if (IsResolving) return;
            IsResolving = true;
            GameplayLock.Hold();
        }

        /// <summary>Contraparte exacta de <see cref="BeginResolve"/>: libera una sola vez.</summary>
        private void EndResolve()
        {
            if (!IsResolving) return;
            IsResolving = false;
            GameplayLock.Release();
        }

        /// <summary>Destroy a mitad de una resolucion: el lock no puede quedar tomado.</summary>
        private void OnDestroy()
        {
            if (IsResolving) EndResolve();
        }

        /// <summary>
        /// RF-4: si el tablero quedo sin jugadas se regenera (el <c>Shuffle</c> in-place
        /// del core es de otro corte). Garantiza que el tablero nunca se trabe.
        /// </summary>
        private void EnsurePlayable()
        {
            if (Board.HasMove()) return;

            Board = NewBoard();
            view.SyncAll();
            Debug.LogWarning("BoardSession: tablero sin jugadas validas, regenerado.");
        }

        private BoardModel NewBoard()
        {
            int effectiveSeed = seed != 0 ? seed : Environment.TickCount;
            return BoardModel.CreateNoMatch(width, height, new SystemRandom(effectiveSeed), tileTypes);
        }

        private static bool AreAdjacent(BoardPos a, BoardPos b) =>
            Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y) == 1;
    }
}
