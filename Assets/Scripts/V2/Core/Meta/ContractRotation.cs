using System;
using System.Collections.Generic;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Rotación del contrato destacado a partir del cursor <c>lastContractId</c>. Modelo
    /// puro y sin estado: el llamador le pasa sólo los ids jugables (nunca locked) y el
    /// cursor persistido; la vista no participa. Sin dependencias del motor: vive en el
    /// asmdef <c>WaifuCafe.Core</c> (<c>noEngineReferences</c>).
    /// </summary>
    public static class ContractRotation
    {
        /// <summary>
        /// Siguiente contrato a destacar. Sin jugables devuelve <c>null</c>; sin historial
        /// (cursor nulo/blanco/desconocido) devuelve el primero jugable; con historial
        /// avanza al siguiente con wrap. Comparación de ids OrdinalIgnoreCase.
        /// </summary>
        public static string NextFeatured(IReadOnlyList<string> playableIds, string lastPlayedId)
        {
            if (playableIds == null || playableIds.Count == 0) return null;

            string cursor = lastPlayedId == null ? string.Empty : lastPlayedId.Trim();
            List<string> playable = null;
            int cursorIndex = -1;

            for (int i = 0; i < playableIds.Count; i++)
            {
                string id = playableIds[i];
                if (string.IsNullOrWhiteSpace(id)) continue;

                id = id.Trim();
                playable ??= new List<string>(playableIds.Count);

                if (cursorIndex < 0 && cursor.Length > 0 &&
                    string.Equals(id, cursor, StringComparison.OrdinalIgnoreCase))
                {
                    cursorIndex = playable.Count;
                }

                playable.Add(id);
            }

            if (playable == null || playable.Count == 0) return null;
            if (cursorIndex < 0) return playable[0];
            return playable[(cursorIndex + 1) % playable.Count];
        }
    }
}
