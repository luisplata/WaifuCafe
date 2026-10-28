using System;
using System.Collections.Generic;
using UnityEngine;
using V2.Waifu;

namespace V2.Meta
{
    /// <summary>
    /// Catálogo único de waifus por id. El asset vive en
    /// <c>Assets/ScriptableObjects/Meta/WaifuCatalog.asset</c> y reemplaza cualquier
    /// búsqueda global por tipo.
    /// </summary>
    [CreateAssetMenu(menuName = "WaifuCafe/Waifu Catalog", fileName = "WaifuCatalog")]
    public sealed class WaifuCatalogSO : ScriptableObject, IWaifuCatalog
    {
        [SerializeField] private WaifuDefinitionSO[] waifus = new WaifuDefinitionSO[0];

        private readonly Dictionary<string, WaifuDefinitionSO> _byId =
            new Dictionary<string, WaifuDefinitionSO>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<WaifuDefinitionSO> All => waifus;

        public WaifuDefinitionSO Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            EnsureIndex();
            string key = id.Trim();
            return _byId.TryGetValue(key, out WaifuDefinitionSO definition) ? definition : null;
        }

        private void OnEnable() => Rebuild();

        private void OnValidate() => Rebuild();

        private void EnsureIndex()
        {
            if (_byId.Count == 0) Rebuild();
        }

        private void Rebuild()
        {
            _byId.Clear();
            if (waifus == null) return;

            foreach (WaifuDefinitionSO definition in waifus)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id)) continue;
                string id = definition.Id.Trim();
                if (_byId.ContainsKey(id))
                {
                    Debug.LogWarning($"[WaifuCatalog] Id duplicado '{id}'; se conserva el primero.");
                    continue;
                }
                _byId[id] = definition;
            }
        }
    }
}
