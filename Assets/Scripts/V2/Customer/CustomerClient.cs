using System;
using Customers;
using DragAndDrop;
using PrimeTween;
using UnityEngine;
using V2.Customer;
using V2.Food;
using V2.Staff;

[RequireComponent(typeof(Collider2D))]
public class CustomerClient : MonoBehaviour, IDropReceiver, ICustomerClient
{
    public Action<CustomerClientModel, FoodModel> OnCustomerAttended;
    public Action OnLeftGo;
    [SerializeField] private CustomerFoodClient foodClient;
    [SerializeField] private CustomerClientModel customerData;
    [SerializeField] private CustomerStateMachine stateMachine;
    private CustomerSeat _seat;
    private GameObject _pointToSpawn;
    private bool isAttended = false;
    private FoodModel _foodByRandom;
    private StaffModel _staffModel;
    private ICustomerSpawn _customerSpawnerCoroutine;

    public CustomerClientModel CustomerData => customerData;

    public void Configure(CustomerSeat seat, GameObject pointToSpawn, FoodModel foodByRandom,
        float modificadorDePaciencia, ICustomerSpawn customerSpawnerCoroutine)
    {
        _customerSpawnerCoroutine = customerSpawnerCoroutine;
        _foodByRandom = foodByRandom;
        foodClient.Configure(foodByRandom);
        _seat = seat;
        customerData.paciencia *= 1 + modificadorDePaciencia;
        Tween.Position(transform, seat.transform.position, customerData.moveToSeatTime).OnComplete(() =>
        {
            stateMachine.SetState(CustomerPhase.ListoParaPedir);
            stateMachine.ListoParaPedir();
            foodClient.Show();
        });
        stateMachine.Configure(this, customerData, _foodByRandom);
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
            payload.origin.GetDragControllerHandle()
                .PedirPedido(customerData.tiempoDeEntregaDePedido, this, _foodByRandom);
            foodClient.Hide();
            _staffModel = payload.origin.GetStaffModel();
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
        isAttended = true;
        stateMachine.SetState(CustomerPhase.Consumir);
    }

    public void Irse()
    {
        foodClient.Hide();
        _seat.Release();


        if (_staffModel is { StaffEspeciality: StaffEspeciality.VipSpecialist
                or StaffEspeciality.CasualSpecialist
                or StaffEspeciality.RushSpecialist
            }
           )
        {
            customerData.pointsToAttend *= 1 + _staffModel.GetIncrementOfPoints();
        }

        if (_customerSpawnerCoroutine.IsEconomyModify())
        {
            customerData.pointsToAttend *= 1 + _customerSpawnerCoroutine.GetGameRules().GetAlteredEconomy();
            _foodByRandom.pointsToAttend *= 1 + _customerSpawnerCoroutine.GetGameRules().GetAlteredEconomy();
        }

        if (isAttended)
        {
            OnCustomerAttended?.Invoke(customerData, _foodByRandom);
        }

        Tween.Position(transform, _pointToSpawn.transform.position, customerData.moveToSeatTime).OnComplete(() =>
        {
            Destroy(gameObject, 2);
        });

        stateMachine.SetState(CustomerPhase.Llendose);
        OnLeftGo?.Invoke();
    }
}