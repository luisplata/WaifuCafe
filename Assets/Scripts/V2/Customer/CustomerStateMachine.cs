using System;
using Customers;
using UnityEngine;
using V2.Customer;
using V2.Food;

public class CustomerStateMachine : MonoBehaviour
{
    public Action<CustomerPhase> OnStateChange;
    public Action<float> OnLoadingState;
    [SerializeField] private CustomerPhase customerPhase = CustomerPhase.Entrando;
    public CustomerPhase State => customerPhase;
    private float _deltaLocal;
    private CustomerClientModel _customerData;
    private ICustomerClient _customerClient;
    private FoodModel _foodByRandom;

    public void Configure(ICustomerClient customerClient, CustomerClientModel customerData, FoodModel foodByRandom)
    {
        _customerData = customerData;
        _customerClient = customerClient;
        _foodByRandom = foodByRandom;
    }

    public void SetState(CustomerPhase newState)
    {
        customerPhase = newState;
        OnStateChange?.Invoke(newState);
    }

    private void Update()
    {
        switch (customerPhase)
        {
            case CustomerPhase.Entrando:
                break;
            case CustomerPhase.ListoParaPedir:
                _deltaLocal += Time.deltaTime;
                OnLoadingState?.Invoke(_deltaLocal / _customerData.paciencia);
                if (_deltaLocal >= _customerData.paciencia)
                {
                    SetState(CustomerPhase.Irse);
                    _deltaLocal = 0f;
                }

                break;
            case CustomerPhase.EsperandoEntrega:
                break;
            case CustomerPhase.EntregandoPedido:
                break;
            case CustomerPhase.Consumir:
                _deltaLocal += Time.deltaTime;
                OnLoadingState?.Invoke(_deltaLocal / _customerData.tiempoDeConsumo +
                                       _foodByRandom.tiempoDeConsumo);
                if (_deltaLocal >= _customerData.tiempoDeConsumo + _foodByRandom.tiempoDeConsumo)
                {
                    SetState(CustomerPhase.Irse);
                    _deltaLocal = 0f;
                }

                break;
            case CustomerPhase.Irse:
                _deltaLocal += Time.deltaTime;
                OnLoadingState?.Invoke(_deltaLocal / 2f);
                if (_deltaLocal >= 2)
                {
                    _customerClient.Irse();
                    _deltaLocal = 0f;
                }

                break;
            case CustomerPhase.Llendose:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void ListoParaPedir()
    {
    }

    public void Esperando()
    {
    }
}