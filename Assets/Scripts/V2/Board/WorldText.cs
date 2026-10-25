using TMPro;
using UnityEngine;

namespace V2.Board
{
    /// <summary>
    /// Texto world-space con TextMeshPro usando la fuente del proyecto. TMP mide en
    /// puntos, no en unidades de mundo: despues de fijar el texto se normaliza la
    /// altura con los bounds reales. Fallback: la fuente default de TMP.
    /// </summary>
    internal static class WorldText
    {
        private const string FontResource = "Fonts & Materials/LiberationSans SDF";

        private static TMP_FontAsset _font;
        private static bool _warned;

        public static TextMeshPro Create(Transform parent, string text, Color color, float height, int order)
        {
            if (_font == null) _font = LoadFont();
            if (_font == null) return null;

            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = _font;
            tmp.text = string.IsNullOrEmpty(text) ? "Ag" : text;
            tmp.color = color;
            tmp.fontSize = 36f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;

            var renderer = tmp.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sortingOrder = order;

            tmp.ForceMeshUpdate();
            float measured = tmp.textBounds.size.y;
            if (measured > 0.001f) go.transform.localScale = Vector3.one * (height / measured);

            tmp.text = text;
            tmp.ForceMeshUpdate();
            return tmp;
        }

        private static TMP_FontAsset LoadFont()
        {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>(FontResource);
            if (font == null) font = TMP_Settings.defaultFontAsset;
            if (font == null && !_warned)
            {
                _warned = true;
                Debug.LogWarning("WorldText: no hay fuente TMP disponible; los textos no se dibujan.");
            }

            return font;
        }
    }
}
