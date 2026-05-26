using System;
using StateMachines;

namespace CustomerNamespace
{
    [Serializable]
    public class Customer
    {
        public string name;
        public int Index;

        // Cuánto tiempo tiene de paciencia esperando el pedido (en segundos)
        public float Patience = 10f;

        // Estado actual del cliente
        public CustomerPhase CurrentPhase = CustomerPhase.Llegada;

        [NonSerialized] private PhaseTimer _phaseTimer = new PhaseTimer();

        public Customer()
        {
            // Por defecto iniciamos en Llegada
            StartPhase(CustomerPhase.Llegada);
        }

        // Inicia una fase con duraciones por defecto (según lo pedido):
        // Llegada (3s), EsperaPedido (Patience), EsperandoEntrega (placeholder 3s), Consumir (5s), Irse (3s)
        public void StartPhase(CustomerPhase phase)
        {
            CurrentPhase = phase;
            switch (phase)
            {
                case CustomerPhase.Llegada:
                    _phaseTimer.Start(3f);
                    break;
                case CustomerPhase.EsperaPedido:
                    _phaseTimer.Start(Math.Max(0f, Patience));
                    break;
                case CustomerPhase.EsperandoEntrega:
                    // placeholder temporal; cambiar cuando tengamos la condición real
                    _phaseTimer.Start(3f);
                    break;
                case CustomerPhase.Consumir:
                    _phaseTimer.Start(5f);
                    break;
                case CustomerPhase.Irse:
                    _phaseTimer.Start(3f);
                    break;
                default:
                    _phaseTimer.Stop();
                    break;
            }
        }

        // Actualiza la fase; deltaTime en segundos.
        public void UpdatePhase(float deltaTime)
        {
            if (_phaseTimer.Update(deltaTime))
            {
                // Transiciones por defecto
                switch (CurrentPhase)
                {
                    case CustomerPhase.Llegada:
                        StartPhase(CustomerPhase.EsperaPedido);
                        break;
                    case CustomerPhase.EsperaPedido:
                        // Si se agota la paciencia -> irse
                        StartPhase(CustomerPhase.Irse);
                        break;
                    case CustomerPhase.EsperandoEntrega:
                        // Tras esperar entrega, consumir
                        StartPhase(CustomerPhase.Consumir);
                        break;
                    case CustomerPhase.Consumir:
                        StartPhase(CustomerPhase.Irse);
                        break;
                    case CustomerPhase.Irse:
                        // Cliente ha salido; quedaría para destruir/reciclar por un manager
                        break;
                    default:
                        StartPhase(CustomerPhase.Irse);
                        break;
                }
            }
        }
    }
}

