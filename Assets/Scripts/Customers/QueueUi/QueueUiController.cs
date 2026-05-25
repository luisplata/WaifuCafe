using UnityEngine;

namespace Customers.QueueUi
{
    public class QueueUiController : MonoBehaviour, IQueueUiController
    {
        [SerializeField] private QueueUiView viewSource;
        [SerializeField] private CustomerQueueUiService serviceSource;
        [SerializeField] private float queueTextRefreshInterval = 0.5f;

        private IQueueUiView view;
        private IQueueUiService service;
        private bool isInitialized;
        private float queueRefreshTimer;

        private void Awake()
        {
            view = viewSource as IQueueUiView;
            service = serviceSource as IQueueUiService;
            queueRefreshTimer = Mathf.Max(0.1f, queueTextRefreshInterval);

            if (view == null)
            {
                Debug.LogError("QueueUiController: viewSource must implement IQueueUiView.");
                return;
            }

            if (service == null)
            {
                Debug.LogError("QueueUiController: serviceSource must implement IQueueUiService.");
                return;
            }

            Initialize(view, service);
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Initialize(IQueueUiView queueView, IQueueUiService queueService)
        {
            if (isInitialized)
            {
                return;
            }

            view = queueView;
            service = queueService;

            view.ServeRequested += HandleServeRequested;
            view.PeekRequested += HandlePeekRequested;
            view.PauseRequested += HandlePauseRequested;
            view.ResumeRequested += HandleResumeRequested;
            view.StatsRequested += HandleStatsRequested;
            view.ResetRequested += HandleResetRequested;

            service.CustomerEnqueued += HandleCustomerEnqueued;
            service.CustomerDequeued += HandleCustomerDequeued;
            service.QueueFull += HandleQueueFull;
            service.QueueEmpty += HandleQueueEmpty;
            service.QueueCountChanged += HandleQueueCountChanged;

            view.SetSpawningPaused(false);
            RefreshQueueEntries();
            view.SetStatus("Queue UI ready.");
            queueRefreshTimer = Mathf.Max(0.1f, queueTextRefreshInterval);

            isInitialized = true;
        }

        public void Dispose()
        {
            if (!isInitialized || view == null || service == null)
            {
                return;
            }

            view.ServeRequested -= HandleServeRequested;
            view.PeekRequested -= HandlePeekRequested;
            view.PauseRequested -= HandlePauseRequested;
            view.ResumeRequested -= HandleResumeRequested;
            view.StatsRequested -= HandleStatsRequested;
            view.ResetRequested -= HandleResetRequested;

            service.CustomerEnqueued -= HandleCustomerEnqueued;
            service.CustomerDequeued -= HandleCustomerDequeued;
            service.QueueFull -= HandleQueueFull;
            service.QueueEmpty -= HandleQueueEmpty;
            service.QueueCountChanged -= HandleQueueCountChanged;

            isInitialized = false;
        }

        private void HandleServeRequested()
        {
            Customer served = service.ServeNext();
            if (served == null)
            {
                view.SetStatus("No customers in queue.");
                return;
            }

            view.SetStatus($"Served {served.Type} (+{served.Reward}).");
        }

        private void HandlePeekRequested()
        {
            Customer next = service.PeekNext();
            if (next == null)
            {
                view.SetStatus("No customers waiting.");
                return;
            }

            view.SetStatus($"Next: {next.Type} (Patience {next.Patience:F1}s).");
        }

        private void HandlePauseRequested()
        {
            service.PauseSpawning();
            view.SetSpawningPaused(true);
            view.SetStatus("Spawning paused.");
        }

        private void HandleResumeRequested()
        {
            service.ResumeSpawning();
            view.SetSpawningPaused(false);
            view.SetStatus("Spawning resumed.");
        }

        private void HandleStatsRequested()
        {
            service.PrintStatistics();
            view.SetStatus("Statistics printed in Console.");
        }

        private void HandleResetRequested()
        {
            service.ResetQueue();
            view.SetSpawningPaused(false);
            view.SetStatus("Queue reset.");
        }

        private void HandleCustomerEnqueued(Customer customer)
        {
            view.SetStatus($"Arrived: {customer.Type}.");
        }

        private void HandleCustomerDequeued(Customer customer)
        {
            view.SetStatus($"Dequeued: {customer.Type}.");
        }

        private void HandleQueueFull()
        {
            view.SetStatus("Queue is full.");
        }

        private void HandleQueueEmpty()
        {
            view.SetStatus("Queue is empty.");
        }

        private void HandleQueueCountChanged(int count)
        {
            RefreshQueueEntries();
        }

        public void ServeByIndex(int queueIndex)
        {
            Customer served = service != null ? service.ServeAt(queueIndex) : null;
            if (served == null)
            {
                if (view != null)
                {
                    view.SetStatus($"Cannot serve customer at index {queueIndex}.");
                }

                return;
            }

            if (view != null)
            {
                view.SetStatus($"Served {served.Type} from position {queueIndex}.");
                RefreshQueueEntries();
            }
        }

        private void RefreshQueueEntries()
        {
            if (view == null || service == null)
            {
                return;
            }

            view.SetQueueEntries(service.GetWaitingCustomers());
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            queueRefreshTimer -= Time.deltaTime;
            if (queueRefreshTimer > 0f)
            {
                return;
            }

            RefreshQueueEntries();
            queueRefreshTimer = Mathf.Max(0.1f, queueTextRefreshInterval);
        }
    }
}
