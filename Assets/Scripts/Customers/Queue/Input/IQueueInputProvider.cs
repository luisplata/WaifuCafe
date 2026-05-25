using System;

namespace Customers.Queue.Input
{
    /// <summary>
    /// Interface para cualquier proveedor de input del Queue.
    /// Permite implementar diferentes sistemas de input (teclado, panel UI, etc.)
    /// </summary>
    public interface IQueueInputProvider
    {
        // ============ EVENTS ============
        event Action OnServeCustomerPressed;
        event Action OnPeekCustomerPressed;
        event Action OnPausePressed;
        event Action OnResumePressed;
        event Action OnStatsPressed;
        event Action OnResetPressed;

        // ============ QUERIES ============
        bool IsServePressed();
        bool IsPeekPressed();
        bool IsPausePressed();
        bool IsResumePressed();
        bool IsStatsPressed();
        bool IsResetPressed();
    }
}

