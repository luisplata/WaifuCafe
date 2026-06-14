using System;

namespace V2.Customer
{
    [Serializable]
    public class CustomerClientModel
    {
        public float moveToSeatTime;
        public float paciencia;
        public float tiempoDeEntregaDePedido;
        public float tiempoDeConsumo;
        public CustomerIdentify customerIdentify;
        public float pointsToAttend;
    }
}