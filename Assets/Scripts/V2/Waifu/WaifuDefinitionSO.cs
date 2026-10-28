using UnityEngine;
using WaifuCafe.Core;

namespace V2.Waifu
{
    public enum WaifuRole { Cocina, Servicio, Apoyo, Control }

    /// <summary>
    /// Arquetipos del slice. <see cref="DelayEviction"/> queda RESERVADO: la expulsion no
    /// existe hasta PR9, asi que ningun SO debe usarlo todavia.
    /// </summary>
    public enum SkillArchetype
    {
        ClearCross,
        ConvertToElement,
        InstantServeUnits,
        DelayEviction,
        ReshuffleBoard,
    }

    public enum PassiveId { None, ExtraCharge, CoinBonus, FasterService }

    public enum WaifuRarity { Comun, Rara, Epica }

    /// <summary>
    /// Una waifu como dato puro: identidad, elemento, rol, carga y skill declarada.
    /// Agregar una waifu = agregar un asset, sin tocar codigo (RF-13).
    /// </summary>
    [CreateAssetMenu(menuName = "WaifuCafe/Waifu", fileName = "Waifu")]
    public sealed class WaifuDefinitionSO : ScriptableObject
    {
        [Header("Identidad")]
        [SerializeField] private string displayName = "Waifu";
        [SerializeField] private WaifuRarity rarity = WaifuRarity.Comun;
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

        public string DisplayName => displayName;
        public Sprite CardArt => cardArt;
        public Sprite ChibiArt => chibiArt;
        public WaifuRarity Rarity => rarity;
        public Color PortraitColor => portraitColor;
        public Dish Element => element;
        public WaifuRole Role => role;
        public int ChargeCost => chargeCost;
        public int CooldownMatches => cooldownMatches;
        public SkillArchetype SkillArchetype => skillArchetype;
        public int Potency => potency;
        public PassiveId PassiveId => passiveId;
    }
}
