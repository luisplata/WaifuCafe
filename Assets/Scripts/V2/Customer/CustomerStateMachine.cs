using System;
using Customers;
using UnityEngine;
using V2.Customer;

public class CustomerStateMachine : MonoBehaviour
{
    public Action<CustomerPhase> OnStateChange;
    public Action<float> OnLoadingState;
    [SerializeField] private CustomerPhase customerPhase = CustomerPhase.Entrando;
    public CustomerPhase State => customerPhase;
    private float _tiempoDePaciencia;
    private CustomerClientModel _customerData;
    private ICustomerClient _customerClient;
    [SerializeField] private SpriteRenderer _renderer;

    public void Configure(ICustomerClient customerClient, CustomerClientModel customerData)
    {
        _customerData = customerData;
        _customerClient = customerClient;
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
                _tiempoDePaciencia += Time.deltaTime;
                OnLoadingState?.Invoke(_tiempoDePaciencia / _customerData.paciencia);
                if (_tiempoDePaciencia >= _customerData.paciencia)
                {
                    SetState(CustomerPhase.Irse);
                    _tiempoDePaciencia = 0f;
                    _renderer.color = Color.red;
                }

                break;
            case CustomerPhase.EsperandoEntrega:
                break;
            case CustomerPhase.EntregandoPedido:
                break;
            case CustomerPhase.Consumir:
                _tiempoDePaciencia += Time.deltaTime;
                OnLoadingState?.Invoke(_tiempoDePaciencia / _customerData.tiempoDeConsumo);
                if (_tiempoDePaciencia >= _customerData.tiempoDeConsumo)
                {
                    SetState(CustomerPhase.Irse);
                    _tiempoDePaciencia = 0f;
                    _renderer.color = Color.blue;
                }

                break;
            case CustomerPhase.Irse:
                _tiempoDePaciencia += Time.deltaTime;
                OnLoadingState?.Invoke(_tiempoDePaciencia / 2f);
                if (_tiempoDePaciencia >= 2)
                {
                    _customerClient.Irse();
                    _tiempoDePaciencia = 0f;
                    _renderer.color = Color.white;
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
        _renderer.color = Color.green;
    }

    public void Esperando()
    {
        _renderer.color = Color.yellow;
    }
}