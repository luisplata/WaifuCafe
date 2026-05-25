using System;
using System.Collections.Generic;
using Customers;

namespace Customers.QueueUi
{
    public interface IQueueUiService
    {
        event Action<Customer> CustomerEnqueued;
        event Action<Customer> CustomerDequeued;
        event Action QueueFull;
        event Action QueueEmpty;
        event Action<int> QueueCountChanged;

        int MaxQueueSize { get; }

        Customer ServeNext();
        Customer ServeAt(int queueIndex);
        Customer ServeCustomer(Customer customer);
        Customer PeekNext();
        IReadOnlyList<Customer> GetWaitingCustomers();
        void PauseSpawning();
        void ResumeSpawning();
        void PrintStatistics();
        void ResetQueue();
    }
}
