using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Staff
{
    public class StaffComponentUi : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private TextMeshProUGUI nameOfWaifu;
        [SerializeField] private Image background;
        [SerializeField] private Slider serviceTimeSlider;

        [Header("Interaction")]
        [SerializeField] private CanvasGroup canvasGroup;

        public event Action<float> BusyStarted;
        public event Action<float> BusyProgressUpdated;
        public event Action BusyFinished;

        private float _busyDuration;
        private bool _isConfigured;
        private bool _isBusy;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            SetIdleState();
        }

        public void Configure(Staff staff)
        {
            if (staff == null)
            {
                Debug.LogWarning("StaffComponentUi: no se puede configurar porque Staff es null.");
                return;
            }

            _isConfigured = true;
            _isBusy = false;
            SyncFromStaff(staff);
            SetInteractionEnabled(true);
            SetSliderValue(0f);
        }

        public void StartBusy(float duration)
        {
            _busyDuration = Mathf.Max(0f, duration);
            _isBusy = true;

            if (serviceTimeSlider != null)
            {
                serviceTimeSlider.minValue = 0f;
                serviceTimeSlider.maxValue = _busyDuration > 0f ? _busyDuration : 1f;
                serviceTimeSlider.value = 0f;
            }

            SetInteractionEnabled(false);
            BusyStarted?.Invoke(_busyDuration);
        }

        public void UpdateBusyProgress(float elapsedBusyTime)
        {
            if (!_isBusy)
            {
                return;
            }

            float clampedTime = Mathf.Clamp(elapsedBusyTime, 0f, _busyDuration > 0f ? _busyDuration : 1f);
            SetSliderValue(clampedTime);
            BusyProgressUpdated?.Invoke(clampedTime);
        }

        public void SyncFromStaff(Staff staff)
        {
            if (staff == null || !_isConfigured)
            {
                return;
            }

            _busyDuration = Mathf.Max(0f, staff.GetCurrentPhaseDuration());

            if (nameOfWaifu != null)
            {
                string displayName = string.IsNullOrWhiteSpace(staff.name) ? $"Staff {staff.Index}" : staff.name;
                nameOfWaifu.text = $"{displayName} - {staff.GetCurrentPhaseLabel()}";
            }

            float elapsed = Mathf.Clamp(staff.GetCurrentPhaseElapsed(), 0f, _busyDuration > 0f ? _busyDuration : 1f);

            if (serviceTimeSlider != null)
            {
                serviceTimeSlider.minValue = 0f;
                serviceTimeSlider.maxValue = _busyDuration > 0f ? _busyDuration : 1f;
                serviceTimeSlider.value = elapsed;
                serviceTimeSlider.interactable = false;
            }

            // NOTE: Interactability is managed ONLY by MarkBusy/MarkFree, not by phase changes.
            // This ensures drag/drop remains responsive.
        }

        public void FinishBusy()
        {
            if (!_isConfigured)
            {
                return;
            }

            if (serviceTimeSlider != null)
            {
                serviceTimeSlider.value = serviceTimeSlider.maxValue;
            }

            _isBusy = false;
            SetInteractionEnabled(true);
            BusyFinished?.Invoke();
        }

        public bool IsBusy => _isBusy;
        public float BusyDuration => _busyDuration;

        private void SetIdleState()
        {
            _isBusy = false;
            SetInteractionEnabled(true);
            SetSliderValue(0f);
        }

        private void SetInteractionEnabled(bool isInteractable)
        {
            // Note: blocksRaycasts is handled by StaffDragItem independently
            // We only manage interactable for UI feedback, not raycasting
            if (canvasGroup != null)
            {
                canvasGroup.interactable = isInteractable;
                // DO NOT set blocksRaycasts here - it breaks drag/drop handled by StaffDragItem
            }

            if (serviceTimeSlider != null)
            {
                serviceTimeSlider.interactable = false;
            }
        }

        private void SetSliderValue(float value)
        {
            if (serviceTimeSlider != null)
            {
                serviceTimeSlider.value = value;
            }
        }
    }
}