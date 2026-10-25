using System;
using UnityEngine;
using WaifuCafe.Core;

namespace V2.Board
{
    /// <summary>
    /// Sprites provisorios generados en runtime (sin arte final, sin assets nuevos).
    /// Cada plato tiene forma propia ademas del color, para distinguirlos sin texto.
    /// </summary>
    internal static class DishSprites
    {
        private const int Size = 64;

        private static readonly Sprite[] Cache = new Sprite[5];
        private static Sprite _solid;

        public static Sprite For(Dish dish)
        {
            int index = (int)dish;
            if (Cache[index] == null) Cache[index] = Build(dish);
            return Cache[index];
        }

        /// <summary>Cuadrado blanco de 1x1 unidad: backdrop y relleno generico.</summary>
        public static Sprite Solid()
        {
            if (_solid != null) return _solid;

            var texture = NewTexture(4);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            _solid = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _solid;
        }

        private static Sprite Build(Dish dish)
        {
            var texture = NewTexture(Size);
            var pixels = new Color32[Size * Size];
            var white = new Color32(255, 255, 255, 255);
            var clear = new Color32(255, 255, 255, 0);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float nx = (x + 0.5f) / Size * 2f - 1f;
                    float ny = (y + 0.5f) / Size * 2f - 1f;
                    pixels[y * Size + x] = Inside(dish, nx, ny) ? white : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        private static Texture2D NewTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "DishShape",
            };
            return texture;
        }

        /// <summary>Formas normalizadas en [-1,1]: cuadrado, circulo, rombo, triangulo y hexagono.</summary>
        private static bool Inside(Dish dish, float x, float y)
        {
            switch (dish)
            {
                case Dish.Desayuno: // cuadrado redondeado
                    return Mathf.Abs(x) <= 0.8f && Mathf.Abs(y) <= 0.8f;
                case Dish.Almuerzo: // circulo
                    return x * x + y * y <= 0.78f * 0.78f;
                case Dish.Bebida: // rombo
                    return Mathf.Abs(x) + Mathf.Abs(y) <= 0.92f;
                case Dish.Postre: // triangulo
                    return y >= -0.62f && y <= 0.82f && Mathf.Abs(x) <= (0.9f - (y + 0.62f) * 0.62f);
                case Dish.Snack: // hexagono
                    return Mathf.Abs(y) <= 0.8f && Mathf.Abs(x) * 0.866f + Mathf.Abs(y) * 0.5f <= 0.72f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dish), dish, null);
            }
        }
    }
}
