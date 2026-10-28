using System;
using System.Collections.Generic;
using UnityEngine;
using V2.Helpers;
using V2.Waifu;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>Servicio de perfil que consumen menú, run y tutorial.</summary>
    public interface IProfileStore
    {
        PlayerProfile Current { get; }
        void Save();
        void Reset();
        event Action Changed;
    }

    /// <summary>Acceso al roster completo de waifus por id estable.</summary>
    public interface IWaifuCatalog
    {
        IReadOnlyList<WaifuDefinitionSO> All { get; }
        WaifuDefinitionSO Find(string id);
    }

    /// <summary>
    /// Composition root persistente entre MetaMenu y Prototype_Board. Construye los
    /// servicios una sola vez, se auto-destruye si hay un duplicado y no usa
    /// <c>FindFirstObjectByType</c>: los coordinadores lo reciben por <see cref="Instance"/>
    /// o por <c>Initialize(...)</c>.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        [SerializeField] private WaifuCatalogSO waifuCatalog;

        private WaifuCatalogSO _catalog;
        private IProfileStore _profile;
        private bool _initialized;

        public bool IsInitialized => _initialized;
        public IWaifuCatalog Catalog => _catalog;
        public IProfileStore Profile => _profile;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        /// <summary>Idempotente. Se puede llamar desde una escena que ya trae referencias.</summary>
        public bool Initialize()
        {
            return Initialize(waifuCatalog, null);
        }

        public bool Initialize(WaifuCatalogSO catalog, IProfileStore profileStore = null)
        {
            if (_initialized) return true;

            _catalog = catalog != null ? catalog : waifuCatalog;
            if (_catalog == null)
            {
                Debug.LogError("[GameRoot] Falta asignar un WaifuCatalogSO; no se construyen servicios.");
                return false;
            }

            _profile = profileStore ?? new ProfileService(_catalog);
            GameplayLock.Reset();
            _initialized = true;
            return true;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            _profile = null;
            _catalog = null;
            _initialized = false;
        }
    }
}
