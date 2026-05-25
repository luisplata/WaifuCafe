using System;
using System.Collections.Generic;
using Customers.Queue;
using UnityEngine;

namespace Customers.QueueUi
{
    public class CustomerQueueUiService : MonoBehaviour, IQueueUiService
    {
        [SerializeField] private CustomerQueue queue;
        [SerializeField] private int configuredMaxQueueSize = 10;

        public event Action<Customer> CustomerEnqueued;
        public event Action<Customer> CustomerDequeued;
        public event Action QueueFull;
        public event Action QueueEmpty;
        public event Action<int> QueueCountChanged;

        public int MaxQueueSize => Mathf.Max(1, configuredMaxQueueSize);

        private void OnEnable()
        {
            if (queue == null)
            {
                Debug.LogError("CustomerQueueUiService: queue is not assigned.");
                return;
            }

            queue.OnCustomerEnqueued += ForwardEnqueued;
            queue.OnCustomerDequeued += ForwardDequeued;
            queue.OnQueueFull += ForwardQueueFull;
            queue.OnQueueEmpty += ForwardQueueEmpty;
            queue.OnQueueCountChanged += ForwardQueueCountChanged;
        }

        private void OnDisable()
        {
            if (queue == null)
            {
                return;
            }

            queue.OnCustomerEnqueued -= ForwardEnqueued;
            queue.OnCustomerDequeued -= ForwardDequeued;
            queue.OnQueueFull -= ForwardQueueFull;
            queue.OnQueueEmpty -= ForwardQueueEmpty;
            queue.OnQueueCountChanged -= ForwardQueueCountChanged;
        }

        public Customer ServeNext()
        {
            return queue != null ? queue.DequeueCustomer() : null;
        }

        public Customer ServeAt(int queueIndex)
        {
            return queue != null ? queue.ServeCustomerAt(queueIndex) : null;
        }

        public Customer ServeCustomer(Customer customer)
        {
            return queue != null ? queue.ServeCustomer(customer) : null;
        }

        public Customer PeekNext()
        {
            return queue != null ? queue.PeekCustomer() : null;
        }

        public IReadOnlyList<Customer> GetWaitingCustomers()
        {
            return queue != null ? queue.GetQueuedCustomersSnapshot() : Array.Empty<Customer>();
        }

        public void PauseSpawning()
        {
            if (queue != null)
            {
                queue.PauseSpawning();
            }
        }

        public void ResumeSpawning()
        {
            if (queue != null)
            {
                queue.ResumeSpawning();
            }
        }

        public void PrintStatistics()
        {
            if (queue != null)
            {
                queue.PrintStatistics();
            }
        }

        public void ResetQueue()
        {
            if (queue != null)
            {
                queue.Reset();
            }
        }

        private void ForwardEnqueued(Customer customer) => CustomerEnqueued?.Invoke(customer);
        private void ForwardDequeued(Customer customer) => CustomerDequeued?.Invoke(customer);
        private void ForwardQueueFull() => QueueFull?.Invoke();
        private void ForwardQueueEmpty() => QueueEmpty?.Invoke();
        private void ForwardQueueCountChanged(int count) => QueueCountChanged?.Invoke(count);
    }
}
