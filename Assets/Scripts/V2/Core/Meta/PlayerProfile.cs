using System;
using System.Collections.Generic;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Perfil persistente del jugador. JSON plano en PlayerPrefs (<c>waifu_profile_v1</c>):
    /// una lista de poseídas con sus estrellas, la party de 4 y el progreso del tutorial.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public const int CurrentSchema = 1;

        public int schema = CurrentSchema;
        public bool starterClaimed;
        public TutorialStep tutorialStep = TutorialStep.Gift;
        public int coins;
        public string lastContractId = string.Empty;
        public List<StarterEntry> owned = new List<StarterEntry>();
        public List<string> party = new List<string>();
    }
}
