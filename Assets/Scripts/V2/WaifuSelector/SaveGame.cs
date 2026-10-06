using System.Collections.Generic;
using UnityEngine;

public class SaveGame : MonoBehaviour
{
    public static SaveGame Instance { get; private set; }

    [SerializeField] private List<StaffNames> waifusSelected = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    public void SaveWaifusSelected(CardOfWaifu[] cardSelected)
    {
        waifusSelected.Clear();
        foreach (var card in cardSelected)
        {
            if (card != null)
            {
                waifusSelected.Add(card.StaffEspeciality);
            }
        }
    }

    public List<StaffNames> GetWaifusSelected()
    {
        return waifusSelected;
    }

    public void EnsureDefaults(List<StaffNames> defaults)
    {
        if (waifusSelected.Count == 0 && defaults != null && defaults.Count > 0)
        {
            waifusSelected.AddRange(defaults);
        }
    }
}