using System;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Pedido de lanzamiento que viaja del menú a la escena de run. <see cref="Config"/>
    /// ya está armado (contrato + semilla + party), así el RunCoordinator no consulta
    /// servicios del menú.
    /// </summary>
    public sealed class RunRequest
    {
        public const string DefaultSceneName = "Prototype_Board";

        public RunRequest(RunConfig config, string sceneName = DefaultSceneName)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? DefaultSceneName : sceneName.Trim();
        }

        public RunConfig Config { get; }
        public string SceneName { get; }
    }
}
