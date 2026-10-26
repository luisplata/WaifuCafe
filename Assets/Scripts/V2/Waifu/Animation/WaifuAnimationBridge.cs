using UnityEngine;

namespace V2.Waifu.Animation
{
    /// <summary>
    /// Unico puente entre las intenciones de gameplay y el Animator de una waifu.
    /// Si falta el Animator o el controller no declara un parametro, la llamada se
    /// ignora: el gameplay nunca recibe una excepcion ni un warning del Animator.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaifuAnimationBridge : MonoBehaviour
    {
        [Header("Referencia")]
        [SerializeField] private Animator animator;

        [Header("Parametros del controller (nombres exactos)")]
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string reactTrigger = "React";
        [SerializeField] private string castTrigger = "Cast";
        [SerializeField] private string activateTrigger = "Activate";
        [SerializeField] private string cooldownBool = "Cooldown";

        private bool _hasIdleState;
        private int _idleHash;
        private bool _hasReact;
        private bool _hasCast;
        private bool _hasActivate;
        private bool _hasCooldown;

        public bool IsReady => animator != null && animator.runtimeAnimatorController != null;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            CacheParameters();
        }

        /// <summary>Vuelve al loop Idle. Si un one-shot sigue vivo, lo corta en el acto.</summary>
        public void Idle()
        {
            SetCooldown(false);
            if (!IsReady) return;

            if (_hasReact) animator.ResetTrigger(reactTrigger);
            if (_hasCast) animator.ResetTrigger(castTrigger);
            if (_hasActivate) animator.ResetTrigger(activateTrigger);

            if (!_hasIdleState) return;
            if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == _idleHash) return;
            animator.Play(_idleHash, 0, 0f);
        }

        public void React() => Fire(reactTrigger, _hasReact);
        public void Cast() => Fire(castTrigger, _hasCast);
        public void Activate() => Fire(activateTrigger, _hasActivate);

        /// <summary>
        /// Trigger por nombre configurable. Si el controller no lo declara, se ignora:
        /// el mismo contrato tolerante que el resto del bridge.
        /// </summary>
        public void PlayTrigger(string trigger)
        {
            if (!IsReady || string.IsNullOrEmpty(trigger) || !DeclaresTrigger(trigger)) return;
            animator.ResetTrigger(trigger);
            animator.SetTrigger(trigger);
        }

        private bool DeclaresTrigger(string trigger)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger &&
                    parameter.name == trigger) return true;
            return false;
        }

        public void SetCooldown(bool on)
        {
            if (!_hasCooldown) return;
            animator.SetBool(cooldownBool, on);
        }

        private void Fire(string trigger, bool declared)
        {
            if (!IsReady || !declared) return;
            animator.ResetTrigger(trigger);
            animator.SetTrigger(trigger);
        }

        private void CacheParameters()
        {
            _hasIdleState = _hasReact = _hasCast = _hasActivate = _hasCooldown = false;
            if (!IsReady) return;

            _idleHash = Animator.StringToHash(idleState);
            _hasIdleState = animator.HasState(0, _idleHash);

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    _hasReact |= parameter.name == reactTrigger;
                    _hasCast |= parameter.name == castTrigger;
                    _hasActivate |= parameter.name == activateTrigger;
                }
                else if (parameter.type == AnimatorControllerParameterType.Bool &&
                         parameter.name == cooldownBool)
                {
                    _hasCooldown = true;
                }
            }
        }
    }
}
