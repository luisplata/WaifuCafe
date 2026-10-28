using UnityEngine;
using WaifuCafe.Core;
using WaifuCafe.Core.Meta;

namespace V2.Waifu
{
    /// <summary>
    /// Una waifu como dato puro: identidad, elemento, rol, carga y skill declarada.
    /// Agregar una waifu = agregar un asset, sin tocar codigo (RF-13). Las estrellas
    /// actuales NO viven aca: el perfil guarda <c>owned[].stars</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "WaifuCafe/Waifu", fileName = "Waifu")]
    public sealed class WaifuDefinitionSO : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Id estable para el perfil y el catalogo (minusculas, sin espacios).")]
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = "Waifu";
        [SerializeField] private WaifuFaction faction = WaifuFaction.Cafe;
        [SerializeField] private WaifuRarity rarity = WaifuRarity.Comun;

        [Header("Estrellas (escala del asset; las actuales viven en el perfil)")]
        [SerializeField, Min(1)] private int baseStars = 1;
        [SerializeField, Min(1)] private int maxStars = 3;

        [Tooltip("Color del retrato procedural (sin arte final).")]
        [SerializeField] private Color portraitColor = Color.white;

        [Header("Elemento y rol")]
        [SerializeField] private Dish element = Dish.Almuerzo;
        [SerializeField] private WaifuRole role = WaifuRole.Servicio;

        [Header("Carga compartida")]
        [Tooltip("Unidades de su elemento para quedar lista.")]
        [SerializeField] private int chargeCost = 10;
        [SerializeField] private int cooldownMatches;

        [Header("Skill (datos; sin comportamiento en PR6)")]
        [SerializeField] private SkillArchetype skillArchetype = SkillArchetype.InstantServeUnits;
        [SerializeField] private int potency = 4;
        [SerializeField] private PassiveId passiveId = PassiveId.None;

        [Header("Arte (opcional; si falta se usa el placeholder procedural)")]
        [Tooltip("Ilustracion de la carta: fondo completo del arte legacy.")]
        [SerializeField] private Sprite cardArt;
        [Tooltip("Chibi transparente: retrato de carta, flash READY y cinematica.")]
        [SerializeField] private Sprite chibiArt;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite CardArt => cardArt;
        public Sprite ChibiArt => chibiArt;
        public WaifuFaction Faction => faction;
        public WaifuRarity Rarity => rarity;
        public int BaseStars => baseStars;
        public int MaxStars => maxStars;
        public Color PortraitColor => portraitColor;
        public Dish Element => element;
        public WaifuRole Role => role;
        public int ChargeCost => chargeCost;
        public int CooldownMatches => cooldownMatches;
        public SkillArchetype SkillArchetype => skillArchetype;
        public int Potency => potency;
        public PassiveId PassiveId => passiveId;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name.ToLowerInvariant();
            baseStars = Mathf.Clamp(baseStars, WaifuStars.Min, WaifuStars.Max);
            maxStars = Mathf.Clamp(maxStars, baseStars, WaifuStars.Max);
        }
    }
}
