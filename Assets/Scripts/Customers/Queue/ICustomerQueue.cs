using System;
using System.Collections.Generic;

namespace Customers.Queue
{
    public interface ICustomerQueue
    {
        // ============ QUEUE OPERATIONS ============
        void EnqueueCustomer(Customer customer);
        Customer DequeueCustomer();
        Customer ServeCustomer(Customer customer);
        Customer ServeCustomerAt(int queueIndex);
        bool TryTakeNextCustomer(out Customer customer);
        void FinishService(Customer customer);
        Customer CompleteService(Customer customer);
        Customer PeekCustomer();
        void RemoveCustomer(Customer customer);

        // ============ QUEUE STATE ============
        int GetQueueCount();
        int GetMaxQueueSize();
        List<Customer> GetQueuedCustomersSnapshot();
        int GetAverageSatisfaction();
        bool IsQueueFull();
        bool IsQueueEmpty();

        // ============ GAME CONTROL ============
        void PauseSpawning();
        void ResumeSpawning();
        void SetSpawningEnabled(bool enabled);
        void ClearQueue();
        void Reset();

        // ============ CONFIG ============
        void Configure(GameManager.RegardsManager regardsManager);

        // ============ STATISTICS ============
        int GetCustomersServed();
        int GetCustomersLeft();
        float GetAverageWaitTime();
        int GetActiveCustomerCount();

        // ============ EVENTS ============
        event Action<Customer> OnCustomerEnqueued;
        event Action<Customer> OnCustomerDequeued;
        event Action OnQueueFull;
        event Action OnQueueEmpty;
        event Action<int> OnQueueCountChanged;
    }
}
