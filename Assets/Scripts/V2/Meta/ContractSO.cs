using System;
using System.Collections.Generic;
using UnityEngine;
using WaifuCafe.Core;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// Contrato como asset: objetivo, modificadores y derrota con los números exactos del
    /// diseño. El menú lo lista y <see cref="ToRunConfig"/> lo convierte en data pura.
    /// </summary>
    [CreateAssetMenu(menuName = "WaifuCafe/Contract", fileName = "Contract")]
    public sealed class ContractSO : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Id estable (minusculas, sin espacios).")]
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string title = "Contrato";
        [SerializeField] private WaifuFaction faction = WaifuFaction.Cafe;
        [Tooltip("Preview bloqueada: se muestra pero no se puede jugar.")]
        [SerializeField] private bool locked;
        [Tooltip("Motivo visible del bloqueo (causa técnica, máx. ~60 caracteres).")]
        [SerializeField] private string lockReason = string.Empty;

        [Header("Textos de UI")]
        [SerializeField] private string goalText = string.Empty;
        [SerializeField] private string modifierText = string.Empty;
        [SerializeField] private string defeatText = string.Empty;

        [Header("Objetivo")]
        [SerializeField] private int goalServedOrders;
        [SerializeField] private int goalSkillsCast;
        [SerializeField] private Dish goalClearedDish = Dish.Postre;
        [SerializeField] private int goalClearedDishUnits;
        [SerializeField] private Dish goalClearedDish2 = Dish.Desayuno;
        [SerializeField] private int goalClearedDish2Units;
        [SerializeField] private int goalVipOrders;

        [Header("Modificadores")]
        [SerializeField] private float arrivalIntervalMultiplier = 1f;
        [SerializeField] private float coinMultiplier = 1f;
        [SerializeField] private float chargeCostMultiplier = 1f;
        [SerializeField] private float postreWeightMultiplier = 1f;
        [SerializeField] private float patienceMultiplier = 1f;
        [SerializeField] private int coinsPerCascade;
        [SerializeField] private bool ultimateEnabled = true;

        [Header("Derrota")]
        [SerializeField] private float timeLimitSeconds = ContractDefinition.DefaultTimeLimitSeconds;
        [SerializeField] private int maxLostOrders = ContractDefinition.DefaultMaxLostOrders;
        [Tooltip("False = el tiempo y los perdidos no derrotan (run de tutorial).")]
        [SerializeField] private bool failEnabled = true;
        [SerializeField] private float basePatienceSeconds = ContractDefinition.DefaultPatienceSeconds;

        public string Id => id;
        public string Title => title;
        public WaifuFaction Faction => faction;
        public bool Locked => locked;
        public string LockReason => lockReason;

        public ContractDefinition ToDefinition()
        {
            return new ContractDefinition
            {
                Id = id,
                Title = title,
                GoalText = goalText,
                ModifierText = modifierText,
                DefeatText = defeatText,
                Faction = faction,
                Locked = locked,
                Goal = new ContractGoal
                {
                    servedOrders = goalServedOrders,
                    skillsCast = goalSkillsCast,
                    clearedDish = goalClearedDish,
                    clearedDishUnits = goalClearedDishUnits,
                    clearedDish2 = goalClearedDish2,
                    clearedDish2Units = goalClearedDish2Units,
                    vipOrders = goalVipOrders,
                },
                Modifiers = new ContractModifiers
                {
                    arrivalIntervalMultiplier = arrivalIntervalMultiplier,
                    coinMultiplier = coinMultiplier,
                    chargeCostMultiplier = chargeCostMultiplier,
                    postreWeightMultiplier = postreWeightMultiplier,
                    patienceMultiplier = patienceMultiplier,
                    coinsPerCascade = coinsPerCascade,
                    ultimateEnabled = ultimateEnabled,
                },
                TimeLimitSeconds = timeLimitSeconds,
                MaxLostOrders = maxLostOrders,
                FailEnabled = failEnabled,
                BasePatienceSeconds = basePatienceSeconds,
            };
        }

        /// <summary>Run lista para el tablero con la party elegida en el menú.</summary>
        public RunConfig ToRunConfig(int seed, System.Collections.Generic.IList<string> partyIds)
        {
            return RunConfig.Create(ToDefinition(), seed, partyIds);
        }

        /// <summary>Variante con pedidos guionados (tutorial de E5).</summary>
        public RunConfig ToRunConfig(int seed, System.Collections.Generic.IList<string> partyIds,
            System.Collections.Generic.IEnumerable<ScriptedOrder> scriptedOrders)
        {
            return RunConfig.Create(ToDefinition(), seed, partyIds, scriptedOrders);
        }
    }

    /// <summary>
    /// Catálogo en memoria de los contratos serializados en <see cref="GameRoot"/>.
    /// Sin asset extra: el menú recibe la lista ya armada.
    /// </summary>
    public sealed class ContractCatalog : IContractCatalog
    {
        private readonly List<ContractSO> _all;
        private readonly Dictionary<string, ContractSO> _byId;

        public ContractCatalog(IEnumerable<ContractSO> contracts)
        {
            _all = new List<ContractSO>();
            _byId = new Dictionary<string, ContractSO>(StringComparer.OrdinalIgnoreCase);
            if (contracts == null) return;

            foreach (ContractSO contract in contracts)
            {
                if (contract == null || string.IsNullOrWhiteSpace(contract.Id)) continue;
                string key = contract.Id.Trim();
                if (_byId.ContainsKey(key))
                {
                    Debug.LogWarning($"[ContractCatalog] Id duplicado '{key}'; se conserva el primero.");
                    continue;
                }

                _byId[key] = contract;
                _all.Add(contract);
            }
        }

        public IReadOnlyList<ContractSO> All => _all;

        public ContractSO Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _byId.TryGetValue(id.Trim(), out ContractSO contract) ? contract : null;
        }
    }
}
