using NUnit.Framework;
using WaifuCafe.Core;
using WaifuCafe.Core.Meta;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// Reglas puras de sinergia: breakpoints de facción/rol, gate de ultimate T1/T2,
    /// fórmulas de carga y la matriz de los 10 kits del diseño.
    /// </summary>
    public sealed class SynergyModelTests
    {
        private static WaifuKit[] Party(params (string Id, int Stars)[] entries)
        {
            var kits = new WaifuKit[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                kits[i] = WaifuKits.For(entries[i].Id, entries[i].Stars);
                Assert.IsNotNull(kits[i], "Kit desconocido: " + entries[i].Id);
            }

            return kits;
        }

        // ---- Gate de ultimate ----------------------------------------------------

        [Test]
        public void ThreeMiticosWithThreeStar_UnlocksLunaDeSangre()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("rika", 3), ("luna", 2), ("hana", 1), ("yuki", 2)));

            Assert.IsTrue(mods.Ultimate.IsReady);
            Assert.AreEqual(UltimateId.LunaDeSangre, mods.Ultimate.Id);
            Assert.IsTrue(mods.Ultimate.Live, "Luna de Sangre es la única ultimate viva en V1.");
            Assert.AreEqual(1, mods.Ultimate.Tier);
            Assert.AreEqual(WaifuFaction.Miticos, mods.Ultimate.Faction);
            Assert.AreEqual(0, mods.Ultimate.LeaderSlot, "Rika (3★) es la caster del slot 0.");
            Assert.AreEqual(3, mods.Ultimate.Members);
        }

        [Test]
        public void ThreeMiticosWithoutCurrentThreeStar_NoUltimate()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("rika", 2), ("luna", 2), ("hana", 2), ("yuki", 3)));

            Assert.IsFalse(mods.Ultimate.IsReady, "La ★3 tiene que ser de la facción que gatea.");
            Assert.AreEqual(2, mods.ChargeCostFlat, "Míticos 3 sigue dando −2 aunque falle la ultimate.");
            StringAssert.Contains("falta una ★3", SynergyModel.BadgeText(mods.Ultimate, mods.Party));
        }

        [Test]
        public void TwoPlusTwo_NoUltimate()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("rika", 3), ("luna", 2), ("airi", 1), ("neko", 1)));

            Assert.IsFalse(mods.Ultimate.IsReady);
            Assert.AreEqual(UltimateId.None, mods.Ultimate.Id);
            // Los breakpoints sí se aplican (FT2), sólo falta la ultimate.
            Assert.AreEqual(1, mods.ChargeCostFlat, "Míticos 2 → −1 carga.");
            Assert.AreEqual(1, mods.OrderCoinBonus, "Café 2 → +1 moneda/pedido.");
            StringAssert.Contains("2+2", SynergyModel.BadgeText(mods.Ultimate, mods.Party));
        }

        [Test]
        public void FourMiticosWithEightStars_UnlocksZorroDeFuegoT2()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("rika", 3), ("hana", 2), ("luna", 2), ("miko", 1)));

            Assert.IsTrue(mods.Ultimate.IsReady);
            Assert.AreEqual(UltimateId.ZorroDeFuego, mods.Ultimate.Id);
            Assert.AreEqual(2, mods.Ultimate.Tier);
            Assert.IsFalse(mods.Ultimate.Live, "T2 es contenido de persecución/debug (FU2).");
            Assert.AreEqual(2, mods.StartCharge, "Zorro T2 suma +2 carga inicial.");
        }

        [Test]
        public void FourMiticosBelowEightStars_FallsBackToLuna()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("rika", 3), ("hana", 1), ("luna", 2), ("miko", 1)));

            Assert.AreEqual(UltimateId.LunaDeSangre, mods.Ultimate.Id);
            Assert.AreEqual(1, mods.Ultimate.Tier);
            Assert.IsTrue(mods.Ultimate.Live);
        }

        [Test]
        public void UltimateDisabledByContract_NoGate()
        {
            ContractModifiers contract = ContractModifiers.Neutral();
            contract.ultimateEnabled = false;

            PartyModifiers mods = SynergyModel.Build(Party(
                ("rika", 3), ("luna", 2), ("hana", 1), ("yuki", 2)), contract);

            Assert.IsFalse(mods.UltimateEnabled);
            Assert.IsFalse(mods.Ultimate.IsReady);
        }

        // ---- Breakpoints de facción ---------------------------------------------

        [Test]
        public void CafeThree_GrantsCoinsAndCheaperCharge()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("airi", 1), ("neko", 2), ("yuki", 2), ("luna", 1)));

            Assert.AreEqual(2, mods.OrderCoinBonus);
            Assert.AreEqual(0.9f, mods.ChargeCostMultiplier, 0.0001f);
            Assert.AreEqual(9, mods.ChargeCost(10));
            Assert.AreEqual(13, mods.ChargeCost(14), "12.6 redondea lejos del cero.");
        }

        [Test]
        public void CafeTwo_GrantsOneCoin()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("airi", 1), ("neko", 1), ("rika", 1), ("luna", 1)));

            Assert.AreEqual(1, mods.OrderCoinBonus);
            Assert.AreEqual(1f, mods.ChargeCostMultiplier, 0.0001f);
        }

        [Test]
        public void MiticosThree_ChargeFlatAndPotency()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("rika", 3), ("luna", 2), ("hana", 1), ("alice", 1)));

            Assert.AreEqual(2, mods.ChargeCostFlat, "Míticos 3 reemplaza el −1 del tier 2.");
            Assert.AreEqual(1, mods.PotencyBonus);
            Assert.AreEqual(12, mods.ChargeCost(14));
            Assert.IsFalse(mods.StartChargeHalf);
        }

        [Test]
        public void MiticosFour_HalfStartCharge()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("rika", 1), ("luna", 1), ("hana", 1), ("miko", 1)));

            Assert.IsTrue(mods.StartChargeHalf);
            Assert.AreEqual(7, mods.StartChargeFor(14), "Mitad de 14, sin Apoyo ni Zorro.");
        }

        [Test]
        public void EspectaculoTwo_PatienceOnly()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("alice", 1), ("emi", 1), ("rika", 1), ("luna", 1)));

            Assert.AreEqual(1.15f, mods.PatienceMultiplier, 0.0001f);
            Assert.AreEqual(0, mods.CascadeCoinBonus);
        }

        [Test]
        public void EspectaculoThree_PatienceAndCascadeCoins()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("alice", 1), ("emi", 1), ("sora", 1), ("rika", 1)));

            Assert.AreEqual(1.30f, mods.PatienceMultiplier, 0.0001f);
            Assert.AreEqual(1, mods.CascadeCoinBonus);
        }

        // ---- Breakpoints de rol --------------------------------------------------

        [Test]
        public void CocinaTwo_AddsPotency()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("hana", 1), ("miko", 1), ("rika", 3), ("yuki", 2)));

            // Míticos 3 (+1) + Cocina 2 (+1).
            Assert.AreEqual(2, mods.PotencyBonus);
        }

        [Test]
        public void ServicioTwoAndThree_ChargeGain()
        {
            PartyModifiers two = SynergyModel.Build(Party(
                ("luna", 1), ("neko", 1), ("alice", 1), ("rika", 1)));
            Assert.AreEqual(1.10f, two.ChargeGainMultiplier, 0.0001f);
            Assert.AreEqual(11, two.ChargeGain(10));

            PartyModifiers three = SynergyModel.Build(Party(
                ("luna", 1), ("neko", 1), ("emi", 1), ("rika", 1)));
            Assert.AreEqual(1.20f, three.ChargeGainMultiplier, 0.0001f);
            Assert.AreEqual(12, three.ChargeGain(10));
        }

        [Test]
        public void ApoyoTwoAndThree_StartCharge()
        {
            PartyModifiers two = SynergyModel.Build(Party(
                ("airi", 1), ("alice", 1), ("rika", 1), ("luna", 1)));
            Assert.AreEqual(1, two.StartCharge);

            PartyModifiers three = SynergyModel.Build(Party(
                ("airi", 1), ("alice", 1), ("sora", 1), ("rika", 1)));
            Assert.AreEqual(2, three.StartCharge);
        }

        [Test]
        public void ControlTwo_CooldownReduction()
        {
            PartyModifiers mods = SynergyModel.Build(Party(
                ("yuki", 1), ("rika", 1), ("luna", 1), ("hana", 1)));

            Assert.AreEqual(1, mods.ControlCooldownReduction);
        }

        // ---- Fórmulas ------------------------------------------------------------

        [Test]
        public void ChargeCost_ClampsAtFour()
        {
            var neutral = PartyModifiers.Neutral();
            Assert.AreEqual(4, neutral.ChargeCost(3));

            PartyModifiers mods = SynergyModel.Build(Party(
                ("rika", 1), ("luna", 1), ("hana", 1), ("yuki", 1)));
            Assert.AreEqual(4, mods.ChargeCost(5), "5 − 2 = 3, el piso es 4.");
        }

        [Test]
        public void ContractModifiers_FoldChargeCascadeAndUltimate()
        {
            var contract = new ContractModifiers
            {
                chargeCostMultiplier = 0.8f,
                coinsPerCascade = 1,
            };

            PartyModifiers mods = SynergyModel.Build(Party(
                ("rika", 3), ("luna", 2), ("hana", 1), ("miko", 1)), contract);

            Assert.AreEqual(10, mods.ChargeCost(14), "(14 − 2) × 0.8 = 9.6 → 10.");
            Assert.AreEqual(1, mods.CascadeCoinBonus);
            Assert.IsTrue(mods.Ultimate.IsReady);
        }

        [Test]
        public void NeutralParty_HasNoBonuses()
        {
            PartyModifiers mods = SynergyModel.Build(null);

            Assert.AreEqual(0, mods.PotencyBonus);
            Assert.AreEqual(0, mods.ChargeCostFlat);
            Assert.AreEqual(1f, mods.ChargeCostMultiplier, 0.0001f);
            Assert.AreEqual(1f, mods.ChargeGainMultiplier, 0.0001f);
            Assert.AreEqual(0, mods.StartCharge);
            Assert.AreEqual(0, mods.OrderCoinBonus);
            Assert.AreEqual(0, mods.CascadeCoinBonus);
            Assert.AreEqual(1f, mods.PatienceMultiplier, 0.0001f);
            Assert.AreEqual(0, mods.ControlCooldownReduction);
            Assert.IsFalse(mods.Ultimate.IsReady);
            Assert.AreEqual(10, mods.ChargeCost(10));
        }

        [Test]
        public void Summary_ListsRepeatedFactionsAndRoles()
        {
            string summary = SynergyModel.Summary(Party(
                ("rika", 1), ("luna", 1), ("hana", 1), ("miko", 1)));

            StringAssert.Contains("Míticos 4", summary);
            StringAssert.Contains("Cocina 2", summary);
        }

        // ---- Matriz de kits (D1) -------------------------------------------------

        [Test]
        public void KitMatrix_MatchesDesign()
        {
            var expected = new (string Id, WaifuFaction Faction, WaifuRole Role, Dish Element,
                SkillArchetype Active, PassiveId Passive)[]
            {
                ("airi", WaifuFaction.Cafe, WaifuRole.Apoyo, Dish.Almuerzo,
                    SkillArchetype.InstantServeUnits, PassiveId.CoinBonus),
                ("neko", WaifuFaction.Cafe, WaifuRole.Servicio, Dish.Snack,
                    SkillArchetype.InstantServeUnits, PassiveId.ExtraCharge),
                ("yuki", WaifuFaction.Cafe, WaifuRole.Control, Dish.Postre,
                    SkillArchetype.ReshuffleBoard, PassiveId.FasterService),
                ("hana", WaifuFaction.Miticos, WaifuRole.Cocina, Dish.Bebida,
                    SkillArchetype.ConvertToElement, PassiveId.ExtraCharge),
                ("luna", WaifuFaction.Miticos, WaifuRole.Servicio, Dish.Almuerzo,
                    SkillArchetype.InstantServeUnits, PassiveId.FasterService),
                ("rika", WaifuFaction.Miticos, WaifuRole.Control, Dish.Bebida,
                    SkillArchetype.ClearCross, PassiveId.ExtraCharge),
                ("alice", WaifuFaction.Espectaculo, WaifuRole.Apoyo, Dish.Postre,
                    SkillArchetype.ClearCross, PassiveId.CoinBonus),
                ("emi", WaifuFaction.Espectaculo, WaifuRole.Servicio, Dish.Snack,
                    SkillArchetype.InstantServeUnits, PassiveId.FasterService),
                ("sora", WaifuFaction.Espectaculo, WaifuRole.Apoyo, Dish.Snack,
                    SkillArchetype.ReshuffleBoard, PassiveId.ExtraCharge),
                ("miko", WaifuFaction.Miticos, WaifuRole.Cocina, Dish.Desayuno,
                    SkillArchetype.ConvertToElement, PassiveId.CoinBonus),
            };

            Assert.AreEqual(expected.Length, WaifuKits.All.Count);
            foreach ((string id, WaifuFaction faction, WaifuRole role, Dish element,
                         SkillArchetype active, PassiveId passive) in expected)
            {
                WaifuKit kit = WaifuKits.For(id);
                Assert.IsNotNull(kit, id);
                Assert.AreEqual(faction, kit.Faction, id + " facción");
                Assert.AreEqual(role, kit.Role, id + " rol");
                Assert.AreEqual(element, kit.Element, id + " elemento");
                Assert.AreEqual(active, kit.Active, id + " activa");
                Assert.AreEqual(passive, kit.Passive, id + " pasiva");
            }
        }

        [Test]
        public void WaifuStars_LevelsAndPotency()
        {
            Assert.AreEqual(1, WaifuStars.LevelFromUnits(0));
            Assert.AreEqual(1, WaifuStars.LevelFromUnits(5));
            Assert.AreEqual(2, WaifuStars.LevelFromUnits(6));
            Assert.AreEqual(2, WaifuStars.LevelFromUnits(11));
            Assert.AreEqual(3, WaifuStars.LevelFromUnits(12));

            // base 4 + 2(3−1) + 2(3−1).
            Assert.AreEqual(12, WaifuStars.Potency(4, 3, 3));
        }
    }
}
