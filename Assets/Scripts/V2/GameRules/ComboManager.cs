using System;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using V2.Audio;
using V2.Customer;
using V2.Food;

public class ComboManager : MonoBehaviour, IComboManager
{
    [SerializeField] private CustomerComboManager customerComboManager;
    [SerializeField] private FoodComboManager foodComboManager;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI comboFood;
    [SerializeField] private TextMeshProUGUI ComboCustomer;
    [SerializeField] private AudioClip[] comboClips;
    [SerializeField] private AudioClip superCombo;
    [SerializeField] private Sprite start;

    [SerializeField] private Image[] startsFood;
    [SerializeField] private Image[] startsCustomers;

    [SerializeField] private VfxMatch vfxMatch;

    private readonly Dictionary<Image, Tween> _pulseTweens = new();

    public Action<int> onComboFinished;
    private IGameRules _gameRules;
    private List<ICustomComboManager> comboManagers = new();

    private bool _foodMatch, _customerMatch;
    private ComboData _comboData;

    public void RegisterServed(FoodModel food, CustomerClientModel customer)
    {
        var comboInput = new ComboInput
        {
            food = food,
            customer = customer
        };
        _foodMatch = false;
        _customerMatch = false;

        foreach (var customComboManager in comboManagers)
        {
            var comboData = customComboManager.RegisterServed(comboInput);

            UpdateUi(comboData);

            if (comboData.matched)
            {
                if (comboData.comboType is ComboType.Vip or ComboType.Casual or ComboType.Rush)
                    _customerMatch = true;

                if (comboData.comboType is ComboType.Breakfast or ComboType.Lunch or ComboType.Drink)
                    _foodMatch = true;
            }

            _comboData = comboData;
        }

        var playOtherSounds = true;

        //Validamos si hay doble match
        if (_foodMatch && _customerMatch)
        {
            playOtherSounds = false;
            AudioService.Instance.StartSfx(superCombo);
            vfxMatch.PlayDoubleMatchVfx();
        }

        if ((_foodMatch || _customerMatch) && playOtherSounds)
        {
            AudioService.Instance.StartSfx(comboClips[2]);
            vfxMatch.PlayMatchVfx(_comboData.comboType);
        }
        else
        {
            AudioService.Instance.StartSfx(
                comboClips[_comboData.comboSize - 1]);
        }

        _foodMatch = false;
        _customerMatch = false;
    }

    private void PulseStar(Image star)
    {
        if (_pulseTweens.ContainsKey(star))
            return;

        _pulseTweens[star] = Tween.Scale(
            star.rectTransform,
            Vector3.one * 1.15f,
            0.5f,
            cycles: -1,
            cycleMode: CycleMode.Yoyo
        );
    }

    private void StopPulse(Image star)
    {
        if (!_pulseTweens.TryGetValue(star, out var tween))
            return;

        tween.Stop();

        star.rectTransform.localScale = Vector3.one;

        _pulseTweens.Remove(star);
    }

    private void UpdateUi(ComboData comboData)
    {
        switch (comboData.comboType)
        {
            case ComboType.Vip or ComboType.Casual or ComboType.Rush:

                foreach (var image in startsCustomers)
                {
                    image.sprite = null;
                    StopPulse(image);
                }

                for (int i = 0; i < comboData.comboSize; i++)
                {
                    startsCustomers[i].sprite = start;
                }

                ComboCustomer.text = $"{comboData.comboType}";

                if (comboData.comboSize == 2)
                {
                    PulseStar(startsCustomers[2]);
                }

                break;
            case ComboType.Breakfast or ComboType.Drink or ComboType.Lunch:

                foreach (var image in startsFood)
                {
                    image.sprite = null;
                    StopPulse(image);
                }

                for (int i = 0; i < comboData.comboSize; i++)
                {
                    startsFood[i].sprite = start;
                }

                comboFood.text = $"{comboData.comboType}";

                if (comboData.comboSize == 2)
                {
                    PulseStar(startsFood[2]);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnMatch(ComboData comboData)
    {
        Debug.Log($"Combo Matched! {comboData.comboSize} de {comboData.comboType}");
    }

    private int CalculateReward(int comboSize)
    {
        return foodComboManager.GetReward(comboSize) + customerComboManager.GetReward(comboSize);
    }

    public void Configure(IGameRules gameRules)
    {
        _gameRules = gameRules;
        customerComboManager.Configure(this);
        foodComboManager.Configure(this);

        comboManagers.Add(customerComboManager);
        comboManagers.Add(foodComboManager);

        foreach (var comboManager in comboManagers)
        {
            comboManager.OnMatch += OnMatch;
        }

        // pointsText.text = $"Points: {0}";
        comboFood.text = $"Combo: x{0}";
        ComboCustomer.text = $"Acumulado: {0}";
    }
}