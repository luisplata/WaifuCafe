using DragAndDrop;
using StateMachines;
using UnityEngine;
using UnityEngine.EventSystems;

namespace V2.Staff
{
    public class StaffClient : MonoBehaviour, IDragControllerHandle
    {
        [SerializeField] private StaffModel staff;
        [SerializeField] private StaffStateMachine stateMachine;
        [SerializeField] private DraggableSprite draggableSprite;

        public void Configure(StaffPosition staffPosition)
        {
            PrimeTween.Tween.Position(transform, staffPosition.transform.position, staff.timeToIntro).OnComplete(() =>
            {
                stateMachine.SetState(StaffPhase.EnEspera);
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
    }
}