namespace Customers
{
    // Fases que atraviesa un cliente
    public enum CustomerPhase
    {
        Entrando,
        ListoParaPedir, // Entrada / llegada (por ejemplo 3s)
        EntregandoPedido, // Paciencia esperando que el staff tome el pedido
        EsperandoEntrega, // Pedido preparado y esperando entrega
        Consumir, // Comer/tomar la entrega (por ejemplo 5s)
        Irse, // Salida (por ejemplo 3s)
        Llendose
    }
}