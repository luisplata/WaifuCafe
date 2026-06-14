using V2.Food;

namespace Staff
{
    public interface IStaffMediator
    {
        void GoToKichen(float tiempoDeMoverseEntreCocinaCliente);
        void GoToClient(float tiempoDeMoverseEntreCocinaCliente);
        void CustomerToWaitPedido();
        void Consumiendo();
        void IrPuesto();
        float GetTimeToPrepare(float timeDePreparacionDeComida, FoodModel food);
    }
}