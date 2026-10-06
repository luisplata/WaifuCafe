using System.Collections.Generic;
using UnityEngine;
using V2.Audio;
using V2.Helpers;

/// <summary>
/// Boot del prototipo Match-3 para playtest público en una sola escena.
/// Garantiza: free-play endless (sin tutorial), singletons mínimos y staff fijo
/// sin pasar por Intro ni WaifuSelector. No toca el juego principal.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PrototypeBoot : MonoBehaviour
{
    [SerializeField] private List<StaffNames> defaultStaff = new() { StaffNames.Rika, StaffNames.Rika, StaffNames.Rika };
    [SerializeField] private bool forceFreePlay = true;

    private void Awake()
    {
        if (forceFreePlay && TutorialProgress.CurrentDay < 4)
        {
            TutorialProgress.CurrentDay = 4;
        }

        if (SaveGame.Instance == null)
        {
            var go = new GameObject("SaveGame (Prototype)");
            go.AddComponent<SaveGame>();
        }
        SaveGame.Instance.EnsureDefaults(defaultStaff);

        if (SaveManager.Instance == null)
        {
            var go = new GameObject("SaveManager (Prototype)");
            go.AddComponent<SaveManager>();
        }

        if (AudioService.Instance == null)
        {
            var go = new GameObject("AudioService (Prototype)");
            go.AddComponent<AudioService>();
        }
    }
}
