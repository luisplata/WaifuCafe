using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using V2.Board;
using V2.Cafe;
using V2.Helpers;
using V2.Waifu;
using WaifuCafe.Core;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// Orquesta una run de contrato sobre el tablero existente. Corre antes que los
    /// <c>Start</c> de BoardSession/CafeFloor/WaifuPartyView (orden -150) para configurarlos,
    /// escucha pedidos/skills/fichas y evalúa victoria/derrota con el evaluador puro.
    /// Sin GameRoot (abrir Prototype_Board directo) no configura nada: free-play puro y
    /// cero escrituras de perfil.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class RunCoordinator : MonoBehaviour
    {
        [Header("Referencias de la escena (sin FindFirstObjectByType)")]
        [SerializeField] private BoardSession boardSession;
        [SerializeField] private CafeFloor cafeFloor;
        [SerializeField] private WaifuPartyView partyView;

        [Header("Dev / fallback")]
        [Tooltip("Contrato de tuning. Solo se usa desde el menú o con StartDevRun(); abrir la escena directo sigue siendo free-play.")]
        [SerializeField] private ContractSO devContract;

        [Tooltip("Escena del menú a la que vuelve 'Volver al menú'.")]
        [SerializeField] private string menuScene = "MetaMenu";

        private RunConfig _config;
        private RunState _state;
        private RunStatus _status = RunStatus.Playing;
        private RunOutcome _outcome = RunOutcome.None;
        private bool _running;
        private bool _ended;
        private RunHud _hud;
        private ResultScreen _result;

        public bool IsRunning => _running;
        public bool HasEnded => _ended;
        public RunStatus Status => _status;
        public RunOutcome Outcome => _outcome;
        public RunConfig Config => _config;
        public RunState State => _state;

        private void Awake()
        {
            GameplayLock.Reset();

            RunRequest request = GameRoot.Instance != null ? GameRoot.Instance.ConsumeRunRequest() : null;
            if (request != null)
            {
                Begin(request.Config);
                return;
            }

            // Sin request: si hay GameRoot (se llegó desde el menú) el contrato dev es el
            // fallback de tuning. Sin GameRoot (apertura directa) no se toca nada.
            if (GameRoot.Instance != null && devContract != null) Begin(devContract.ToRunConfig(0, null));
        }

        /// <summary>
        /// Dev/tuning: arranca el contrato serializado sin pasar por el menú. No es el
        /// camino normal y no escribe perfil si no hay GameRoot.
        /// </summary>
        [ContextMenu("Dev: iniciar contrato")]
        public void StartDevRun()
        {
            if (devContract == null)
            {
                Debug.LogWarning("[RunCoordinator] No hay devContract asignado.");
                return;
            }

            StartRun(devContract, 0, null);
        }

        /// <summary>Arranca una run con un contrato concreto (menú o dev).</summary>
        public void StartRun(ContractSO contract, int seed, IList<string> partyIds)
        {
            if (contract == null)
            {
                Debug.LogError("[RunCoordinator] StartRun con contrato nulo.");
                return;
            }

            Begin(contract.ToRunConfig(seed, partyIds));
        }

        private void Begin(RunConfig config)
        {
            if (config == null || config.Contract == null)
            {
                Debug.LogError("[RunCoordinator] RunConfig o contrato nulo; no se inicia la run.");
                return;
            }

            if (_running) return;

            Unsubscribe();

            _config = config;
            _state = new RunState(config.Contract);
            _status = RunStatus.Playing;
            _outcome = RunOutcome.None;
            _running = true;
            _ended = false;

            if (boardSession != null)
            {
                boardSession.Locked = false;
                boardSession.Configure(config.Seed, config.BoardWidth, config.BoardHeight, config.TileTypes);
            }
            else
            {
                Debug.LogError("[RunCoordinator] Falta asignar BoardSession.");
            }

            if (cafeFloor != null)
                cafeFloor.Configure(config);
            else
                Debug.LogError("[RunCoordinator] Falta asignar CafeFloor.");

            ConfigureParty(config);
            Subscribe();

            EnsureUi();
            _result.Hide();
            _hud.Build(config);
            _hud.Refresh(_state.Progress, config);
        }

        private void Update()
        {
            if (!_running || _ended) return;

            if (!GameplayLock.IsLocked) _state.ElapsedSeconds += Time.deltaTime;
            _state.Coins = cafeFloor != null ? cafeFloor.Coins : _state.Coins;

            Evaluate();
        }

        private void ConfigureParty(RunConfig config)
        {
            if (partyView == null) return;

            IReadOnlyList<string> ids = config.PartyIds;
            if (ids == null || ids.Count == 0)
            {
                partyView.Configure(null);
                return;
            }

            IWaifuCatalog catalog = GameRoot.Instance != null ? GameRoot.Instance.Catalog : null;
            if (catalog == null)
            {
                Debug.LogWarning("[RunCoordinator] Sin catálogo de waifus; se conserva la party de la escena.");
                return;
            }

            var definitions = new List<WaifuDefinitionSO>(ids.Count);
            foreach (string id in ids)
            {
                WaifuDefinitionSO definition = catalog.Find(id);
                if (definition == null)
                {
                    Debug.LogWarning($"[RunCoordinator] Waifu '{id}' desconocida; se omite.");
                    continue;
                }

                definitions.Add(definition);
            }

            partyView.Configure(definitions.ToArray());
        }

        private void Subscribe()
        {
            if (cafeFloor != null)
            {
                cafeFloor.OrderServed += OnOrderServed;
                cafeFloor.OrderLost += OnOrderLost;
                cafeFloor.CoinsChanged += OnCoinsChanged;
            }

            if (boardSession != null) boardSession.Delivered += OnBoardDelivered;
            if (partyView != null) partyView.SkillCast += OnSkillCast;
        }

        private void Unsubscribe()
        {
            if (cafeFloor != null)
            {
                cafeFloor.OrderServed -= OnOrderServed;
                cafeFloor.OrderLost -= OnOrderLost;
                cafeFloor.CoinsChanged -= OnCoinsChanged;
            }

            if (boardSession != null) boardSession.Delivered -= OnBoardDelivered;
            if (partyView != null) partyView.SkillCast -= OnSkillCast;
        }

        private void OnOrderServed(Order order)
        {
            if (!_running || _ended) return;
            _state.ServedOrders++;
            _state.Coins = cafeFloor != null ? cafeFloor.Coins : _state.Coins;
            Evaluate();
        }

        private void OnOrderLost(Order order)
        {
            if (!_running || _ended) return;
            _state.LostOrders++;
            Evaluate();
        }

        private void OnCoinsChanged(int delta)
        {
            if (!_running || _ended) return;
            if (cafeFloor != null) _state.Coins = cafeFloor.Coins;
        }

        private void OnBoardDelivered(ResolveResult result)
        {
            if (!_running || _ended) return;
            _state.AddCleared(result);
            Evaluate();
        }

        private void OnSkillCast(int slot)
        {
            if (!_running || _ended) return;
            _state.SkillsCast++;
            Evaluate();
        }

        private void Evaluate()
        {
            if (!_running || _ended) return;

            ContractEvaluation evaluation = ContractEvaluator.Evaluate(_config.Contract, _state);
            if (evaluation.Status != RunStatus.Playing)
            {
                Finish(evaluation);
                return;
            }

            _hud?.Refresh(evaluation.Progress, _config);
        }

        private void Finish(ContractEvaluation evaluation)
        {
            if (_ended) return;

            _ended = true;
            _running = false;
            _status = evaluation.Status;
            _outcome = evaluation.Outcome;

            Unsubscribe();

            if (boardSession != null) boardSession.Locked = true;
            if (cafeFloor != null) cafeFloor.StopRun();

            if (cafeFloor != null) _state.Coins = cafeFloor.Coins;

            RunResult result = RunResult.From(_config.Contract, _state, evaluation.Status, evaluation.Outcome);
            Persist(result);

            _hud?.SetVisible(false);
            EnsureUi();
            _result.Show(result, _config.Contract, GoToMenu);
        }

        /// <summary>
        /// Las monedas parciales se guardan SIEMPRE (soft fail), junto con el último
        /// contrato jugado. Sin GameRoot (free-play/dev directo) no se escribe nada.
        /// </summary>
        private void Persist(RunResult result)
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Profile == null) return;

            PlayerProfile profile = root.Profile.Current;
            if (profile == null) return;

            if (result.Coins > 0) profile.coins += result.Coins;
            profile.lastContractId = result.ContractId;
            root.Profile.Save();
        }

        private void GoToMenu()
        {
            bool hasMenu = !string.IsNullOrWhiteSpace(menuScene) &&
                           Application.CanStreamedLevelBeLoaded(menuScene);

            if (hasMenu)
            {
                SceneManager.LoadScene(menuScene);
                return;
            }

            // Dev sin menú (E3): se cierra el resultado sin romper la escena. Un run real
            // sin MetaMenu en Build Settings es un error de configuración (E2/E8).
            if (GameRoot.Instance != null)
                Debug.LogWarning($"[RunCoordinator] La escena '{menuScene}' no está en Build Settings.");

            _result.Hide();
            if (boardSession != null) boardSession.Locked = false;
        }

        private void EnsureUi()
        {
            if (_hud == null) _hud = new RunHud(transform);
            if (_result == null) _result = new ResultScreen(_hud.CanvasTransform);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _result?.Dispose();
            _hud?.Dispose();
        }
    }
}
