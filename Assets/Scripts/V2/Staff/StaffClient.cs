using DragAndDrop;
using Staff;
using StateMachines;
using UnityEngine;
using V2.Audio;
using V2.Food;

namespace V2.Staff
{
    public class StaffClient : MonoBehaviour, IDragControllerHandle, IStaffMediator
    {
        [SerializeField] private StaffStateMachine stateMachine;
        [SerializeField] private DraggableSprite draggableSprite;
        [SerializeField] private SpriteRenderer staffRenderer;
        [SerializeField] private AudioClip goToKichen, goToClient;
        private StaffModel _staff;
        private GameObject _spawnPosition;
        private ICustomerClient _customerClient;
        private StaffPosition _staffPosition;

        public void Configure(StaffPosition staffPosition, GameObject spawnPosition, StaffModel staffreference)
        {
            _staff = staffreference;
            _staffPosition = staffPosition;
            _spawnPosition = spawnPosition;
            stateMachine.Configure(this);
            PrimeTween.Tween.Position(transform, staffPosition.transform.position, _staff.TimeToMove()).OnComplete(() =>
            {
                stateMachine.SetState(StaffPhase.EnEspera);
                stateMachine.Disponible();
            });
            draggableSprite.Configure(this);
            draggableSprite.OnEnterDrag += OnEnterDrag;
            draggableSprite.OnFinishDrag += OnFinishDrag;
            draggableSprite.OnDragging += OnDragging;
            staffRenderer.sprite = _staff.Sprite;
        }

        private void OnFinishDrag(IDropReceiver obj)
        {
            if (obj != null)
            {
                Debug.Log($"StaffClient: recibí drop desde {gameObject.name} sobre {obj.GetGameObject().name}");
            }
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

        public void PedirPedido(float customerDataTiempoDeEntregaDePedido, ICustomerClient customerClient,
            FoodModel foodModel)
        {
            _customerClient = customerClient;
            stateMachine.PedirPedido(customerDataTiempoDeEntregaDePedido, foodModel);
        }

        public StaffModel GetStaffModel()
        {
            return _staff;
        }

        public void GoToKichen(float tiempoDeMoverseEntreCocinaCliente)
        {
            AudioService.Instance.StartSfx(goToKichen);
            PrimeTween.Tween.Position(transform, _spawnPosition.transform.position, tiempoDeMoverseEntreCocinaCliente);
        }

        public void GoToClient(float tiempoDeMoverseEntreCocinaCliente)
        {
            AudioService.Instance.StartSfx(goToClient);
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
            PrimeTween.Tween.Position(transform, _staffPosition.transform.position, _staff.TimeToMove());
        }

        public float GetTimeToPrepare(float timeDePreparacionDeComida, FoodModel food)
        {
            return timeDePreparacionDeComida * 1 - _staff.ModificadorDeTiempoDePreparacion(food);
        }

        public StaffModel GetModel()
        {
            return _staff;
        }
    }
}