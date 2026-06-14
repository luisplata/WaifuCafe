using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardOfWaifu : MonoBehaviour
{
    public Action<CardOfWaifu> OnCardSelected;
    public Action<CardOfWaifu> OnCardDeselected;
    private bool isSelected;
    [SerializeField] private Button cardButton;
    [SerializeField] private Image contorno;
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private Sprite cardSprite;
    [SerializeField] private Sprite splash;
    [SerializeField] private string waifuName;
    [SerializeField] private string waifuDescription;
    [SerializeField] private StaffNames staffEspeciality;
    public Sprite SplashArt => splash;
    public string WaifuName => waifuName;
    public string WaifuDescription => waifuDescription;
    public StaffNames StaffEspeciality => staffEspeciality;

    private void Start()
    {
        cardImage.sprite = cardSprite;
        cardName.text = waifuName;
        cardButton.onClick.AddListener(() =>
        {
            isSelected = !isSelected;
            if (isSelected)
            {
                contorno.color = Color.green;
                OnCardSelected?.Invoke(this);
            }
            else
            {
                contorno.color = Color.clear;
                OnCardDeselected?.Invoke(this);
            }
        });
    }

    public void SetCardDeselected()
    {
        isSelected = false;
        contorno.color = Color.clear;
    }
}