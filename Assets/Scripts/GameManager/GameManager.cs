using System;
using UnityEngine;
using Staff;
using Customers;
using Customers.Queue;
using V2.Customer;

namespace GameManager
{
    // Orquesta la state machine global y aplica side effects de pausa/reanudacion.
    public class GameManager : MonoBehaviour
    {
        [Header("State Durations")] [SerializeField]
        private float preparationDuration = 2f;

        [SerializeField] private float transitionDuration = 2f;
        [SerializeField] private float gameOverAfterSeconds = 60f;

        [Header("Behavior")] [SerializeField] private bool autoStart = true;

        [Header("References")] [SerializeField]
        private StaffManager staffManager;

        [SerializeField] private CustomerQueue customerQueue;

        [SerializeField] private ServiceCoordinator serviceCoordinator;

        [Header("Golds")] [SerializeField] private RegardsManager regardsManager;

        private GameStateMachine _machine;

        public event Action<GameLoopState, GameLoopState> OnStateChanged;
        public event Action OnGameOverReached;

        public GameLoopState CurrentState => _machine != null ? _machine.CurrentState : GameLoopState.Preparacion;
        public float StateElapsed => _machine != null ? _machine.StateElapsed : 0f;
        public float StateDuration => _machine != null ? _machine.StateDuration : 0f;
        public float GameElapsedSeconds => _machine != null ? _machine.GameElapsedSeconds : 0f;

        private void Awake()
        {
            CreateMachine();
        }

        private void Start()
        {
            if (autoStart)
            {
                StartRun();
            }
        }

        private void Update()
        {
            if (_machine == null)
            {
                return;
            }

            _machine.Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            DetachMachineEvents();
            if (CurrentState == GameLoopState.Pausa || CurrentState == GameLoopState.GameOver)
            {
                Time.timeScale = 1f;
            }
        }

        private void OnApplicationQuit()
        {
            // Evita dejar el editor con timescale en cero al salir en Play Mode.
            Time.timeScale = 1f;
        }

        public void StartRun()
        {
            if (_machine == null)
            {
                CreateMachine();
            }

            GameStateMachine machine = _machine;
            if (machine == null)
            {
                return;
            }

            machine.ResetRun();
        }

        public bool PauseGame()
        {
            return _machine != null && _machine.Pause();
        }

        public bool ResumeGame()
        {
            return _machine != null && _machine.Resume();
        }

        public bool TogglePause()
        {
            if (_machine == null)
            {
                return false;
            }

            if (CurrentState == GameLoopState.Pausa)
            {
                return ResumeGame();
            }

            return PauseGame();
        }

        public void ForceGameOver()
        {
            _machine?.ForceGameOver();
        }

        private void CreateMachine()
        {
            DetachMachineEvents();

            _machine = new GameStateMachine
            {
                PreparationDuration = preparationDuration,
                TransitionDuration = transitionDuration,
                GameOverAfterSeconds = gameOverAfterSeconds
            };

            _machine.OnStateChanged += HandleStateChanged;
            _machine.OnGameOverReached += HandleGameOverReached;
        }

        private void DetachMachineEvents()
        {
            if (_machine == null)
            {
                return;
            }

            _machine.OnStateChanged -= HandleStateChanged;
            _machine.OnGameOverReached -= HandleGameOverReached;
        }

        private void HandleStateChanged(GameLoopState previous, GameLoopState next)
        {
            // On Preparacion, initialize subsystems so they start in a known state
            if (next == GameLoopState.Preparacion)
            {
                try
                {
                    staffManager?.Configure();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameManager] Error configuring StaffManager: {ex.Message}");
                }

                try
                {
                    customerQueue?.Configure(regardsManager);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameManager] Error configuring CustomerQueue: {ex.Message}");
                }

                // Subscribe to customer events for gold accumulation
                if (customerQueue != null)
                {
                    customerQueue.OnCustomerEnqueued += OnCustomerEnqueued;
                    customerQueue.OnCustomerDequeued += OnCustomerDequeued;
                }
            }

            bool shouldPauseTime = next == GameLoopState.Pausa || next == GameLoopState.GameOver;
            Time.timeScale = shouldPauseTime ? 0f : 1f;

            Debug.Log($"[SM][Game] {previous} -> {next}");
            OnStateChanged?.Invoke(previous, next);
        }

        private void HandleGameOverReached()
        {
            Debug.Log("[SM][Game] GameOver reached by timer");

            // Limpiar subsistemas para evitar que queden staff/customers disponibles tras el GameOver
            try
            {
                staffManager?.ClearAllStaff();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameManager] Error clearing StaffManager on GameOver: {ex.Message}");
            }

            // Unsubscribe from customer events
            if (customerQueue != null)
            {
                customerQueue.OnCustomerEnqueued -= OnCustomerEnqueued;
                customerQueue.OnCustomerDequeued -= OnCustomerDequeued;
            }

            try
            {
                customerQueue?.ClearQueue();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameManager] Error clearing CustomerQueue on GameOver: {ex.Message}");
            }

            OnGameOverReached?.Invoke();
        }

        private void OnCustomerEnqueued(Customer customer)
        {
            customer.OnGoldEarned += HandleCustomerGoldEarned;
        }

        private void OnCustomerDequeued(Customer customer)
        {
            customer.OnGoldEarned -= HandleCustomerGoldEarned;
        }

        private void HandleCustomerGoldEarned(int reward)
        {
            if (regardsManager != null)
            {
                regardsManager.AddGold(reward);
            }
        }
    }
}