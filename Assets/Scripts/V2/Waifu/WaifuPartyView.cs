using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using V2.Board;
using V2.Helpers;
using WaifuCafe.Core;

namespace V2.Waifu
{
    /// <summary>
    /// Cartas reales de la party en la columna del cafe: retrato procedural, nombre, rol,
    /// barra de carga propia de cada waifu y estado READY. Solo lee el runtime y la
    /// resolucion del tablero; no ejecuta skills ni decide reglas. El A/B de lado se
    /// resuelve reconstruyendo las cartas espejadas.
    /// </summary>
    public sealed class WaifuPartyView : MonoBehaviour
    {
        [Header("Party (4 slots, editable en inspector)")]
        [SerializeField] private WaifuDefinitionSO[] party = new WaifuDefinitionSO[WaifuPartyRuntime.MaxSlots];

        [Header("Layout de la columna (unidades de mundo)")]
        [SerializeField] private float columnCenterX = 6.7f;
        [SerializeField] private float cardY = -5f;
        [SerializeField] private float cardWidth = 3f;
        [SerializeField] private float cardHeight = 4f;
        [SerializeField] private float cardGap = 0.25f;

        [Header("Animacion")]
        [SerializeField] private float chargePulse = 1.06f;
        [SerializeField] private float chargeTime = 0.18f;
        [SerializeField] private float readyScale = 1.08f;
        [SerializeField] private float readyTime = 0.34f;

        private const float TextHeightPerUnit = 3.5f;
        private const float BarWidth = 2.3f;

        private static readonly Color CardColor = new Color(0.18f, 0.15f, 0.24f, 0.95f);
        private static readonly Color BarColor = new Color(0.06f, 0.05f, 0.08f, 1f);
        private static readonly Color FrameColor = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.86f);
        private static readonly Color MutedColor = new Color(1f, 1f, 1f, 0.62f);
        private static readonly Color ReadyColor = new Color(1f, 0.86f, 0.42f, 1f);
        private static readonly Color CooldownColor = new Color(0.94f, 0.55f, 0.58f, 1f);

        private sealed class Card
        {
            public int Slot;
            public Transform Root;
            public SpriteRenderer Fill;
            public TextMesh Name;
            public TextMesh Role;
            public TextMesh Charge;
            public GameObject ReadyGlow;
            public float BarY = -1.18f;
            public Sequence Pulse;
            public int LastAmount = -1;
            public bool WasReady;
        }

        private readonly System.Collections.Generic.List<Card> _cards =
            new System.Collections.Generic.List<Card>();

        private WaifuPartyRuntime _runtime;
        private SkillExecutor _executor;
        private BoardSession _session;
        private BoardView _board;
        private BoardLayoutMock _layout;
        private WaifuCutsceneDirector _cutscene;
        private Font _font;
        private Transform _root;
        private int _sign = -1;
        private WaifuDefinitionSO[] _overrideParty;

        /// <summary>Estado de carga de la party (lo consume PR7 para disparar skills).</summary>
        public WaifuPartyRuntime Runtime => _runtime;

        /// <summary>Skill ejecutada (slot). El contrato la cuenta para su objetivo.</summary>
        public event Action<int> SkillCast;

        /// <summary>
        /// Party de la run (viene del perfil). Llamar antes de <c>Start</c>; el
        /// RunCoordinator usa orden -150. Null/empty conserva la party del inspector.
        /// </summary>
        public void Configure(WaifuDefinitionSO[] definitions)
        {
            _overrideParty = definitions != null && definitions.Length > 0 ? definitions : null;
            if (_overrideParty == null || _runtime != null) return;

            var slots = new WaifuDefinitionSO[WaifuPartyRuntime.MaxSlots];
            for (int i = 0; i < slots.Length && i < _overrideParty.Length; i++) slots[i] = _overrideParty[i];
            party = slots;
        }

        private void Start()
        {
            _layout = FindFirstObjectByType<BoardLayoutMock>();
            _session = FindFirstObjectByType<BoardSession>();
            _board = FindFirstObjectByType<BoardView>();
            _cutscene = FindFirstObjectByType<WaifuCutsceneDirector>();
            if (_layout != null)
            {
                _sign = _layout.IsRight ? -1 : 1;
                _layout.SideChanged += Rebuild;
            }

            if (_session != null) _session.Delivered += OnDelivered;

            _runtime = new WaifuPartyRuntime(party);
            _executor = new SkillExecutor(_runtime, _session);
            Rebuild();
        }

        private void OnDestroy()
        {
            if (_session != null) _session.Delivered -= OnDelivered;
            if (_layout != null) _layout.SideChanged -= Rebuild;
            StopPulses();
        }

        private void OnDelivered(ResolveResult result)
        {
            if (_runtime == null) return;

            // Solo los matches del jugador cobran carga y descuentan cooldown. Si la
            // resolucion de una skill rechargea la party, la carga gastada se devolveria
            // sola y no seria un costo.
            bool swap = _session != null && _session.LastSource == ResolutionSource.Swap;
            if (swap)
            {
                _runtime.Charge(result);
                if (_executor != null) _executor.TickCooldowns();
            }

            Refresh(true);
        }

        /// <summary>
        /// Click en la carta: valida y compromete el costo, corre la cinematica completa y
        /// ejecuta el efecto UNA sola vez al cerrarla (fin natural o skip). Si no puede,
        /// solo hace el shake corto.
        /// </summary>
        public bool Fire(int slot)
        {
            // Cinematica (o cualquier otro bloqueo global): la carta no dispara.
            if (GameplayLock.IsLocked) return false;

            Card card = CardOf(slot);
            if (_executor == null || !_executor.CanFire(slot) || !_executor.CommitFire(slot))
            {
                PlayDenied(card);
                return false;
            }

            Refresh(false);
            PlayFire(card);
            SkillCast?.Invoke(slot);
            if (_cutscene != null) _cutscene.PlayFire(slot, () => ExecuteFire(slot));
            else ExecuteFire(slot);
            return true;
        }

        /// <summary>Cierra el disparo: aplica el efecto ya cobrado y refresca las cartas.</summary>
        private void ExecuteFire(int slot)
        {
            if (_executor != null) _executor.ExecuteFire(slot);
            Refresh(false);
        }

        private void Rebuild()
        {
            if (_layout != null) _sign = _layout.IsRight ? -1 : 1;
            StopPulses();

            if (_root != null) Destroy(_root.gameObject);
            _cards.Clear();

            _root = new GameObject("WaifuCards").transform;
            _root.SetParent(transform, false);

            for (int i = 0; i < _runtime.SlotCount; i++)
            {
                WaifuDefinitionSO def = _runtime[i];
                if (def != null) _cards.Add(BuildCard(i, def));
            }

            Refresh(false);
        }

        private Card BuildCard(int slot, WaifuDefinitionSO def)
        {
            float x = (columnCenterX - 6.375f + cardWidth * 0.5f + slot * (cardWidth + cardGap)) * _sign;

            var card = new Card { Slot = slot };
            var root = new GameObject("Waifu_" + (slot + 1) + "_" + def.DisplayName).transform;
            root.SetParent(_root, false);
            root.localPosition = new Vector3(x, cardY, 0f);
            card.Root = root;

            // Zona de click de la carta (EventSystem + Physics2DRaycaster, sin Input legado).
            var collider = root.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(cardWidth, cardHeight);
            var click = root.gameObject.AddComponent<CardClick>();
            click.Owner = this;
            click.Slot = slot;

            Block(root, "Card", Vector3.zero, new Vector2(cardWidth, cardHeight), CardColor, 0);

            // Carta ilustrada: el arte legacy ocupa la carta completa y el pie oscuro
            // mantiene legibles nombre, rol y barra. Sin arte, se usa el retrato procedural.
            if (def.CardArt != null) BuildArtCard(root, card, def);
            else BuildFallbackCard(root, card, def);

            return card;
        }

        /// <summary>Carta con ilustracion legacy a sangre completa y pie de datos.</summary>
        private void BuildArtCard(Transform root, Card card, WaifuDefinitionSO def)
        {
            Block(root, "CardArt", Vector3.zero, new Vector2(cardWidth, cardHeight),
                Color.white, 1, def.CardArt);
            Block(root, "CardFrame", Vector3.zero, new Vector2(cardWidth, cardHeight),
                FrameColor, 9, DishSprites.RoundFrame());
            Block(root, "CardFooter", new Vector3(0f, -1.28f, 0f),
                new Vector2(cardWidth, 1.44f), new Color(0.10f, 0.08f, 0.13f, 1f), 4);

            card.ReadyGlow = Block(root, "ReadyGlow", Vector3.zero,
                new Vector2(cardWidth + 0.10f, cardHeight + 0.10f),
                GlowColor(Accent(def)), 15, DishSprites.RoundFrame()).gameObject;
            card.ReadyGlow.SetActive(false);

            card.Name = Label(root, def.DisplayName, -0.74f, 0.34f, Color.white);
            card.Role = Label(root, def.Role.ToString(), -1.06f, 0.22f, MutedColor);
            // Icono del elemento a la derecha del rol: misma forma y color que la ficha
            // del tablero, para que se lea de un vistazo.
            Color elementColor = _board != null ? _board.DishColor(def.Element) : Color.white;
            Block(root, "ElementIcon", new Vector3(1.08f, -1.06f, 0f),
                new Vector2(0.42f, 0.42f), elementColor, 11, DishSprites.For(def.Element));
            BuildCharge(root, card, def, -1.44f, -1.80f);
        }

        /// <summary>Fallback sin arte: panel plano, retrato chibi o forma del elemento.</summary>
        private void BuildFallbackCard(Transform root, Card card, WaifuDefinitionSO def)
        {
            card.ReadyGlow = Block(root, "ReadyGlow", new Vector3(0f, 0.2f, 0f),
                new Vector2(2.5f, 2.5f), GlowColor(Accent(def)), 2, DishSprites.Glow()).gameObject;
            card.ReadyGlow.SetActive(false);

            Sprite portraitSprite = def.ChibiArt != null ? def.ChibiArt : DishSprites.For(def.Element);
            SpriteRenderer portrait = Block(root, "Portrait", new Vector3(0f, 0.2f, 0f),
                new Vector2(1.8f, 1.8f), def.ChibiArt != null ? Color.white : Accent(def),
                3, portraitSprite);
            if (def.ChibiArt != null) FitInside(portrait.transform, def.ChibiArt, 1.8f);
            Block(root, "PortraitFrame", new Vector3(0f, 0.2f, 0f),
                new Vector2(1.94f, 1.94f), FrameColor, 4, DishSprites.RoundFrame());

            card.Name = Label(root, def.DisplayName, 1.70f, 0.34f, Color.white);
            card.Role = Label(root, def.Role + " · " + def.Element, 1.34f, 0.22f, MutedColor);
            BuildCharge(root, card, def, -1.18f, -1.58f);
        }

        private void BuildCharge(Transform root, Card card, WaifuDefinitionSO def,
            float barY, float textY)
        {
            card.BarY = barY;
            Block(root, "BarBg", new Vector3(0f, barY, 0f),
                new Vector2(BarWidth + 0.2f, 0.30f), BarColor, 5);
            card.Fill = Block(root, "BarFill", new Vector3(0f, barY, 0f),
                new Vector2(0.001f, 0.20f), Accent(def), 6);
            Block(root, "BarFrame", new Vector3(0f, barY, 0f),
                new Vector2(BarWidth + 0.24f, 0.34f), FrameColor, 7, DishSprites.RoundFrame());

            card.Charge = Label(root, "0/" + def.ChargeCost, textY, 0.30f, TextColor);
        }

        private void Refresh(bool animate)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                Card card = _cards[i];
                WaifuDefinitionSO def = _runtime[card.Slot];
                if (def == null) continue;

                int amount = _runtime.AmountFor(card.Slot);
                bool ready = _runtime.IsReady(card.Slot);
                int cooldown = _executor != null ? _executor.CooldownLeft(card.Slot) : 0;
                bool available = ready && cooldown <= 0;
                bool gained = card.LastAmount >= 0 && amount > card.LastAmount;
                card.LastAmount = amount;

                if (card.Charge != null)
                {
                    if (cooldown > 0)
                    {
                        card.Charge.text = "CD " + cooldown;
                        card.Charge.color = CooldownColor;
                    }
                    else
                    {
                        card.Charge.text = ready ? "READY" : amount + "/" + def.ChargeCost;
                        card.Charge.color = ready ? ReadyColor : TextColor;
                    }
                }

                ApplyFill(card, def.ChargeCost <= 0 ? 0f : Mathf.Clamp01(amount / (float)def.ChargeCost));
                card.ReadyGlow.SetActive(available);
                // Flash ambiental solo cuando la carta queda realmente disponible (READY + CD).
                if (available && !card.WasReady && _cutscene != null) _cutscene.PlayReady(card.Slot);

                if (animate && gained)
                {
                    card.Pulse.Stop();
                    card.Pulse = Sequence.Create()
                        .Chain(Tween.Scale(card.Root, Vector3.one * chargePulse, chargeTime * 0.4f, Ease.OutQuad))
                        .Chain(Tween.Scale(card.Root, Vector3.one, chargeTime * 0.6f, Ease.OutBack));
                }

                if (available && !card.WasReady)
                {
                    card.Pulse.Stop();
                    card.Pulse = Sequence.Create()
                        .Chain(Tween.Scale(card.Root, Vector3.one * readyScale,
                            readyTime * 0.35f, Ease.OutQuad))
                        .Chain(Tween.Scale(card.Root, Vector3.one,
                            readyTime * 0.65f, Ease.OutBack));
                }

                card.WasReady = available;
            }
        }

        private Card CardOf(int slot)
        {
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i].Slot == slot) return _cards[i];
            return null;
        }

        /// <summary>Skill disparada: la carta escala como feedback.</summary>
        private void PlayFire(Card card)
        {
            if (card == null) return;

            card.Pulse.Stop();
            card.Pulse = Sequence.Create()
                .Chain(Tween.Scale(card.Root, Vector3.one * 1.10f, 0.07f, Ease.OutQuad))
                .Chain(Tween.Scale(card.Root, Vector3.one, 0.2f, Ease.OutBack));
        }

        /// <summary>Skill no disponible: shake corto, sin errores.</summary>
        private void PlayDenied(Card card)
        {
            if (card == null) return;

            Vector3 origin = card.Root.localPosition;
            card.Pulse.Stop();
            card.Pulse = Sequence.Create()
                .Chain(Tween.LocalPosition(card.Root, origin + new Vector3(0.12f * _sign, 0f, 0f),
                    0.05f, Ease.OutQuad))
                .Chain(Tween.LocalPosition(card.Root, origin, 0.12f, Ease.OutBack));
        }

        private void ApplyFill(Card card, float t)
        {
            float width = Mathf.Max(0.001f, BarWidth * t);
            float origin = -_sign * BarWidth * 0.5f;
            card.Fill.transform.localPosition = new Vector3(origin + _sign * width * 0.5f, card.BarY, 0f);
            card.Fill.transform.localScale = new Vector3(width, 0.20f, 1f);
            card.Fill.enabled = t > 0.001f;
        }

        private void StopPulses()
        {
            foreach (Card card in _cards) card.Pulse.Stop();
        }

        private static Color Accent(WaifuDefinitionSO def) =>
            def.PortraitColor.a > 0.01f ? def.PortraitColor : Color.white;

        private static Color GlowColor(Color color)
        {
            color.a = 0.85f;
            return color;
        }

        /// <summary>Escala uniforme para que el sprite entre completo en una caja cuadrada.</summary>
        private static void FitInside(Transform target, Sprite sprite, float maxSize)
        {
            Vector2 size = sprite.bounds.size;
            float scale = Mathf.Min(maxSize / Mathf.Max(0.001f, size.x),
                                    maxSize / Mathf.Max(0.001f, size.y));
            target.localScale = Vector3.one * scale;
        }

        private static SpriteRenderer Block(Transform parent, string name, Vector3 localPos,
            Vector2 size, Color color, int order, Sprite sprite = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : DishSprites.RoundRect();
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private TextMesh Label(Transform parent, string text, float y, float size, Color color)
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) return null;

            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);

            var mesh = go.AddComponent<TextMesh>();
            mesh.font = _font;
            mesh.fontSize = 64;
            mesh.characterSize = size / TextHeightPerUnit;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontStyle = FontStyle.Bold;
            mesh.text = text;
            mesh.color = color;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = _font.material;
                renderer.sortingOrder = 20;
            }

            return mesh;
        }

        /// <summary>Zona clickeable de una carta: no conoce reglas, solo pide disparar.</summary>
        private sealed class CardClick : MonoBehaviour, IPointerClickHandler
        {
            public WaifuPartyView Owner;
            public int Slot;

            public void OnPointerClick(PointerEventData eventData) => Owner.Fire(Slot);
        }
    }
}
