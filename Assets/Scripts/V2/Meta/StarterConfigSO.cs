using System;
using System.Collections.Generic;
using UnityEngine;
using V2.Waifu;

namespace V2.Meta
{
    /// <summary>
    /// Una waifu del regalo o de la recompensa del tutorial, con las estrellas curadas.
    /// No hay azar: el asset declara exactamente quién entra y con cuántas estrellas.
    /// </summary>
    [Serializable]
    public sealed class StarterEntryConfig
    {
        [SerializeField] private WaifuDefinitionSO waifu;
        [SerializeField, Range(1, 3)] private int stars = 1;

        public StarterEntryConfig()
        {
        }

        public StarterEntryConfig(WaifuDefinitionSO waifu, int stars)
        {
            this.waifu = waifu;
            this.stars = Mathf.Clamp(stars, 1, 3);
        }

        public WaifuDefinitionSO Waifu => waifu;
        public string Id => waifu != null ? waifu.Id : string.Empty;
        public int Stars => Mathf.Clamp(stars, 1, 3);
        public Sprite Art => waifu == null ? null : (waifu.ChibiArt != null ? waifu.ChibiArt : waifu.CardArt);
        public string DisplayName => waifu != null ? waifu.DisplayName : "Waifu";
    }

    /// <summary>
    /// Contenido curado del tutorial: cuatro starters en el regalo (Airi 1★, Neko 2★,
    /// Luna 2★, Rika 3★) y dos recompensas (Hana 1★, Yuki 2★). Cero aleatoriedad.
    /// </summary>
    [CreateAssetMenu(menuName = "WaifuCafe/Starter Config", fileName = "StarterConfig")]
    public sealed class StarterConfigSO : ScriptableObject
    {
        [Header("Regalo inicial (4 starters)")]
        [SerializeField] private StarterEntryConfig[] gift = Array.Empty<StarterEntryConfig>();

        [Header("Recompensa al ganar el tutorial (2 waifus)")]
        [SerializeField] private StarterEntryConfig[] reward = Array.Empty<StarterEntryConfig>();

        public IReadOnlyList<StarterEntryConfig> Gift => gift ?? Array.Empty<StarterEntryConfig>();
        public IReadOnlyList<StarterEntryConfig> Reward => reward ?? Array.Empty<StarterEntryConfig>();
    }
}
