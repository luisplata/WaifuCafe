using System.Collections.Generic;
using NUnit.Framework;
using WaifuCafe.Core.Meta;

namespace WaifuCafe.Tests
{
    /// <summary>
    /// Reglas puras del perfil: basura → defaults, estrellas 1..3, ids desconocidos fuera,
    /// duplicados colapsados y party de 4 poseídas distintas.
    /// </summary>
    public class ProfileSanitizerTests
    {
        private static readonly string[] KnownWaifus = { "airi", "neko", "luna", "rika", "hana", "yuki" };
        private static readonly string[] KnownContracts = { "rush", "noche" };

        [Test]
        public void GarbageProfile_FallsBackToDefaults()
        {
            var profile = new PlayerProfile
            {
                schema = 99,
                starterClaimed = true,
                tutorialStep = (TutorialStep)42,
                coins = 500,
                lastContractId = "rush",
                owned = new List<StarterEntry> { new StarterEntry("rika", 3) },
                party = new List<string> { "rika" },
            };

            bool changed = ProfileSanitizer.Sanitize(profile, KnownWaifus, KnownContracts);

            Assert.IsTrue(changed);
            Assert.AreEqual(PlayerProfile.CurrentSchema, profile.schema);
            Assert.IsFalse(profile.starterClaimed);
            Assert.AreEqual(TutorialStep.Gift, profile.tutorialStep);
            Assert.AreEqual(0, profile.coins);
            Assert.AreEqual(string.Empty, profile.lastContractId);
            Assert.AreEqual(0, profile.owned.Count);
            Assert.AreEqual(0, profile.party.Count);
        }

        [Test]
        public void NullLists_AreRebuilt()
        {
            var profile = new PlayerProfile { owned = null, party = null };

            Assert.IsTrue(ProfileSanitizer.Sanitize(profile, KnownWaifus, KnownContracts));
            Assert.IsNotNull(profile.owned);
            Assert.IsNotNull(profile.party);
        }

        [Test]
        public void Owned_ClampsStars_DropsUnknown_AndCollapsesDuplicates()
        {
            var profile = new PlayerProfile
            {
                owned = new List<StarterEntry>
                {
                    new StarterEntry("rika", 0),
                    new StarterEntry("ri", 3),
                    new StarterEntry("rika", 9),
                },
            };

            Assert.IsTrue(ProfileSanitizer.Sanitize(profile, KnownWaifus, KnownContracts));

            Assert.AreEqual(1, profile.owned.Count, "solo Rika sobrevive");
            Assert.AreEqual("rika", profile.owned[0].waifuId);
            Assert.AreEqual(WaifuStars.Max, profile.owned[0].stars, "el duplicado conserva las estrellas mas altas");
        }

        [Test]
        public void EmptyCatalog_DropsEveryOwnedEntry()
        {
            var profile = new PlayerProfile
            {
                owned = new List<StarterEntry> { new StarterEntry("rika", 3) },
            };

            Assert.IsTrue(ProfileSanitizer.Sanitize(profile, new string[0], KnownContracts));
            Assert.AreEqual(0, profile.owned.Count);
        }

        [Test]
        public void Party_KeepsFourDistinctOwned()
        {
            var profile = new PlayerProfile
            {
                owned = new List<StarterEntry>
                {
                    new StarterEntry("airi", 1),
                    new StarterEntry("neko", 2),
                    new StarterEntry("luna", 2),
                    new StarterEntry("rika", 3),
                    new StarterEntry("hana", 1),
                    new StarterEntry("yuki", 2),
                },
                party = new List<string>
                {
                    "airi", "neko", "luna", "rika", "hana", "yuki",
                    "ghost", "airi", "luna",
                },
            };

            Assert.IsTrue(ProfileSanitizer.Sanitize(profile, KnownWaifus, KnownContracts));

            CollectionAssert.AreEqual(new[] { "airi", "neko", "luna", "rika" }, profile.party);
        }

        [Test]
        public void Party_DropsWaifusThatAreNotOwned()
        {
            var profile = new PlayerProfile
            {
                owned = new List<StarterEntry> { new StarterEntry("rika", 3) },
                party = new List<string> { "rika", "luna" },
            };

            Assert.IsTrue(ProfileSanitizer.Sanitize(profile, KnownWaifus, KnownContracts));
            CollectionAssert.AreEqual(new[] { "rika" }, profile.party);
        }

        [Test]
        public void Contract_KeepsKnown_AndClearsUnknown()
        {
            var profile = new PlayerProfile { lastContractId = "rush" };
            Assert.IsFalse(ProfileSanitizer.Sanitize(profile, KnownWaifus, KnownContracts));
            Assert.AreEqual("rush", profile.lastContractId);

            profile.lastContractId = "contrato-fantasma";
            Assert.IsTrue(ProfileSanitizer.Sanitize(profile, KnownWaifus, KnownContracts));
            Assert.AreEqual(string.Empty, profile.lastContractId);
        }

        [Test]
        public void NegativeCoinsAndInvalidTutorial_AreFixed()
        {
            var profile = new PlayerProfile
            {
                coins = -25,
                tutorialStep = (TutorialStep)(-7),
            };

            Assert.IsTrue(ProfileSanitizer.Sanitize(profile, KnownWaifus, KnownContracts));
            Assert.AreEqual(0, profile.coins);
            Assert.AreEqual(TutorialStep.Gift, profile.tutorialStep);
        }

        [Test]
        public void AlreadyCleanProfile_IsNotModified()
        {
            var profile = new PlayerProfile
            {
                owned = new List<StarterEntry>
                {
                    new StarterEntry("airi", 1),
                    new StarterEntry("rika", 3),
                },
                party = new List<string> { "airi", "rika" },
                lastContractId = "noche",
                coins = 12,
                tutorialStep = TutorialStep.ScriptedRun,
            };

            Assert.IsFalse(ProfileSanitizer.Sanitize(profile, KnownWaifus, KnownContracts));
            Assert.AreEqual("airi", profile.owned[0].waifuId);
            Assert.AreEqual("noche", profile.lastContractId);
        }
    }
}
