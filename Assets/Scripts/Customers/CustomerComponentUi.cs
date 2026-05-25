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

        private float _patienceDuration;
        private bool _isConfigured;
        private bool _isWaiting;

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

            if (nameText != null)
            {
                nameText.text = customer.Type.ToString();
            }

            _patienceDuration = Mathf.Max(0f, customer.Patience);

            if (patienceSlider != null)
            {
                patienceSlider.minValue = 0f;
                patienceSlider.maxValue = _patienceDuration > 0f ? _patienceDuration : 1f;
                patienceSlider.value = Mathf.Clamp(customer.WaitTime, 0f, patienceSlider.maxValue);
                patienceSlider.interactable = false;
            }

            _isConfigured = true;
            _isWaiting = false;
        }

        public void StartWaiting(float patience)
        {
            _patienceDuration = Mathf.Max(0f, patience);
            _isWaiting = true;

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
            if (!_isWaiting && !_isConfigured) return;

            float clamped = Mathf.Clamp(elapsed, 0f, _patienceDuration > 0f ? _patienceDuration : 1f);
            if (patienceSlider != null)
            {
                patienceSlider.value = clamped;
            }

            WaitProgressUpdated?.Invoke(clamped);
        }

        public void FinishWaitingAndClear()
        {
            if (!_isConfigured) return;

            if (patienceSlider != null)
            {
                patienceSlider.value = patienceSlider.maxValue;
            }

            _isWaiting = false;
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
            _isWaiting = false;
            if (patienceSlider != null)
            {
                patienceSlider.value = 0f;
            }
        }
    }
}

