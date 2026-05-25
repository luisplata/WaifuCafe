using UnityEngine;
using UnityEngine.InputSystem;

namespace Customers.Queue.Input
{
    /// <summary>
    /// Versión avanzada de QueueInputHandler que usa InputSystem_Actions.inputactions.
    /// Implementa IQueueInputProvider para ser intercambiable.
    /// 
    /// Antes de usar:
    /// 1. Abre Assets/InputSystem_Actions.inputactions
    /// 2. Agrega estas acciones en el mapa "UI":
    ///    - ServeCustomer (Button, E)
    ///    - PeekCustomer (Button, Q)
    ///    - Pause (Button, P)
    ///    - Resume (Button, R)
    ///    - PrintStats (Button, S)
    ///    - Reset (Button, Delete)
    /// 3. Genera C# class desde el editor
    /// 4. Reemplaza QueueInputHandler por este en tu scene
    /// </summary>
    public class QueueInputHandlerAdvanced : MonoBehaviour, IQueueInputProvider
    {
        [SerializeField] private CustomerQueue queue;
        
        private InputSystem_Actions inputActions;

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
            inputActions?.Enable();
        }

        private void OnDisable()
        {
            inputActions?.Disable();
        }

        protected virtual void InitializeInputActions()
        {
            inputActions = new InputSystem_Actions();

            // Intentar usar mapa "UI" primero, fallback a "Player"
            try
            {
                // Si existe un mapa "UI"
                var uiMap = inputActions.asset.FindActionMap("UI");
                if (uiMap != null)
                {
                    BindUIActions();
                    return;
                }
            }
            catch { }

            // Fallback a "Player" o el primer mapa disponible
            try
            {
                BindPlayerActions();
            }
            catch
            {
                Debug.LogWarning("No se pudieron bindear las acciones de input. Asegúrate de que InputSystem_Actions.inputactions tiene las acciones configuradas.");
            }
        }

        private void BindUIActions()
        {
            // Asumir que las acciones están en el mapa "UI"
            try
            {
                // Si generaste la clase con InputSystem_Actions.cs
                // y tiene una propiedad pública UI con estas acciones
                var ui = inputActions.asset.FindActionMap("UI");
                
                var serveAction = ui.FindAction("ServeCustomer");
                var peekAction = ui.FindAction("PeekCustomer");
                var pauseAction = ui.FindAction("Pause");
                var resumeAction = ui.FindAction("Resume");
                var statsAction = ui.FindAction("PrintStats");
                var resetAction = ui.FindAction("Reset");

                serveAction.performed += _ => OnServeCustomerPressed?.Invoke();
                peekAction.performed += _ => OnPeekCustomerPressed?.Invoke();
                pauseAction.performed += _ => OnPausePressed?.Invoke();
                resumeAction.performed += _ => OnResumePressed?.Invoke();
                statsAction.performed += _ => OnStatsPressed?.Invoke();
                resetAction.performed += _ => OnResetPressed?.Invoke();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error bindear acciones UI: {ex.Message}");
            }
        }

        private void BindPlayerActions()
        {
            // Fallback: bindear directamente del asset
            try
            {
                var playerMap = inputActions.asset.FindActionMap("Player");
                
                // Buscar por nombre (flexible)
                foreach (var action in playerMap.actions)
                {
                    switch (action.name)
                    {
                        case "ServeCustomer":
                        case "Serve":
                        case "Interact":
                            action.performed += _ => OnServeCustomerPressed?.Invoke();
                            break;
                        case "PeekCustomer":
                        case "Look":
                            action.performed += _ => OnPeekCustomerPressed?.Invoke();
                            break;
                        case "Pause":
                            action.performed += _ => OnPausePressed?.Invoke();
                            break;
                        case "Resume":
                            action.performed += _ => OnResumePressed?.Invoke();
                            break;
                        case "PrintStats":
                        case "Stats":
                            action.performed += _ => OnStatsPressed?.Invoke();
                            break;
                        case "Reset":
                            action.performed += _ => OnResetPressed?.Invoke();
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error bindear acciones Player: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            inputActions?.Dispose();
        }

        // ============ PUBLIC QUERIES ============
        public bool IsServePressed()
        {
            try
            {
                return inputActions?.asset.FindActionMap("UI")?.FindAction("ServeCustomer")?.IsPressed() ?? false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsPeekPressed()
        {
            try
            {
                return inputActions?.asset.FindActionMap("UI")?.FindAction("PeekCustomer")?.IsPressed() ?? false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsPausePressed()
        {
            try
            {
                return inputActions?.asset.FindActionMap("UI")?.FindAction("Pause")?.IsPressed() ?? false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsResumePressed()
        {
            try
            {
                return inputActions?.asset.FindActionMap("UI")?.FindAction("Resume")?.IsPressed() ?? false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsStatsPressed()
        {
            try
            {
                return inputActions?.asset.FindActionMap("UI")?.FindAction("PrintStats")?.IsPressed() ?? false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsResetPressed()
        {
            try
            {
                return inputActions?.asset.FindActionMap("UI")?.FindAction("Reset")?.IsPressed() ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Remapea una acción a una nueva tecla en runtime.
        /// Ej: handler.RemapAction("ServeCustomer", new Key[] { Key.Space });
        /// </summary>
        public void RemapAction(string actionName, Key newKey)
        {
            try
            {
                var action = inputActions.asset.FindActionMap("UI").FindAction(actionName);
                if (action != null)
                {
                    // Crear nuevos bindings
                    for (int i = 0; i < action.bindings.Count; i++)
                    {
                        var binding = action.bindings[i];
                        binding.overridePath = $"<Keyboard>/{newKey.ToString()}";
                        action.ApplyBindingOverride(i, binding);
                        break; // Solo el primer binding
                    }

                    Debug.Log($"✓ Remapped {actionName} to {newKey}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error remapping action: {ex.Message}");
            }
        }
    }
}

