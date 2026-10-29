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
        private List<WaifuKit> _partyKits = new List<WaifuKit>();
        private PartyModifiers _partyModifiers = PartyModifiers.Neutral();

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

            _partyKits = BuildPartyKits(config.PartyIds);
            _partyModifiers = SynergyModel.Build(_partyKits, config.Modifiers);

            if (cafeFloor != null)
            {
                cafeFloor.Configure(config);
                cafeFloor.SetOrderCoinBonus(_partyModifiers.OrderCoinBonus);
                cafeFloor.SetPatienceMultiplier(_partyModifiers.PatienceMultiplier);
            }
            else
            {
                Debug.LogError("[RunCoordinator] Falta asignar CafeFloor.");
            }

            ConfigureParty(config, _partyModifiers);
            Subscribe();

            EnsureUi();
            _result.Hide();
            _hud.Build(config);
            _hud.SetUltimate(UltimateBadge(), IsUltimateLive());
            _hud.Refresh(_state.Progress, config);
        }

        private void Update()
        {
            if (!_running || _ended) return;

            if (!GameplayLock.IsLocked) _state.ElapsedSeconds += Time.deltaTime;
            _state.Coins = cafeFloor != null ? cafeFloor.Coins : _state.Coins;

            Evaluate();
        }

        private void ConfigureParty(RunConfig config, PartyModifiers modifiers)
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

            partyView.Configure(definitions.ToArray(), modifiers, cafeFloor);
        }

        /// <summary>
        /// Kits puros de la party de la run (estrellas del perfil). Sin GameRoot no hay
        /// sinergia: free-play conserva las reglas base.
        /// </summary>
        private List<WaifuKit> BuildPartyKits(IReadOnlyList<string> ids)
        {
            var kits = new List<WaifuKit>();
            GameRoot root = GameRoot.Instance;
            IWaifuCatalog catalog = root != null ? root.Catalog : null;
            if (catalog == null || ids == null) return kits;

            PlayerProfile profile = root.Profile != null ? root.Profile.Current : null;
            foreach (string id in ids)
            {
                WaifuDefinitionSO def = catalog.Find(id);
                if (def == null) continue;

                kits.Add(new WaifuKit
                {
                    Id = def.Id,
                    Faction = def.Faction,
                    Role = def.Role,
                    Element = def.Element,
                    Active = def.SkillArchetype,
                    Passive = def.PassiveId,
                    Stars = StarsOf(profile, def.Id, def.BaseStars),
                    ChargeCost = def.ChargeCost,
                    CooldownMatches = def.CooldownMatches,
                    BasePotency = def.Potency,
                });
            }

            return kits;
        }

        private static int StarsOf(PlayerProfile profile, string id, int fallback)
        {
            if (profile == null || profile.owned == null || string.IsNullOrWhiteSpace(id))
                return WaifuStars.Clamp(fallback);

            foreach (StarterEntry entry in profile.owned)
            {
                if (entry != null && string.Equals(entry.waifuId, id, StringComparison.OrdinalIgnoreCase))
                    return WaifuStars.Clamp(entry.stars);
            }

            return WaifuStars.Clamp(fallback);
        }

        private string UltimateBadge() =>
            SynergyModel.BadgeText(_partyModifiers.Ultimate, _partyKits);

        private bool IsUltimateLive() =>
            _partyModifiers.Ultimate.IsReady && _partyModifiers.Ultimate.Live;

        private void Subscribe()
        {
            if (cafeFloor != null)
            {
                cafeFloor.OrderServed += OnOrderServed;
                cafeFloor.OrderLost += OnOrderLost;
                cafeFloor.CoinsChanged += OnCoinsChanged;
            }

            if (boardSession != null) boardSession.Delivered += OnBoardDelivered;
            if (partyView != null)
            {
                partyView.SkillCast += OnSkillCast;
                partyView.UltimateCast += OnUltimateCast;
            }
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
            if (partyView != null)
            {
                partyView.SkillCast -= OnSkillCast;
                partyView.UltimateCast -= OnUltimateCast;
            }
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

            // Espectáculo 3 + contrato: monedas por resolución de 3 o más cascadas.
            if (result.Cascades >= 3 && cafeFloor != null && _partyModifiers.CascadeCoinBonus > 0)
            {
                cafeFloor.AddBonusCoins(_partyModifiers.CascadeCoinBonus);
                _state.Coins = cafeFloor.Coins;
            }

            Evaluate();
        }

        private void OnSkillCast(int slot)
        {
            if (!_running || _ended) return;
            _state.SkillsCast++;
            Evaluate();
        }

        private void OnUltimateCast(string title)
        {
            if (!_running || _ended || string.IsNullOrWhiteSpace(title)) return;
            _state.UltimateUsed = title;
            _hud?.SetUltimate("ULTIMATE: " + title + " — USADA", false);
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

            bool tutorial = string.Equals(result.ContractId, TutorialFlow.ContractId,
                StringComparison.Ordinal);

            // La run del tutorial no es un contrato del menú: no ensucia lastContractId.
            if (!tutorial) profile.lastContractId = result.ContractId;

            // Victoria del tutorial: el menú retoma en Reward (Hana + Yuki) al volver.
            if (tutorial && result.IsVictory && profile.tutorialStep == TutorialStep.ScriptedRun)
                profile.tutorialStep = TutorialStep.Reward;

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
