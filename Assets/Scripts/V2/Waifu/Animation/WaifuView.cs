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

        private Vector3 _bodyBaseScale = new Vector3(2.6f, 3.4f, 1f);
        private bool _bodyUsesArt;

        private void Awake()
        {
            if (bridge == null) bridge = GetComponent<WaifuAnimationBridge>();
            if (body == null) body = FindChild<SpriteRenderer>("Body");
            if (flash == null) flash = FindChild<SpriteRenderer>("Flash");
            if (nameLabel == null) nameLabel = FindChild<TextMesh>("Name");
            if (body != null) _bodyBaseScale = body.transform.localScale;
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

        /// <summary>
        /// Ajusta identidad y color de elemento (la cinematica la llama antes de Activate).
        /// Si recibe arte, reemplaza el rectangulo por el chibi transparente conservando
        /// la proporcion dentro de la caja de diseno (2.6 x 3.4).
        /// </summary>
        public void ApplyIdentity(string name, Dish dishElement, Color color, Sprite art = null)
        {
            displayName = name;
            element = dishElement;
            elementColor = color;

            if (art != null && body != null)
            {
                _bodyUsesArt = true;
                body.sprite = art;
                body.color = Color.white;
                Vector2 size = art.bounds.size;
                float scale = Mathf.Min(2.6f / Mathf.Max(0.001f, size.x),
                                        3.4f / Mathf.Max(0.001f, size.y));
                body.transform.localScale = Vector3.one * scale;
                if (nameLabel != null) nameLabel.text = displayName;
                if (flash != null)
                {
                    Color glow = elementColor;
                    glow.a = 0f;
                    flash.color = glow;
                }
                return;
            }

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
                // El arte manda mientras esta activo. Al volver al placeholder se restaura
                // el rectangulo, sin pisar un sprite propio que venga del prefab.
                if (generateBodyIfMissing && (_bodyUsesArt || body.sprite == null))
                    body.sprite = DishSprites.RoundRect();
                _bodyUsesArt = false;
                body.color = elementColor;
                body.transform.localScale = _bodyBaseScale;
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
