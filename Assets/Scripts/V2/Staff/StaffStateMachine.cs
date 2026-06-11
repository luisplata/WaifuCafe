using System;
using PrimeTween;
using Staff;
using StateMachines;
using UnityEngine;

namespace V2.Staff
{
    public class StaffStateMachine : MonoBehaviour
    {
        public Action<StaffPhase> OnStateChange;
        public Action<float> OnLoadingState;
        [SerializeField] private StaffPhase staffPhase = StaffPhase.Entrando;
        [SerializeField] private float timeDePreparacionDeComida;
        [SerializeField] private float tiempoDeMoverseEntreCocinaCliente;
        [SerializeField] private SpriteRenderer _renderer;
        private float _localDelta;
        private IStaffMediator _staffClient;
        private float _customerDataTiempoDeEntregaDePedido;

        public bool CanUse()
        {
            return staffPhase == StaffPhase.EnEspera;
        }

        public void SetState(StaffPhase newState)
        {
            staffPhase = newState;
            OnStateChange?.Invoke(newState);
        }

        private void Update()
        {
            switch (staffPhase)
            {
                case StaffPhase.Moviendose:
                    _localDelta += Time.deltaTime;
                    OnLoadingState?.Invoke(_localDelta / tiempoDeMoverseEntreCocinaCliente);
                    if (_localDelta >= tiempoDeMoverseEntreCocinaCliente)
                    {
                        _localDelta = 0f;
                        SetState(StaffPhase.EnEspera);
                        _renderer.color = Color.white;
                    }

                    break;
                case StaffPhase.Entrando:
                    break;
                case StaffPhase.EnEspera:
                    break;
                case StaffPhase.AtendiendoCliente:
                    _localDelta += Time.deltaTime;
                    OnLoadingState?.Invoke(_localDelta / _customerDataTiempoDeEntregaDePedido);
                    if (_localDelta >= _customerDataTiempoDeEntregaDePedido)
                    {
                        _localDelta = 0f;
                        SetState(StaffPhase.LlevandoPedidoCocina);
                        _staffClient.GoToKichen(tiempoDeMoverseEntreCocinaCliente);
                        _staffClient.CustomerToWaitPedido();
                    }

                    break;
                case StaffPhase.PreparandoPedido:
                    _localDelta += Time.deltaTime;
                    OnLoadingState?.Invoke(_localDelta / timeDePreparacionDeComida);
                    if (_localDelta >= timeDePreparacionDeComida)
                    {
                        _localDelta = 0f;
                        SetState(StaffPhase.LlevandoPedidoCliente);
                        _staffClient.GoToClient(tiempoDeMoverseEntreCocinaCliente);
                    }

                    break;
                case StaffPhase.LlevandoPedidoCocina:
                    _localDelta += Time.deltaTime;
                    OnLoadingState?.Invoke(_localDelta / tiempoDeMoverseEntreCocinaCliente);
                    if (_localDelta >= tiempoDeMoverseEntreCocinaCliente)
                    {
                        _localDelta = 0f;
                        SetState(StaffPhase.PreparandoPedido);
                    }

                    break;

                case StaffPhase.LlevandoPedidoCliente:
                    _localDelta += Time.deltaTime;
                    OnLoadingState?.Invoke(_localDelta / tiempoDeMoverseEntreCocinaCliente);
                    if (_localDelta >= tiempoDeMoverseEntreCocinaCliente)
                    {
                        _localDelta = 0f;
                        SetState(StaffPhase.Moviendose);
                        _staffClient.IrPuesto();
                        _staffClient.Consumiendo();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void PedirPedido(float customerDataTiempoDeEntregaDePedido)
        {
            _customerDataTiempoDeEntregaDePedido = customerDataTiempoDeEntregaDePedido;
            _renderer.color = Color.brown;
        }

        public void Configure(IStaffMediator staffClient)
        {
            _staffClient = staffClient;
        }

        public void Disponible()
        {
            _renderer.color = Color.white;
        }
    }
}