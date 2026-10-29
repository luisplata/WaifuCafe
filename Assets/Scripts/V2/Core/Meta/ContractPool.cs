using System;
using System.Collections.Generic;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Pool visible de contratos jugables a partir del cursor <c>lastContractId</c>. Modelo
    /// puro y sin estado: el llamador pasa sólo los ids jugables (nunca locked) y el cursor
    /// persistido. La ventana arranca en el contrato siguiente al último jugado, con wrap y
    /// sin repetir ids cuando <c>size</c> cubre el catálogo entero. Sin dependencias del
    /// motor: vive en el asmdef <c>WaifuCafe.Core</c> (<c>noEngineReferences</c>).
    /// </summary>
    public static class ContractPool
    {
        /// <summary>
        /// Ventana de hasta <paramref name="size"/> ids. Sin jugables o <c>size &lt;= 0</c>
        /// devuelve una lista vacía; sin cursor (nulo/blanco/desconocido) arranca en el
        /// primero; con cursor avanza al siguiente con wrap; si <c>size</c> cubre el
        /// catálogo devuelve el ciclo completo una sola vez. Comparación OrdinalIgnoreCase.
        /// </summary>
        public static IReadOnlyList<string> Build(IReadOnlyList<string> playableIds,
            string lastPlayedId, int size)
        {
            if (playableIds == null || playableIds.Count == 0 || size <= 0)
                return Array.Empty<string>();

            string cursor = lastPlayedId == null ? string.Empty : lastPlayedId.Trim();

            var playable = new List<string>(playableIds.Count);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int cursorIndex = -1;

            for (int i = 0; i < playableIds.Count; i++)
            {
                string id = playableIds[i];
                if (string.IsNullOrWhiteSpace(id)) continue;

                id = id.Trim();
                if (!seen.Add(id)) continue;

                if (cursorIndex < 0 && cursor.Length > 0 &&
                    string.Equals(id, cursor, StringComparison.OrdinalIgnoreCase))
                {
                    cursorIndex = playable.Count;
                }

                playable.Add(id);
            }

            if (playable.Count == 0) return Array.Empty<string>();

            int take = Math.Min(size, playable.Count);
            int start = cursorIndex >= 0 ? (cursorIndex + 1) % playable.Count : 0;

            var result = new string[take];
            for (int i = 0; i < take; i++)
                result[i] = playable[(start + i) % playable.Count];

            return result;
        }
    }
}
