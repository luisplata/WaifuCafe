using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    /// <summary>Acceso a los contratos jugables por id estable.</summary>
    public interface IContractCatalog
    {
        IReadOnlyList<ContractSO> All { get; }
        ContractSO Find(string id);
    }

    /// <summary>Lanzador de runs: guarda el pedido y cambia a la escena del tablero.</summary>
    public interface IRunLauncher
    {
        void Launch(RunRequest request);
    }

    /// <summary>
    /// Composition root persistente entre MetaMenu y Prototype_Board. Construye los
    /// servicios una sola vez, se auto-destruye si hay un duplicado y no usa
    /// <c>FindFirstObjectByType</c>: los coordinadores lo reciben por <see cref="Instance"/>
    /// o por <c>Initialize(...)</c>.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class GameRoot : MonoBehaviour, IRunLauncher
    {
        public static GameRoot Instance { get; private set; }

        [SerializeField] private WaifuCatalogSO waifuCatalog;

        [Tooltip("Contratos que ofrece el menú (Rush, Noche, Hora del Té…).")]
        [SerializeField] private ContractSO[] contracts = Array.Empty<ContractSO>();

        private WaifuCatalogSO _catalog;
        private ContractCatalog _contracts;
        private IProfileStore _profile;
        private bool _initialized;

        public bool IsInitialized => _initialized;
        public IWaifuCatalog Catalog => _catalog;
        public IContractCatalog Contracts => _contracts;
        public IProfileStore Profile => _profile;

        /// <summary>Run pedida por el menú; la consume una sola vez el RunCoordinator.</summary>
        public RunRequest PendingRun { get; private set; }

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
            _contracts = new ContractCatalog(contracts);

            if (_catalog == null)
            {
                Debug.LogError("[GameRoot] Falta asignar un WaifuCatalogSO; no se construyen servicios.");
                return false;
            }

            _profile = profileStore ?? new ProfileService(_catalog, _contracts);
            GameplayLock.Reset();
            _initialized = true;
            return true;
        }

        /// <summary>
        /// Flujo del menú: deja la run pendiente y carga el tablero. La escena de run
        /// consume el pedido una sola vez; si nadie lo consume, la próxima la reemplaza.
        /// </summary>
        public void Launch(RunRequest request)
        {
            if (request == null)
            {
                Debug.LogError("[GameRoot] Launch con request nulo; no se carga la escena.");
                return;
            }

            PendingRun = request;
            if (!Application.CanStreamedLevelBeLoaded(request.SceneName))
            {
                Debug.LogError($"[GameRoot] La escena '{request.SceneName}' no está en Build Settings.");
                return;
            }

            SceneManager.LoadScene(request.SceneName);
        }

        /// <summary>One-shot: el RunCoordinator lo toma y lo limpia.</summary>
        public RunRequest ConsumeRunRequest()
        {
            RunRequest request = PendingRun;
            PendingRun = null;
            return request;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            PendingRun = null;
            _profile = null;
            _catalog = null;
            _contracts = null;
            _initialized = false;
        }
    }
}
