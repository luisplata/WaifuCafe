using System;
using GameManager;
using UnityEngine;
using StateMachines;

namespace Customers
{
    [Serializable]
    public class Customer
    {
        public CustomerType Type;
        public float Patience;
        public int Reward;

        private bool _fueAtendido = false;

        // Estado actual del cliente dentro de la state machine
        public CustomerPhase CurrentPhase = CustomerPhase.Llegada;

        [NonSerialized] public float WaitTime = 0f;
        [NonSerialized] public bool WasServed = false;

        [NonSerialized] private PhaseTimer _phaseTimer = new PhaseTimer();
        private RegardsManager _regardsManager;

        private string Label => Type.ToString();

        public float GetCurrentPhaseElapsed() => _phaseTimer.Elapsed;

        public float GetCurrentPhaseDuration() => _phaseTimer.Duration;

        public string GetCurrentPhaseLabel() => CurrentPhase.ToString();

        public void StartPhase(CustomerPhase phase)
        {
            var previous = CurrentPhase;
            CurrentPhase = phase;

            if (previous != phase)
            {
                Debug.Log($"[SM][Customer] {Label}: {previous} -> {phase}");
            }

            switch (phase)
            {
                case CustomerPhase.Llegada:
                    WaitTime = 0f;
                    _phaseTimer.Start(3f);
                    break;
                case CustomerPhase.EsperaPedido:
                    WaitTime = 0f;
                    _phaseTimer.Start(Math.Max(0f, Patience));
                    break;
                case CustomerPhase.EsperandoEntrega:
                    // Esperando entrega: esperar a que el staff entregue el pedido.
                    // No iniciar temporizador automático aquí; la transición a Consumir
                    // será disparada por el staff/coordindor cuando la entrega ocurra.
                    _fueAtendido = true;
                    _phaseTimer.Stop();
                    break;
                case CustomerPhase.Consumir:
                    _phaseTimer.Start(5f);
                    break;
                case CustomerPhase.Irse:
                    _phaseTimer.Start(3f);
                    if (_fueAtendido)
                    {
                        _regardsManager.AddGold(Reward);
                        Debug.Log($"[SM][Customer] {Label}: Served and leaving, reward {Reward} gold");
                    }

                    break;
                default:
                    _phaseTimer.Stop();
                    break;
            }
        }

        public void UpdatePhase(float deltaTime)
        {
            if (!_phaseTimer.Update(deltaTime)) return;

            switch (CurrentPhase)
            {
                case CustomerPhase.Llegada:
                    Debug.Log($"[SM][Customer] {Label}: Llegada completed");
                    StartPhase(CustomerPhase.EsperaPedido);
                    break;
                case CustomerPhase.EsperaPedido:
                    Debug.Log($"[SM][Customer] {Label}: Patience expired, leaving");
                    StartPhase(CustomerPhase.Irse);
                    break;
                // EsperandoEntrega no avanza por temporizador: la entrega será notificada por el staff
                case CustomerPhase.Consumir:
                    Debug.Log($"[SM][Customer] {Label}: Consumption completed");
                    StartPhase(CustomerPhase.Irse);
                    break;
                default:
                    _phaseTimer.Stop();
                    break;
            }
        }

        public void AddRegardManager(RegardsManager regardsManager)
        {
            _regardsManager = regardsManager;
        }
    }
}