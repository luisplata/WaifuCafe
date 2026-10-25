using System;

namespace WaifuCafe.Core
{
    /// <summary>
    /// Foto inmutable del tablero en un instante. Guarda una copia plana, asi que el modelo
    /// puede seguir mutando sin alterar lo que la vista esta animando.
    /// </summary>
    public sealed class BoardSnapshot
    {
        private readonly Dish[] _cells;

        public int Width { get; }
        public int Height { get; }

        private BoardSnapshot(int width, int height, Dish[] cells)
        {
            Width = width;
            Height = height;
            _cells = cells;
        }

        /// <summary>Copia la grilla: la foto no comparte estado con el modelo.</summary>
        public static BoardSnapshot Capture(Dish[,] grid)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));

            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            var cells = new Dish[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    cells[y * width + x] = grid[x, y];

            return new BoardSnapshot(width, height, cells);
        }

        public Dish At(int x, int y) => _cells[y * Width + x];
    }
}
