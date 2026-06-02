using UnityEngine;
using Customers.Queue.Input;

namespace Customers.Queue.Examples
{
    /// <summary>
    /// Ejemplo de cómo usar el CustomerQueue en tu juego.
    /// Este script muestra los eventos principales y operaciones.
    /// 
    /// Controles:
    /// E - Atender cliente
    /// Q - Ver siguiente en cola
    /// P - Pausar spawn
    /// R - Reanudar spawn
    /// S - Ver estadísticas
    /// Del - Reset completo
    /// </summary>
    public class CustomerQueueExample : MonoBehaviour
    {
        [SerializeField] private CustomerQueue queue;
        [SerializeField] private QueueInputHandler inputHandler;

        private void Start()
        {
            // Validar que tenemos los componentes necesarios
            if (queue == null)
                Debug.LogError("CustomerQueue no asignado en CustomerQueueExample!");
            
            if (inputHandler == null)
                Debug.LogError("QueueInputHandler no asignado en CustomerQueueExample!");

            // Suscribirse a eventos de queue
            queue.OnCustomerEnqueued += HandleCustomerArrived;
            queue.OnCustomerDequeued += HandleCustomerServed;
            queue.OnQueueFull += HandleQueueFull;
            queue.OnQueueEmpty += HandleQueueEmpty;
            queue.OnQueueCountChanged += HandleQueueCountChanged;

            // Suscribirse a eventos de input
            inputHandler.OnServeCustomerPressed += ServeNextCustomer;
            inputHandler.OnPeekCustomerPressed += PeekNextCustomer;
            inputHandler.OnPausePressed += HandlePauseInput;
            inputHandler.OnResumePressed += HandleResumeInput;
            inputHandler.OnStatsPressed += HandleStatsInput;
            inputHandler.OnResetPressed += HandleResetInput;
        }

        private void OnDestroy()
        {
            // Desuscribirse cuando se destruya
            if (queue != null)
            {
                queue.OnCustomerEnqueued -= HandleCustomerArrived;
                queue.OnCustomerDequeued -= HandleCustomerServed;
                queue.OnQueueFull -= HandleQueueFull;
                queue.OnQueueEmpty -= HandleQueueEmpty;
                queue.OnQueueCountChanged -= HandleQueueCountChanged;
            }

            if (inputHandler != null)
            {
                inputHandler.OnServeCustomerPressed -= ServeNextCustomer;
                inputHandler.OnPeekCustomerPressed -= PeekNextCustomer;
                inputHandler.OnPausePressed -= HandlePauseInput;
                inputHandler.OnResumePressed -= HandleResumeInput;
                inputHandler.OnStatsPressed -= HandleStatsInput;
                inputHandler.OnResetPressed -= HandleResetInput;
            }
        }

        // ============ EVENT HANDLERS ============
        private void HandleCustomerArrived(Customer customer)
        {
            Debug.Log($"🎉 {customer.Type} customer arrived! Reward: {customer.Reward}");
        }

        private void HandleCustomerServed(Customer customer)
        {
            Debug.Log($"✓ {customer.Type} customer served with wait time: {customer.WaitTime:F2}s");
        }

        private void HandleQueueFull()
        {
            Debug.LogWarning("⚠️ Queue is full! Customer rejected!");
        }

        private void HandleQueueEmpty()
        {
            Debug.Log("📭 Queue is now empty!");
        }

        private void HandleQueueCountChanged(int newCount)
        {
            Debug.Log($"📊 Queue count: {newCount}");
        }

        // ============ INPUT HANDLERS ============
        private void HandlePauseInput()
        {
            queue.PauseSpawning();
            Debug.Log("⏸️ Spawning paused");
        }

        private void HandleResumeInput()
        {
            queue.ResumeSpawning();
            Debug.Log("▶️ Spawning resumed");
        }

        private void HandleStatsInput()
        {
            queue.PrintStatistics();
        }

        private void HandleResetInput()
        {
            queue.Reset();
            Debug.Log("🔄 Queue reset!");
        }

        private void ServeNextCustomer()
        {
            Customer customer = queue.DequeueCustomer();
            if (customer != null)
            {
                Debug.Log($"✓ Served {customer.Type} - Wait: {customer.WaitTime:F1}s - +{customer.Reward} coins");
            }
            else
            {
                Debug.LogWarning("❌ No customers in queue!");
            }
        }

        private void PeekNextCustomer()
        {
            Customer customer = queue.PeekCustomer();
            if (customer != null)
            {
                Debug.Log($"👀 Next: {customer.Type} - Patience: {customer.Patience:F1}s - Reward: {customer.Reward}");
            }
            else
            {
                Debug.LogWarning("❌ No customers waiting!");
            }
        }
    }
}

