using System;
using UnityEngine;
using WaifuCafe.Core;

namespace V2.Board
{
    /// <summary>
    /// Unica orquestadora de una jugada: modelo puro -> vista -> entrega de unidades.
    /// Un solo camino para todo lo que produce unidades (hoy el swap; las skills lo
    /// reusaran via <see cref="Deliver"/>). Sin economia ni pedidos: eso llega despues.
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

        /// <summary>El tablero cambio: la vista ya se re-sincronizo.</summary>
        public event Action BoardChanged;

        private void Start()
        {
            if (view == null) view = GetComponentInChildren<BoardView>();
            Board = NewBoard();
            view.Attach(this);
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

            IsResolving = true;
            view.PlayResolve(a, b, () => CompleteMove(result));
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

        private void CompleteMove(ResolveResult result)
        {
            EnsurePlayable();
            IsResolving = false;
            Deliver(result);
            BoardChanged?.Invoke();
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
