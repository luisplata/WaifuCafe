using System;
using System.Collections.Generic;
using System.Text;
using Customers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Customers.QueueUi
{
    public class QueueUiView : MonoBehaviour, IQueueUiView
    {
        [Header("Buttons")]
        [SerializeField] private Button serveButton;
        [SerializeField] private Button peekButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button resetButton;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI queueCountText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI spawningStateText;

        public event Action ServeRequested;
        public event Action PeekRequested;
        public event Action PauseRequested;
        public event Action ResumeRequested;
        public event Action StatsRequested;
        public event Action ResetRequested;

        private void Awake()
        {
            RegisterButton(serveButton, RaiseServeRequested);
            RegisterButton(peekButton, RaisePeekRequested);
            RegisterButton(pauseButton, RaisePauseRequested);
            RegisterButton(resumeButton, RaiseResumeRequested);
            RegisterButton(statsButton, RaiseStatsRequested);
            RegisterButton(resetButton, RaiseResetRequested);
        }

        private void OnDestroy()
        {
            UnregisterButton(serveButton, RaiseServeRequested);
            UnregisterButton(peekButton, RaisePeekRequested);
            UnregisterButton(pauseButton, RaisePauseRequested);
            UnregisterButton(resumeButton, RaiseResumeRequested);
            UnregisterButton(statsButton, RaiseStatsRequested);
            UnregisterButton(resetButton, RaiseResetRequested);
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        public void SetSpawningPaused(bool isPaused)
        {
            if (spawningStateText != null)
            {
                spawningStateText.text = isPaused ? "Spawning: Paused" : "Spawning: Running";
            }
        }

        public void SetQueueEntries(IReadOnlyList<Customer> customers)
        {
            if (queueCountText == null)
            {
                return;
            }

            if (customers == null || customers.Count == 0)
            {
                queueCountText.text = "Queue: empty";
                return;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < customers.Count; i++)
            {
                Customer customer = customers[i];
                if (customer == null)
                {
                    continue;
                }

                int position = i + 1;
                builder.Append($"[{customer.Type} | P:{customer.Patience:F1}s | W:{customer.WaitTime:F1}s | $:{customer.Reward}][Pos:{position}]\n");
            }

            queueCountText.text = builder.ToString().TrimEnd();
        }

        private static void RegisterButton(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button != null)
            {
                button.onClick.AddListener(callback);
            }
        }

        private static void UnregisterButton(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(callback);
            }
        }

        private void RaiseServeRequested() => ServeRequested?.Invoke();
        private void RaisePeekRequested() => PeekRequested?.Invoke();
        private void RaisePauseRequested() => PauseRequested?.Invoke();
        private void RaiseResumeRequested() => ResumeRequested?.Invoke();
        private void RaiseStatsRequested() => StatsRequested?.Invoke();
        private void RaiseResetRequested() => ResetRequested?.Invoke();
    }
}
