using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Customers
{
    public class CustomerComponentUi : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image background;
        [SerializeField] private Slider patienceSlider;

        [Header("Interaction")]
        [SerializeField] private CanvasGroup canvasGroup;

        public event Action<float> WaitStarted;
        public event Action<float> WaitProgressUpdated;
        public event Action Left;

        private Customer _customer;
        private float _patienceDuration;
        private bool _isConfigured;

        private void Awake()
        {

            SetIdleState();
        }

        public void Configure(Customer customer)
        {
            if (customer == null)
            {
                Debug.LogWarning("CustomerComponentUi: customer null");
                return;
            }

            _customer = customer;

            RefreshFromCustomer(customer, customer.GetCurrentPhaseElapsed());

            _isConfigured = true;
        }

        public void StartWaiting(float patience)
        {
            _patienceDuration = Mathf.Max(0f, patience);

            if (patienceSlider != null)
            {
                patienceSlider.minValue = 0f;
                patienceSlider.maxValue = _patienceDuration > 0f ? _patienceDuration : 1f;
                patienceSlider.value = 0f;
            }

            WaitStarted?.Invoke(_patienceDuration);
        }

        public void UpdateWaitingProgress(float elapsed)
        {
            if (!_isConfigured) return;

            if (_customer != null)
            {
                RefreshFromCustomer(_customer, elapsed);
            }
            else
            {
                RefreshFallback(elapsed);
            }

            WaitProgressUpdated?.Invoke(elapsed);
        }

        public void FinishWaitingAndClear()
        {
            if (!_isConfigured) return;

            if (patienceSlider != null)
            {
                patienceSlider.value = patienceSlider.maxValue;
            }

        }

        public void MarkLeaving()
        {
            // Visual cue: tint background red/orange to indicate leaving
            if (background != null)
            {
                background.color = Color.Lerp(background.color, Color.red, 0.7f);
            }

            Left?.Invoke();
        }

        private void SetIdleState()
        {
            if (patienceSlider != null)
            {
                patienceSlider.value = 0f;
            }
        }

        private void RefreshFromCustomer(Customer customer, float elapsed)
        {
            float duration = Mathf.Max(0f, customer.GetCurrentPhaseDuration());
            float clampedDuration = duration > 0f ? duration : 1f;
            float clampedElapsed = Mathf.Clamp(elapsed, 0f, clampedDuration);

            _patienceDuration = clampedDuration;

            if (nameText != null)
            {
                nameText.text = $"{customer.Type} - {customer.GetCurrentPhaseLabel()}";
            }

            if (patienceSlider != null)
            {
                patienceSlider.minValue = 0f;
                patienceSlider.maxValue = clampedDuration;
                patienceSlider.value = clampedElapsed;
                patienceSlider.interactable = false;
            }
        }

        private void RefreshFallback(float elapsed)
        {
            float clamped = Mathf.Clamp(elapsed, 0f, _patienceDuration > 0f ? _patienceDuration : 1f);

            if (patienceSlider != null)
            {
                patienceSlider.value = clamped;
            }
        }
    }
}

