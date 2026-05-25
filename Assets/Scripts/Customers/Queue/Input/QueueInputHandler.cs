using UnityEngine;
using UnityEngine.InputSystem;

namespace Customers.Queue.Input
{
    /// <summary>
    /// Maneja todos los inputs para CustomerQueue usando el New Input System.
    /// Centraliza la lógica de input en una clase separada para mejor mantenimiento.
    /// Implementa IQueueInputProvider para ser intercambiable con otras implementaciones.
    /// </summary>
    public class QueueInputHandler : MonoBehaviour, IQueueInputProvider
    {
        [SerializeField] private CustomerQueue queue;

        // ============ INPUT ACTION REFERENCES ============
        private InputAction serveCustomerAction;
        private InputAction peekCustomerAction;
        private InputAction pauseAction;
        private InputAction resumeAction;
        private InputAction statsAction;
        private InputAction resetAction;

        // ============ EVENTS ============
        public event System.Action OnServeCustomerPressed;
        public event System.Action OnPeekCustomerPressed;
        public event System.Action OnPausePressed;
        public event System.Action OnResumePressed;
        public event System.Action OnStatsPressed;
        public event System.Action OnResetPressed;

        private void Awake()
        {
            InitializeInputActions();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        /// <summary>
        /// Inicializa las acciones de input del sistema.
        /// Puedes anular este método para usar InputSystem_Actions.inputactions
        /// </summary>
        protected virtual void InitializeInputActions()
        {
            // Crear acciones programáticamente
            serveCustomerAction = new InputAction("ServeCustomer", InputActionType.Button, "<Keyboard>/e");
            peekCustomerAction = new InputAction("PeekCustomer", InputActionType.Button, "<Keyboard>/q");
            pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/p");
            resumeAction = new InputAction("Resume", InputActionType.Button, "<Keyboard>/r");
            statsAction = new InputAction("Stats", InputActionType.Button, "<Keyboard>/s");
            resetAction = new InputAction("Reset", InputActionType.Button, "<Keyboard>/delete");

            // Suscribirse a los eventos de input
            serveCustomerAction.performed += _ => OnServeCustomerPressed?.Invoke();
            peekCustomerAction.performed += _ => OnPeekCustomerPressed?.Invoke();
            pauseAction.performed += _ => OnPausePressed?.Invoke();
            resumeAction.performed += _ => OnResumePressed?.Invoke();
            statsAction.performed += _ => OnStatsPressed?.Invoke();
            resetAction.performed += _ => OnResetPressed?.Invoke();
        }

        private void EnableInputActions()
        {
            serveCustomerAction?.Enable();
            peekCustomerAction?.Enable();
            pauseAction?.Enable();
            resumeAction?.Enable();
            statsAction?.Enable();
            resetAction?.Enable();
        }

        private void DisableInputActions()
        {
            serveCustomerAction?.Disable();
            peekCustomerAction?.Disable();
            pauseAction?.Disable();
            resumeAction?.Disable();
            statsAction?.Disable();
            resetAction?.Disable();
        }

        private void OnDestroy()
        {
            // Limpiar acciones
            serveCustomerAction?.Dispose();
            peekCustomerAction?.Dispose();
            pauseAction?.Dispose();
            resumeAction?.Dispose();
            statsAction?.Dispose();
            resetAction?.Dispose();
        }

        // ============ PUBLIC QUERIES ============
        public bool IsServePressed() => serveCustomerAction?.IsPressed() ?? false;
        public bool IsPeekPressed() => peekCustomerAction?.IsPressed() ?? false;
        public bool IsPausePressed() => pauseAction?.IsPressed() ?? false;
        public bool IsResumePressed() => resumeAction?.IsPressed() ?? false;
        public bool IsStatsPressed() => statsAction?.IsPressed() ?? false;
        public bool IsResetPressed() => resetAction?.IsPressed() ?? false;

        /// <summary>
        /// Remapea una acción a una nueva tecla en runtime.
        /// Ej: handler.RemapAction("ServeCustomer", Key.Space);
        /// </summary>
        public void RemapAction(string actionName, Key newKey)
        {
            InputAction action = actionName switch
            {
                "Serve" or "ServeCustomer" => serveCustomerAction,
                "Peek" or "PeekCustomer" => peekCustomerAction,
                "Pause" => pauseAction,
                "Resume" => resumeAction,
                "Stats" or "Statistics" => statsAction,
                "Reset" => resetAction,
                _ => null
            };

            if (action == null)
            {
                Debug.LogWarning($"Action '{actionName}' not found!");
                return;
            }

            if (action.bindings.Count == 0)
            {
                Debug.LogWarning($"Action '{actionName}' has no bindings to remap.");
                return;
            }

            string keyPath = GetKeyboardPath(newKey);
            if (string.IsNullOrEmpty(keyPath))
            {
                Debug.LogWarning($"Key '{newKey}' is not mapped to a keyboard control path.");
                return;
            }

            bool wasEnabled = action.enabled;
            if (wasEnabled) action.Disable();
            action.ChangeBinding(0).WithPath(keyPath);
            if (wasEnabled) action.Enable();

            Debug.Log($"Remapped {actionName} to {newKey} ({keyPath})");
        }

        private static string GetKeyboardPath(Key key)
        {
            return key switch
            {
                Key.Space => "<Keyboard>/space",
                Key.Enter => "<Keyboard>/enter",
                Key.Escape => "<Keyboard>/escape",
                Key.Backspace => "<Keyboard>/backspace",
                Key.Tab => "<Keyboard>/tab",
                Key.Delete => "<Keyboard>/delete",
                Key.UpArrow => "<Keyboard>/upArrow",
                Key.DownArrow => "<Keyboard>/downArrow",
                Key.LeftArrow => "<Keyboard>/leftArrow",
                Key.RightArrow => "<Keyboard>/rightArrow",
                _ => $"<Keyboard>/{key.ToString().ToLowerInvariant()}"
            };
        }
    }
}
