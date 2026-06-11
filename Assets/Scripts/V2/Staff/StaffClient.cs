using DragAndDrop;
using Staff;
using StateMachines;
using UnityEngine;

namespace V2.Staff
{
    public class StaffClient : MonoBehaviour, IDragControllerHandle, IStaffMediator
    {
        [SerializeField] private StaffModel staff;
        [SerializeField] private StaffStateMachine stateMachine;
        [SerializeField] private DraggableSprite draggableSprite;
        private GameObject _spawnPosition;
        private ICustomerClient _customerClient;
        private StaffPosition _staffPosition;

        public void Configure(StaffPosition staffPosition, GameObject spawnPosition)
        {
            _staffPosition = staffPosition;
            _spawnPosition = spawnPosition;
            stateMachine.Configure(this);
            PrimeTween.Tween.Position(transform, staffPosition.transform.position, staff.timeToIntro).OnComplete(() =>
            {
                stateMachine.SetState(StaffPhase.EnEspera);
                stateMachine.Disponible();
            });
            draggableSprite.Configure(this);
            draggableSprite.OnEnterDrag += OnEnterDrag;
            draggableSprite.OnFinishDrag += OnFinishDrag;
            draggableSprite.OnDragging += OnDragging;
        }

        private void OnFinishDrag(IDropReceiver obj)
        {
            Debug.Log($"StaffClient: recibí drop desde {gameObject.name} sobre {obj.GetGameObject().name}");
        }

        private void OnDragging()
        {
        }

        private void OnEnterDrag()
        {
        }

        public bool CanUse()
        {
            return stateMachine.CanUse();
        }

        public void PedirPedido(float customerDataTiempoDeEntregaDePedido, ICustomerClient customerClient)
        {
            _customerClient = customerClient;
            stateMachine.PedirPedido(customerDataTiempoDeEntregaDePedido);
            stateMachine.SetState(StaffPhase.AtendiendoCliente);
        }

        public void GoToKichen(float tiempoDeMoverseEntreCocinaCliente)
        {
            PrimeTween.Tween.Position(transform, _spawnPosition.transform.position, tiempoDeMoverseEntreCocinaCliente);
        }

        public void GoToClient(float tiempoDeMoverseEntreCocinaCliente)
        {
            PrimeTween.Tween.Position(transform, _customerClient.GetGameObject().transform.position,
                tiempoDeMoverseEntreCocinaCliente);
        }

        public void CustomerToWaitPedido()
        {
            _customerClient.EsperandoPedido();
        }

        public void Consumiendo()
        {
            _customerClient.Consumiendo();
        }

        public void IrPuesto()
        {
            PrimeTween.Tween.Position(transform, _staffPosition.transform.position, staff.timeToIntro);
        }
    }
}