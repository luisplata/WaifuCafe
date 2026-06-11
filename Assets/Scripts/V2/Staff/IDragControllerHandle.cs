namespace V2.Staff
{
    public interface IDragControllerHandle
    {
        bool CanUse();
        void PedirPedido(float customerDataTiempoDeEntregaDePedido, ICustomerClient customerClient);
    }
}