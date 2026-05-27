using System;
using StateMachines;

namespace GameManager
{
    // Logica pura de state machine para el flujo global del juego.
    public class GameStateMachine
    {
        private readonly PhaseTimer _stateTimer = new PhaseTimer();

        public event Action<GameLoopState, GameLoopState> OnStateChanged;
        public event Action OnGameOverReached;

        public GameLoopState CurrentState { get; private set; } = GameLoopState.Preparacion;

        public float StateElapsed => _stateTimer.Elapsed;
        public float StateDuration => _stateTimer.Duration;
        public float GameElapsedSeconds { get; private set; }

        public float PreparationDuration { get; set; } = 2f;
        public float TransitionDuration { get; set; } = 2f;
        public float GameOverAfterSeconds { get; set; } = 60f;

        public void ResetRun()
        {
            GameElapsedSeconds = 0f;
            StartState(GameLoopState.Preparacion);
        }

        public void Update(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                deltaTime = 0f;
            }

            switch (CurrentState)
            {
                case GameLoopState.Preparacion:
                    if (_stateTimer.Update(deltaTime))
                    {
                        StartState(GameLoopState.Transicion);
                    }
                    break;

                case GameLoopState.Transicion:
                    if (_stateTimer.Update(deltaTime))
                    {
                        StartState(GameLoopState.Game);
                    }
                    break;

                case GameLoopState.Game:
                    GameElapsedSeconds += deltaTime;
                    if (GameElapsedSeconds >= Math.Max(0f, GameOverAfterSeconds))
                    {
                        StartState(GameLoopState.GameOver);
                        OnGameOverReached?.Invoke();
                    }
                    break;

                case GameLoopState.Reanudacion:
                    // Estado puente para telemetria/eventos.
                    StartState(GameLoopState.Game);
                    break;

                case GameLoopState.Pausa:
                case GameLoopState.GameOver:
                default:
                    // Estados sin avance automatico.
                    break;
            }
        }

        public bool Pause()
        {
            if (CurrentState == GameLoopState.Pausa || CurrentState == GameLoopState.GameOver)
            {
                return false;
            }

            StartState(GameLoopState.Pausa);
            return true;
        }

        public bool Resume()
        {
            if (CurrentState != GameLoopState.Pausa)
            {
                return false;
            }

            StartState(GameLoopState.Reanudacion);
            return true;
        }

        public void ForceGameOver()
        {
            if (CurrentState == GameLoopState.GameOver)
            {
                return;
            }

            StartState(GameLoopState.GameOver);
            OnGameOverReached?.Invoke();
        }

        private void StartState(GameLoopState next)
        {
            GameLoopState previous = CurrentState;
            CurrentState = next;

            switch (next)
            {
                case GameLoopState.Preparacion:
                    _stateTimer.Start(Math.Max(0f, PreparationDuration));
                    break;

                case GameLoopState.Transicion:
                    _stateTimer.Start(Math.Max(0f, TransitionDuration));
                    break;

                case GameLoopState.Game:
                    _stateTimer.Stop();
                    break;

                case GameLoopState.Pausa:
                case GameLoopState.Reanudacion:
                case GameLoopState.GameOver:
                default:
                    _stateTimer.Stop();
                    break;
            }

            OnStateChanged?.Invoke(previous, next);
        }
    }
}

