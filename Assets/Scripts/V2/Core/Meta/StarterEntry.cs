using System;

namespace WaifuCafe.Core.Meta
{
    /// <summary>
    /// Una waifu poseída por el jugador y sus estrellas actuales. La lista vive completa
    /// en <see cref="PlayerProfile"/>; el perfil es la única fuente de estrellas.
    /// </summary>
    [Serializable]
    public sealed class StarterEntry
    {
        public string waifuId;
        public int stars;

        public StarterEntry()
        {
        }

        public StarterEntry(string waifuId, int stars)
        {
            this.waifuId = waifuId;
            this.stars = stars;
        }
    }
}
