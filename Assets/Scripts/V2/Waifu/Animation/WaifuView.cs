using UnityEngine;
using V2.Board;
using WaifuCafe.Core;

namespace V2.Waifu.Animation
{
    /// <summary>
    /// Fachada visual de una waifu: el gameplay solo llama Idle/React/Cast/Activate/
    /// SetCooldown. El Animator y sus parametros quedan detras de WaifuAnimationBridge.
    /// El cuerpo, el flash y el nombre son placeholders procedurales hasta que llegue el arte.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(WaifuAnimationBridge))]
    public sealed class WaifuView : MonoBehaviour
    {
        [Header("Placeholder (sin arte real)")]
        [SerializeField] private string displayName = "Rika";
        [SerializeField] private Dish element = Dish.Bebida;
        [SerializeField] private Color elementColor = new Color(0.36f, 0.66f, 1f, 1f);
        [SerializeField] private bool generateBodyIfMissing = true;

        [Header("Referencias")]
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer flash;
        [SerializeField] private TextMesh nameLabel;
        [SerializeField] private WaifuAnimationBridge bridge;

        public string DisplayName => displayName;
        public Dish Element => element;
        public bool IsCooldown { get; private set; }

        private void Awake()
        {
            if (bridge == null) bridge = GetComponent<WaifuAnimationBridge>();
            if (body == null) body = FindChild<SpriteRenderer>("Body");
            if (flash == null) flash = FindChild<SpriteRenderer>("Flash");
            if (nameLabel == null) nameLabel = FindChild<TextMesh>("Name");
            ApplyPlaceholder();
        }

        public void Idle()
        {
            IsCooldown = false;
            if (bridge != null) bridge.Idle();
        }

        public void React() { if (bridge != null) bridge.React(); }
        public void Cast() { if (bridge != null) bridge.Cast(); }
        public void Activate() { if (bridge != null) bridge.Activate(); }

        /// <summary>
        /// Dispara un one-shot por nombre de trigger: cada modo de presentacion aporta el
        /// suyo (grande = Activate, flash de READY = Cast) sin atarse a los nombres.
        /// </summary>
        public void PlayClip(string trigger)
        {
            if (bridge != null) bridge.PlayTrigger(trigger);
        }

        /// <summary>Ajusta identidad y color de elemento (la cinematica la llama antes de Activate).</summary>
        public void ApplyIdentity(string name, Dish dishElement, Color color)
        {
            displayName = name;
            element = dishElement;
            elementColor = color;
            ApplyPlaceholder();
        }

        public void SetCooldown(bool on)
        {
            IsCooldown = on;
            if (bridge != null) bridge.SetCooldown(on);
        }

        private void ApplyPlaceholder()
        {
            if (body != null)
            {
                if (body.sprite == null && generateBodyIfMissing) body.sprite = DishSprites.RoundRect();
                body.color = elementColor;
            }

            if (flash != null)
            {
                if (flash.sprite == null && generateBodyIfMissing) flash.sprite = DishSprites.RoundRect();
                Color glow = elementColor;
                glow.a = 0f;
                flash.color = glow;
            }

            if (nameLabel != null) nameLabel.text = displayName;
        }

        private T FindChild<T>(string childName) where T : Component
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }
    }
}
