using System;
using UnityEngine;
using StateMachines;

namespace Staff
{
    [Serializable]
    public class Staff
    {
        public string name;
        // Identificador dentro del pool (index)
        public int Index;

        // Tiempo que tarda en preparar el pedido (en segundos)
        public float ServiceTime = 5f;

        // Indica si el personal está ocupado atendiendo a alguien
        [NonSerialized] public bool IsBusy = false;

        // Estado actual del staff
        public StaffPhase CurrentPhase = StaffPhase.EnEspera;

        // Temporizador para la fase actual
        [NonSerialized] private PhaseTimer _phaseTimer = new PhaseTimer();

        private string Label => string.IsNullOrWhiteSpace(name) ? $"Staff#{Index}" : name;

        public float GetCurrentPhaseElapsed() => _phaseTimer.Elapsed;

        public float GetCurrentPhaseDuration() => _phaseTimer.Duration;

        public string GetCurrentPhaseLabel() => CurrentPhase.ToString();

        // Puedes añadir más stats aquí si hacen falta (skill, speed, mood, etc.)

        // Método simple que indica si puede atender ahora mismo
        public bool CanAttend() => CurrentPhase == StaffPhase.EnEspera && !IsBusy;

        // Orden para que el staff empiece a atender a un cliente.
        // Retorna true si la orden fue aceptada.
        public bool CommandAttend()
        {
            if (CurrentPhase != StaffPhase.EnEspera) return false;

            Debug.Log($"[SM][Staff] {Label}: command attend received");
            StartPhase(StaffPhase.AtendiendoCliente);
            return true;
        }

        // Inicia una fase y ajusta el temporizador según las reglas por defecto.
        public void StartPhase(StaffPhase phase)
        {
            var previous = CurrentPhase;
            CurrentPhase = phase;

            if (previous != phase)
            {
                Debug.Log($"[SM][Staff] {Label}: {previous} -> {phase}");
            }

            switch (phase)
            {
                case StaffPhase.EnEspera:
                    _phaseTimer.Stop();
                    IsBusy = false;
                    break;
                case StaffPhase.AtendiendoCliente:
                    // Tiempo fijo para tomar el pedido
                    _phaseTimer.Start(2f);
                    IsBusy = true;
                    break;
                case StaffPhase.PreparandoPedido:
                    // Usa ServiceTime definido en el staff
                    // Evitar bloqueo: si ServiceTime es 0 o negativo, usar una duración mínima
                    // para permitir que UpdatePhase dispare la transición al siguiente estado.
                    _phaseTimer.Start(Mathf.Max(0.01f, ServiceTime));
                    IsBusy = true;
                    break;
                case StaffPhase.LlevandoPedidoCocina:
                    // Tiempo fijo para llevar el pedido
                    _phaseTimer.Start(2f);
                    IsBusy = true;
                    break;
                default:
                    _phaseTimer.Stop();
                    IsBusy = false;
                    break;
            }
        }

        // Actualizar el estado del staff; deltaTime en segundos.
        // Este método debería llamarse periódicamente por un manager o MonoBehaviour.
        public void UpdatePhase(float deltaTime)
        {
            if (_phaseTimer.Update(deltaTime))
            {
                // El temporizador terminó; aplicar transición por defecto basada en la fase actual
                switch (CurrentPhase)
                {
                    case StaffPhase.AtendiendoCliente:
                        Debug.Log($"[SM][Staff] {Label}: atendiendo finished, preparing order");
                        // Después de tomar el pedido -> empezar a preparar
                        StartPhase(StaffPhase.PreparandoPedido);
                        break;
                    case StaffPhase.PreparandoPedido:
                        Debug.Log($"[SM][Staff] {Label}: preparing finished, delivering order");
                        // Después de preparar -> llevar al cliente
                        StartPhase(StaffPhase.LlevandoPedidoCocina);
                        break;
                    case StaffPhase.LlevandoPedidoCocina:
                        Debug.Log($"[SM][Staff] {Label}: delivery finished, back to idle");
                        // Después de llevar -> volver a estar en espera
                        StartPhase(StaffPhase.EnEspera);
                        break;
                    default:
                        // Para EnEspera y casos no contemplados, quedarse en EnEspera
                        StartPhase(StaffPhase.EnEspera);
                        break;
                }
            }
        }
    }
}


