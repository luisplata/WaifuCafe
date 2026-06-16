using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaifuSelectorRules : MonoBehaviour
{
    [SerializeField] private List<CardOfWaifu> cards;
    [SerializeField] private Transform parentoToCard;
    [SerializeField] [Range(1, 4)] private int maxCardSelected;
    [SerializeField] private Image cardArt;
    [SerializeField] private Image splashArt;
    [SerializeField] private TextMeshProUGUI sinopsis;
    [SerializeField] private TextMeshProUGUI waifuname;
    [SerializeField] private Button continuar;
    [SerializeField] private Sprite bg, cardbg;

    [SerializeField] private CardOfWaifu[] cardSelected;

    private List<CardOfWaifu> _cardsInstantiated = new();

    private void Awake()
    {
        cardSelected = new CardOfWaifu[maxCardSelected];
    }

    private void Start()
    {
        foreach (var cardInstantiate in cards.Select(card => Instantiate(card, parentoToCard)))
        {
            cardInstantiate.OnCardSelected += OnCardSelected;
            cardInstantiate.OnCardDeselected += OnCardDeselected;
            _cardsInstantiated.Add(cardInstantiate);
        }

        continuar.onClick.AddListener(() =>
        {
            SaveGame.Instance.SaveWaifusSelected(cardSelected);
            SceneManager.LoadScene(1);
        });
        sinopsis.text = string.Empty;
        waifuname.text = string.Empty;
        splashArt.sprite = bg;
        cardArt.sprite = cardbg;
    }

    private void OnCardDeselected(CardOfWaifu obj)
    {
        for (int index = 0; index < cardSelected.Length; index++)
        {
            var card = cardSelected[index];
            if (card == obj)
            {
                cardSelected[index] = null;

                splashArt.sprite = bg;
                cardArt.sprite = cardbg;
                break;
            }
        }
    }

    private void OnCardSelected(CardOfWaifu obj)
    {
        for (var index = 0; index < cardSelected.Length; index++)
        {
            var cardOfWaifu = cardSelected[index];
            if (cardOfWaifu == null)
            {
                cardSelected[index] = obj;
                cardArt.sprite = obj.CardArtArt;
                sinopsis.text = obj.WaifuDescription;
                waifuname.text = obj.WaifuName;
                splashArt.sprite = obj.SplashArt;
                return;
            }
        }

        obj.SetCardDeselected();

        continuar.gameObject.SetActive(cardSelected.All(card => card != null));
    }
}