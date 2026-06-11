using Customers;
using DragAndDrop;
using PrimeTween;
using UnityEngine;
using V2.Customer;

[RequireComponent(typeof(Collider2D))]
public class CustomerClient : MonoBehaviour, IDropReceiver, ICustomerClient
{
    [SerializeField] private CustomerClientModel customerData;
    [SerializeField] private CustomerStateMachine stateMachine;
    private CustomerSeat _seat;
    private GameObject _pointToSpawn;

    public void Configure(CustomerSeat seat, GameObject pointToSpawn)
    {
        _seat = seat;
        Tween.Position(transform, seat.transform.position, customerData.moveToSeatTime).OnComplete(() =>
        {
            stateMachine.SetState(CustomerPhase.ListoParaPedir);
            stateMachine.ListoParaPedir();
        });
        stateMachine.Configure(this, customerData);
        _pointToSpawn = pointToSpawn;
        _seat.Hold();
    }

    public void OnDrop(DropPayload payload)
    {
        if (payload.originType == OriginType.Sprite && payload.origin != null &&
            stateMachine.State == CustomerPhase.ListoParaPedir)
        {
            Debug.Log(
                $"DropTargetSprite: recibí drop desde {payload.origin.GetGameObject().name} sobre {gameObject.name}");
            payload.origin.GetGameObject().transform.position = transform.position;
            stateMachine.SetState(CustomerPhase.EntregandoPedido);
            stateMachine.Esperando();
            payload.origin.GetDragControllerHandle().PedirPedido(customerData.tiempoDeEntregaDePedido, this);
        }
    }

    public bool Accepts(DropPayload payload)
    {
        return stateMachine.State == CustomerPhase.ListoParaPedir;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public void EsperandoPedido()
    {
        stateMachine.SetState(CustomerPhase.EsperandoEntrega);
        stateMachine.Esperando();
    }

    public void Consumiendo()
    {
        stateMachine.SetState(CustomerPhase.Consumir);
    }

    public void Irse()
    {
        _seat.Release();
        Tween.Position(transform, _pointToSpawn.transform.position, customerData.moveToSeatTime).OnComplete(() =>
        {
            Destroy(gameObject, 2);
        });
        stateMachine.SetState(CustomerPhase.Llendose);
    }
}