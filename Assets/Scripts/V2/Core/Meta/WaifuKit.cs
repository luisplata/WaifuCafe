using System;
using System.Collections.Generic;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Kit de una waifu resuelto para reglas puras: facción, rol, elemento, activa, pasiva
    /// y estrellas actuales (perfil). Las definiciones en runtime salen de
    /// <c>WaifuDefinitionSO</c>; esta forma es la que consume <see cref="SynergyModel"/> y
    /// la que usan los tests para no depender de assets.
    /// </summary>
    public sealed class WaifuKit
    {
        public string Id = string.Empty;
        public WaifuFaction Faction = WaifuFaction.Cafe;
        public WaifuRole Role = WaifuRole.Servicio;
        public Dish Element = Dish.Almuerzo;
        public SkillArchetype Active = SkillArchetype.InstantServeUnits;
        public PassiveId Passive = PassiveId.None;
        public int Stars = 1;
        public int ChargeCost = 10;
        public int CooldownMatches;
        public int BasePotency = 4;

        /// <summary>Copia con otras estrellas (el perfil manda sobre <c>baseStars</c>).</summary>
        public WaifuKit WithStars(int stars) => new WaifuKit
        {
            Id = Id,
            Faction = Faction,
            Role = Role,
            Element = Element,
            Active = Active,
            Passive = Passive,
            Stars = stars,
            ChargeCost = ChargeCost,
            CooldownMatches = CooldownMatches,
            BasePotency = BasePotency,
        };
    }

    /// <summary>
    /// Matriz de los 10 kits del diseño (D1). Es el contrato legible/testeable; en runtime
    /// el asset de cada waifu es la fuente de verdad y este catálogo sólo aporta valores
    /// por defecto (tests y herramientas).
    /// </summary>
    public static class WaifuKits
    {
        public const string Airi = "airi";
        public const string Neko = "neko";
        public const string Yuki = "yuki";
        public const string Hana = "hana";
        public const string Luna = "luna";
        public const string Rika = "rika";
        public const string Alice = "alice";
        public const string Emi = "emi";
        public const string Sora = "sora";
        public const string Miko = "miko";

        private static readonly WaifuKit[] Table =
        {
            new WaifuKit
            {
                Id = Airi, Faction = WaifuFaction.Cafe, Role = WaifuRole.Apoyo, Element = Dish.Almuerzo,
                Active = SkillArchetype.InstantServeUnits, Passive = PassiveId.CoinBonus,
                ChargeCost = 10, CooldownMatches = 0, BasePotency = 4,
            },
            new WaifuKit
            {
                Id = Neko, Faction = WaifuFaction.Cafe, Role = WaifuRole.Servicio, Element = Dish.Snack,
                Active = SkillArchetype.InstantServeUnits, Passive = PassiveId.ExtraCharge,
                ChargeCost = 10, CooldownMatches = 0, BasePotency = 4,
            },
            new WaifuKit
            {
                Id = Yuki, Faction = WaifuFaction.Cafe, Role = WaifuRole.Control, Element = Dish.Postre,
                Active = SkillArchetype.ReshuffleBoard, Passive = PassiveId.FasterService,
                ChargeCost = 12, CooldownMatches = 2, BasePotency = 12,
            },
            new WaifuKit
            {
                Id = Hana, Faction = WaifuFaction.Miticos, Role = WaifuRole.Cocina, Element = Dish.Bebida,
                Active = SkillArchetype.ConvertToElement, Passive = PassiveId.ExtraCharge,
                ChargeCost = 12, CooldownMatches = 2, BasePotency = 3,
            },
            new WaifuKit
            {
                Id = Luna, Faction = WaifuFaction.Miticos, Role = WaifuRole.Servicio, Element = Dish.Almuerzo,
                Active = SkillArchetype.InstantServeUnits, Passive = PassiveId.FasterService,
                ChargeCost = 10, CooldownMatches = 2, BasePotency = 4,
            },
            new WaifuKit
            {
                Id = Rika, Faction = WaifuFaction.Miticos, Role = WaifuRole.Control, Element = Dish.Bebida,
                Active = SkillArchetype.ClearCross, Passive = PassiveId.ExtraCharge,
                ChargeCost = 14, CooldownMatches = 2, BasePotency = 13,
            },
            new WaifuKit
            {
                Id = Alice, Faction = WaifuFaction.Espectaculo, Role = WaifuRole.Apoyo, Element = Dish.Postre,
                Active = SkillArchetype.ClearCross, Passive = PassiveId.CoinBonus,
                ChargeCost = 14, CooldownMatches = 2, BasePotency = 10,
            },
            new WaifuKit
            {
                Id = Emi, Faction = WaifuFaction.Espectaculo, Role = WaifuRole.Servicio, Element = Dish.Snack,
                Active = SkillArchetype.InstantServeUnits, Passive = PassiveId.FasterService,
                ChargeCost = 10, CooldownMatches = 0, BasePotency = 4,
            },
            new WaifuKit
            {
                Id = Sora, Faction = WaifuFaction.Espectaculo, Role = WaifuRole.Apoyo, Element = Dish.Snack,
                Active = SkillArchetype.ReshuffleBoard, Passive = PassiveId.ExtraCharge,
                ChargeCost = 12, CooldownMatches = 2, BasePotency = 4,
            },
            new WaifuKit
            {
                Id = Miko, Faction = WaifuFaction.Miticos, Role = WaifuRole.Cocina, Element = Dish.Desayuno,
                Active = SkillArchetype.ConvertToElement, Passive = PassiveId.CoinBonus,
                ChargeCost = 12, CooldownMatches = 0, BasePotency = 6,
            },
        };

        /// <summary>Los 10 kits del diseño, en orden estable.</summary>
        public static IReadOnlyList<WaifuKit> All => Table;

        /// <summary>Kit del diseño por id (case-insensitive) con las estrellas indicadas.</summary>
        public static WaifuKit For(string id, int stars = 1)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            foreach (WaifuKit kit in Table)
                if (string.Equals(kit.Id, id, StringComparison.OrdinalIgnoreCase))
                    return kit.WithStars(WaifuStars.Clamp(stars));

            return null;
        }

        /// <summary>Copia nueva por búsqueda; devuelve null si el id no existe.</summary>
        public static WaifuKit Find(string id) => For(id, 1);
    }
}
