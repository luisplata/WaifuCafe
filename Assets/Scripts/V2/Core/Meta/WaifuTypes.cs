namespace WaifuCafe.Core.Meta
{
    /// <summary>Rol de gameplay de una waifu. Los valores enteros son contrato con los assets.</summary>
    public enum WaifuRole { Cocina, Servicio, Apoyo, Control }

    /// <summary>
    /// Arquetipos de skill. <see cref="DelayEviction"/> queda RESERVADO: la expulsion no
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

    /// <summary>Peso de drop. NO es poder: las estrellas actuales viven en el perfil.</summary>
    public enum WaifuRarity { Comun, Rara, Epica }

    /// <summary>Facción jugable del roster V1. Espectáculo queda como preview bloqueada.</summary>
    public enum WaifuFaction { Cafe, Miticos, Espectaculo }
}
