namespace StateMachines
{
    // Fases que atraviesa un cliente
    public enum CustomerPhase
    {
        Llegada,        // Entrada / llegada (por ejemplo 3s)
        EsperaPedido,   // Paciencia esperando que el staff tome el pedido
        EsperandoEntrega, // Pedido preparado y esperando entrega
        Consumir,       // Comer/tomar la entrega (por ejemplo 5s)
        Irse            // Salida (por ejemplo 3s)
    }

    // Fases del personal (staff)
    public enum StaffPhase
    {
        EnEspera,           // Espera a que se le asigne atender
        AtendiendoCliente,   // Toma el pedido (2s)
        PreparandoPedido,    // Cocina/prepara (ServiceTime)
        LlevandoPedido       // Lleva el pedido al cliente (2s)
    }

    // Estados generales del juego
    public enum GameState
    {
        Preparacion,
        Inicio,
        Pausa,
        Reanudar,
        EventoDeLaRun,
        ActivacionDeActiva,
        GameOver
    }
}

