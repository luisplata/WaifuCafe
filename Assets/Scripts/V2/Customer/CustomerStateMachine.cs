using System;
using Customers;
using UnityEngine;
using V2.Customer;

public class CustomerStateMachine : MonoBehaviour
{
    [SerializeField] private CustomerPhase customerPhase = CustomerPhase.Entrando;
    public CustomerPhase State => customerPhase;
    private float _tiempoDePaciencia;
    private CustomerClientModel _customerData;
    private ICustomerClient _customerClient;

    public void Configure(ICustomerClient customerClient, CustomerClientModel customerData)
    {
        _customerData = customerData;
        _customerClient = customerClient;
    }

    public void SetState(CustomerPhase newState)
    {
        customerPhase = newState;
    }

    private void Update()
    {
        switch (customerPhase)
        {
            case CustomerPhase.Entrando:
                break;
            case CustomerPhase.ListoParaPedir:
                _tiempoDePaciencia += Time.deltaTime;
                if (_tiempoDePaciencia >= _customerData.paciencia)
                {
                    SetState(CustomerPhase.Irse);
                    _tiempoDePaciencia = 0f;
                }

                break;
            case CustomerPhase.EsperandoEntrega:
                break;
            case CustomerPhase.EntregandoPedido:
                break;
            case CustomerPhase.Consumir:
                break;
            case CustomerPhase.Irse:
                _customerClient.Irse();
                break;
            case CustomerPhase.Llendose:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}