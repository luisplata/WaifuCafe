using UnityEngine;
using StateMachines;

namespace V2.Staff
{
    [RequireComponent(typeof(StaffStateMachine))]
    public class StaffAnimationBridge : MonoBehaviour
    {
        private StaffStateMachine _stateMachine;
        private Animator _animator;

        private void Awake()
        {
            _stateMachine = GetComponent<StaffStateMachine>();
            
            // Try to find Animator in children (e.g. on the 'waifu' child)
            _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null)
            {
                // Fallback: search on root
                _animator = GetComponent<Animator>();
            }
        }

        private void OnEnable()
        {
            if (_stateMachine != null)
            {
                _stateMachine.OnStateChange += HandleStateChange;
            }
        }

        private void OnDisable()
        {
            if (_stateMachine != null)
            {
                _stateMachine.OnStateChange -= HandleStateChange;
            }
        }

        private void Start()
        {
            if (_stateMachine != null)
            {
                HandleStateChange(_stateMachine.CanUse() ? StaffPhase.EnEspera : StaffPhase.Entrando);
            }
        }

        private void HandleStateChange(StaffPhase newState)
        {
            if (_animator == null) return;

            bool isMoving = newState == StaffPhase.Entrando || 
                            newState == StaffPhase.LlevandoPedidoCocina || 
                            newState == StaffPhase.LlevandoPedidoCliente || 
                            newState == StaffPhase.Moviendose;

            _animator.SetFloat("Speed", isMoving ? 1f : 0f);
        }
    }
}
