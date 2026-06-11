using CustomerPhase = Customers.CustomerPhase;

namespace StateMachines
{
    // Fases del personal (staff)
    public enum StaffPhase
    {
        Entrando,
        EnEspera, // Espera a que se le asigne atender
        AtendiendoCliente, // Toma el pedido (2s)
        PreparandoPedido, // Cocina/prepara (ServiceTime)
        LlevandoPedidoCocina, // Lleva el pedido al cliente (2s)
        LlevandoPedidoCliente, // Lleva el pedido al cliente (2s)
        Moviendose
    }
}