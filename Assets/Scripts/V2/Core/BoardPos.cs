using System;

namespace WaifuCafe.Core
{
    /// <summary>Coordenada de celda del tablero. X = columna, Y = fila.</summary>
    public readonly struct BoardPos : IEquatable<BoardPos>
    {
        public readonly int X;
        public readonly int Y;

        public BoardPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(BoardPos other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is BoardPos other && Equals(other);

        public override int GetHashCode() => (X * 397) ^ Y;

        public override string ToString() => $"({X},{Y})";
    }
}
