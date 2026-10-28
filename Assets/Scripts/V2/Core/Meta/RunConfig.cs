using System;
using System.Collections.Generic;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Todo lo que una run necesita para configurarse antes de que arranquen las capas de
    /// Unity: contrato, semilla, party e instrucciones guionadas. Es data pura.
    /// </summary>
    public sealed class RunConfig
    {
        public const int DefaultBoardWidth = 7;
        public const int DefaultBoardHeight = 7;
        public const int DefaultTileTypes = 5;

        public RunConfig(ContractDefinition contract, int seed, IEnumerable<string> partyIds = null,
            int boardWidth = DefaultBoardWidth, int boardHeight = DefaultBoardHeight,
            int tileTypes = DefaultTileTypes, IEnumerable<ScriptedOrder> scriptedOrders = null)
        {
            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            Seed = seed;
            BoardWidth = boardWidth > 0 ? boardWidth : DefaultBoardWidth;
            BoardHeight = boardHeight > 0 ? boardHeight : DefaultBoardHeight;
            TileTypes = tileTypes > 0 ? tileTypes : DefaultTileTypes;
            PartyIds = CopyIds(partyIds);
            ScriptedOrders = CopyOrders(scriptedOrders);
        }

        public ContractDefinition Contract { get; }
        public int Seed { get; }
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public int TileTypes { get; }
        public IReadOnlyList<string> PartyIds { get; }
        public IReadOnlyList<ScriptedOrder> ScriptedOrders { get; }

        public ContractModifiers Modifiers => Contract.Modifiers ?? ContractModifiers.Neutral();

        public float DurationSeconds =>
            Contract.TimeLimitSeconds > 0f ? Contract.TimeLimitSeconds : ContractDefinition.DefaultTimeLimitSeconds;

        public bool FailEnabled => Contract.FailEnabled;

        public float PatienceSeconds => Contract.EffectivePatienceSeconds;

        public static RunConfig Create(ContractDefinition contract, int seed,
            IEnumerable<string> partyIds = null, IEnumerable<ScriptedOrder> scriptedOrders = null) =>
            new RunConfig(contract, seed, partyIds, DefaultBoardWidth, DefaultBoardHeight, DefaultTileTypes,
                scriptedOrders);

        private static string[] CopyIds(IEnumerable<string> ids)
        {
            if (ids == null) return Array.Empty<string>();

            var list = new List<string>();
            foreach (string id in ids)
                if (!string.IsNullOrWhiteSpace(id)) list.Add(id.Trim());
            return list.ToArray();
        }

        private static ScriptedOrder[] CopyOrders(IEnumerable<ScriptedOrder> orders)
        {
            if (orders == null) return Array.Empty<ScriptedOrder>();

            var list = new List<ScriptedOrder>(orders);
            return list.ToArray();
        }
    }
}
