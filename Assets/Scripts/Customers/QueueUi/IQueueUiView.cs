using System;
using System.Collections.Generic;
using Customers;

namespace Customers.QueueUi
{
    public interface IQueueUiView
    {
        event Action ServeRequested;
        event Action PeekRequested;
        event Action PauseRequested;
        event Action ResumeRequested;
        event Action StatsRequested;
        event Action ResetRequested;

        void SetQueueEntries(IReadOnlyList<Customer> customers);
        void SetStatus(string message);
        void SetSpawningPaused(bool isPaused);
    }
}
