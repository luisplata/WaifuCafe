using System;
using Customers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomerUiComponent : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI customerStatusText;
    [SerializeField] private CustomerStateMachine customerStateMachine;
    [SerializeField] private Image statusSlider;

    private void Start()
    {
        customerStateMachine.OnStateChange += UpdateStatusUi;
        customerStateMachine.OnLoadingState += UpdateStatusSlider;
    }

    private void UpdateStatusSlider(float obj)
    {
        statusSlider.fillAmount = obj;
    }

    private void UpdateStatusUi(CustomerPhase obj)
    {
        customerStatusText.text = $"Customer status: {obj}";
    }

    private void OnDestroy()
    {
        customerStateMachine.OnStateChange -= UpdateStatusUi;
        customerStateMachine.OnLoadingState -= UpdateStatusSlider;
    }
}