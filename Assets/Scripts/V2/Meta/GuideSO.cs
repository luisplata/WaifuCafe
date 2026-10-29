using System;
using UnityEngine;

namespace V2.Meta
{
    /// <summary>Zona del menú que el tour señala en cada página.</summary>
    public enum GuideFocus
    {
        Contracts = 0,
        Party = 1,
        Gacha = 2,
        Restaurant = 3,
    }

    /// <summary>Una página del tour del manager (título + texto + zona).</summary>
    [Serializable]
    public sealed class GuidePage
    {
        [SerializeField] private string title = string.Empty;
        [SerializeField, TextArea(2, 5)] private string body = string.Empty;
        [SerializeField] private GuideFocus focus = GuideFocus.Contracts;

        public string Title => title;
        public string Body => body;
        public GuideFocus Focus => focus;
    }

    /// <summary>
    /// Guion del tour guiado por la manager (NPC no jugable, nunca entra al roster).
    /// El orden de las páginas es el orden del tour: contratos → party → gacha → restaurante.
    /// </summary>
    [CreateAssetMenu(menuName = "WaifuCafe/Guide", fileName = "Guide")]
    public sealed class GuideSO : ScriptableObject
    {
        [Header("Manager (NPC no jugable)")]
        [SerializeField] private string managerName = "MANAGER";
        [Tooltip("Retrato del manager. Se reutiliza el arte existente de la manager.")]
        [SerializeField] private Sprite portrait;

        [Header("Páginas del tour")]
        [SerializeField] private GuidePage[] pages = Array.Empty<GuidePage>();

        public string ManagerName => string.IsNullOrWhiteSpace(managerName) ? "MANAGER" : managerName;
        public Sprite Portrait => portrait;
        public GuidePage[] Pages => pages ?? Array.Empty<GuidePage>();
    }
}
