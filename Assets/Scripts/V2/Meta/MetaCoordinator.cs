using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using V2.Waifu;
using WaifuCafe.Core.Meta;

namespace V2.Meta
{
    /// <summary>
    /// Composition root del menú meta. Usa los servicios de <see cref="GameRoot"/>
    /// (catálogo, contratos y perfil), pinta roster/party/contratos y lanza la run con
    /// <see cref="IRunLauncher"/>. No busca objetos por tipo: las referencias de escena
    /// llegan serializadas y los servicios por <c>GameRoot.Instance</c>.
    /// </summary>
    public sealed class MetaCoordinator : MonoBehaviour
    {
        private const int MaxParty = 4;

        /// <summary>Contratos jugables visibles por sesión (el resto rota al lanzar).</summary>
        private const int ContractPoolSize = 3;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI coinsLabel;
        [SerializeField] private TextMeshProUGUI profileLabel;
        [SerializeField] private Button resetButton;

        [Header("Paneles")]
        [SerializeField] private RectTransform contractPanel;
        [SerializeField] private RectTransform rosterPanel;
        [SerializeField] private RectTransform partyPanel;

        [Header("Reset")]
        [SerializeField] private GameObject resetConfirmPanel;
        [SerializeField] private Button resetConfirmYesButton;
        [SerializeField] private Button resetConfirmNoButton;

        private GameRoot _root;
        private IWaifuCatalog _catalog;
        private IContractCatalog _contracts;
        private IProfileStore _profile;
        private IRunLauncher _launcher;
        private PartyPanelView _partyPanelView;
        private RectTransform _rosterContent;

        private readonly List<ContractCardView> _cards = new List<ContractCardView>();
        private readonly List<RosterChipView> _chips = new List<RosterChipView>();
        private readonly List<string> _playableIds = new List<string>();
        private readonly List<string> _pool = new List<string>();
        private ContractSO _selectedContract;
        private ContractSO _recommendedContract;
        private bool _contractChosenThisSession;

        private void Start()
        {
            Build();
        }

        private void OnDestroy()
        {
            if (_profile != null) _profile.Changed -= RefreshAll;
            if (resetButton != null) resetButton.onClick.RemoveListener(ShowResetConfirm);
            if (resetConfirmYesButton != null) resetConfirmYesButton.onClick.RemoveListener(ConfirmReset);
            if (resetConfirmNoButton != null) resetConfirmNoButton.onClick.RemoveListener(HideResetConfirm);
        }

        private void Build()
        {
            _root = GameRoot.Instance;
            if (_root == null || !_root.IsInitialized)
            {
                Debug.LogError("[MetaCoordinator] No hay GameRoot inicializado en MetaMenu.");
                return;
            }

            _catalog = _root.Catalog;
            _contracts = _root.Contracts;
            _profile = _root.Profile;
            _launcher = _root;

            if (_catalog == null || _contracts == null || _profile == null)
            {
                Debug.LogError("[MetaCoordinator] GameRoot sin catálogo/contratos/perfil; menú inerte.");
                return;
            }

            if (_contracts.All.Count == 0)
            {
                Debug.LogWarning("[MetaCoordinator] GameRoot.contracts está vacío: " +
                                 "lastContractId se saneará a \"\" y no habrá contratos jugables.");
            }

            if (resetButton != null) resetButton.onClick.AddListener(ShowResetConfirm);
            if (resetConfirmYesButton != null) resetConfirmYesButton.onClick.AddListener(ConfirmReset);
            if (resetConfirmNoButton != null) resetConfirmNoButton.onClick.AddListener(HideResetConfirm);

            // El pool se deriva una vez por sesión; la recomendación depende de la party,
            // así que se recalcula en cada refresh (sin party no hay listón).
            CachePlayableIds();
            ResolvePool();
            ResolveRecommendation();

            BuildContractCards();
            BuildRosterScroll();
            _partyPanelView = new PartyPanelView(partyPanel, RemoveFromParty);
            _partyPanelView.StartRequested += StartRun;

            _profile.Changed += RefreshAll;
            RefreshAll();
        }

        private void BuildContractCards()
        {
            if (contractPanel == null) return;

            float panelWidth = contractPanel.rect.width > 0f ? contractPanel.rect.width : 1120f;

            MetaUi.Label(contractPanel, "Title",
                $"CONTRATOS · {_pool.Count} DE {_playableIds.Count}", 24f, MetaUi.Accent,
                TextAlignmentOptions.TopLeft, new Vector2(18f, 12f), new Vector2(600f, 34f),
                FontStyles.Bold);

            BuildUpcomingLabel(panelWidth);

            int count = _pool.Count;
            if (count <= 0) return;

            const float gap = 12f;
            const float padding = 18f;
            const float top = 56f;
            // Sin cap: con 3 tarjetas el ancho disponible se reparte entre todas.
            float cardWidth = (panelWidth - padding * 2f - gap * (count - 1)) / count;
            float cardHeight = 424f;

            for (int i = 0; i < count; i++)
            {
                ContractSO contract = _contracts.Find(_pool[i]);
                if (contract == null) continue;

                var view = new ContractCardView(contractPanel, contract,
                    new Vector2(padding + i * (cardWidth + gap), top),
                    new Vector2(cardWidth, cardHeight), SelectContract);
                _cards.Add(view);
            }

            // El pool es la sugerencia; el pie aclara que no es una obligación.
            MetaUi.Label(contractPanel, "Hint", "Elegí un contrato para jugar", 13f, MetaUi.Muted,
                TextAlignmentOptions.TopLeft, new Vector2(padding, top + cardHeight + 2f),
                new Vector2(panelWidth - padding * 2f, 22f));
        }

        /// <summary>
        /// Los locked no tienen tarjeta: una sola línea en el header telegrafía qué falta
        /// y por qué (título + <see cref="ContractSO.LockReason"/>).
        /// </summary>
        private void BuildUpcomingLabel(float panelWidth)
        {
            IReadOnlyList<ContractSO> all = _contracts.All;
            var text = new System.Text.StringBuilder("PRÓXIMAMENTE");
            int shown = 0;

            for (int i = 0; i < all.Count; i++)
            {
                ContractSO contract = all[i];
                if (contract == null || !contract.Locked) continue;

                text.Append(" · ").Append(contract.Title);
                if (!string.IsNullOrWhiteSpace(contract.LockReason))
                    text.Append(" (").Append(contract.LockReason.Trim()).Append(')');
                shown++;
            }

            if (shown == 0) return;

            const float reservedTitle = 620f;
            MetaUi.Label(contractPanel, "Upcoming", text.ToString(), 12f, MetaUi.Muted,
                TextAlignmentOptions.TopRight, new Vector2(reservedTitle, 10f),
                new Vector2(panelWidth - reservedTitle - 18f, 44f));
        }

        /// <summary>
        /// Ids de los contratos jugables en orden de catálogo. <see cref="IContractCatalog.All"/>
        /// es inmutable por vida de <see cref="GameRoot"/>, así que se cachea una vez.
        /// </summary>
        private void CachePlayableIds()
        {
            _playableIds.Clear();
            IReadOnlyList<ContractSO> all = _contracts.All;
            for (int i = 0; i < all.Count; i++)
            {
                ContractSO contract = all[i];
                if (contract != null && !contract.Locked && !string.IsNullOrWhiteSpace(contract.Id))
                    _playableIds.Add(contract.Id);
            }
        }

        /// <summary>
        /// Ventana de contratos jugables derivada del cursor. Sin cursor (perfil nuevo,
        /// reset, cursor locked/desconocido) arranca en el primero del catálogo.
        /// </summary>
        private void ResolvePool()
        {
            _pool.Clear();
            _recommendedContract = null;
            PlayerProfile profile = _profile != null ? _profile.Current : null;
            string cursor = profile != null ? profile.lastContractId : null;
            IReadOnlyList<string> ids = ContractPool.Build(_playableIds, cursor, ContractPoolSize);
            for (int i = 0; i < ids.Count; i++) _pool.Add(ids[i]);
        }

        /// <summary>
        /// Recomendación del juego: el contrato del pool que comparte la facción dominante
        /// de la party. Sin mayoría clara o sin match, no hay recomendado y no hay listón.
        /// </summary>
        private void ResolveRecommendation()
        {
            _recommendedContract = null;
            if (_pool.Count == 0 || _profile == null || _catalog == null) return;

            PlayerProfile profile = _profile.Current;
            if (profile == null) return;

            var candidates = new List<ContractCandidate>(_pool.Count);
            for (int i = 0; i < _pool.Count; i++)
            {
                ContractSO contract = _contracts.Find(_pool[i]);
                if (contract != null) candidates.Add(new ContractCandidate(contract.Id, contract.Faction));
            }

            var partyFactions = new List<WaifuFaction>(MaxParty);
            if (profile.party != null)
            {
                for (int i = 0; i < profile.party.Count; i++)
                {
                    WaifuDefinitionSO definition = _catalog.Find(profile.party[i]);
                    if (definition != null) partyFactions.Add(definition.Faction);
                }
            }

            string recommendedId = ContractRecommendation.Recommend(candidates, partyFactions);
            if (!string.IsNullOrEmpty(recommendedId)) _recommendedContract = _contracts.Find(recommendedId);
        }

        private ContractSO FirstPlayable()
        {
            IReadOnlyList<ContractSO> all = _contracts.All;
            for (int i = 0; i < all.Count; i++)
            {
                ContractSO contract = all[i];
                if (contract != null && !contract.Locked) return contract;
            }

            return null;
        }

        /// <summary>
        /// Rearma la grilla completa desde el pool actual. Se usa tras un reset, cuando el
        /// cursor cambia y por lo tanto la ventana visible ya no es la misma.
        /// </summary>
        private void RebuildContractCards()
        {
            if (contractPanel == null) return;

            for (int i = contractPanel.childCount - 1; i >= 0; i--)
            {
                GameObject child = contractPanel.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            _cards.Clear();
            ResolvePool();
            BuildContractCards();
            UpdateCardSelection();
        }

        private void BuildRosterScroll()
        {
            if (rosterPanel == null) return;

            MetaUi.Label(rosterPanel, "Title", "ROSTER", 24f, MetaUi.Accent,
                TextAlignmentOptions.TopLeft, new Vector2(18f, 12f), new Vector2(600f, 34f),
                FontStyles.Bold);

            float width = rosterPanel.rect.width > 0f ? rosterPanel.rect.width : 1120f;
            float height = rosterPanel.rect.height > 0f ? rosterPanel.rect.height : 399f;

            var scrollGo = new GameObject("RosterScroll", typeof(RectTransform), typeof(ScrollRect));
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.SetParent(rosterPanel, false);
            MetaUi.SetTopLeft(scrollRect, new Vector2(18f, 50f), new Vector2(width - 36f, height - 64f));

            GameObject viewport = MetaUi.Panel(scrollRect, "Viewport",
                new Color(1f, 1f, 1f, 0.01f), Vector2.zero, Vector2.zero);
            var viewportRect = viewport.GetComponent<RectTransform>();
            MetaUi.Stretch(viewportRect);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content", typeof(RectTransform));
            _rosterContent = content.GetComponent<RectTransform>();
            _rosterContent.SetParent(viewport.transform, false);
            _rosterContent.anchorMin = new Vector2(0f, 1f);
            _rosterContent.anchorMax = new Vector2(1f, 1f);
            _rosterContent.pivot = new Vector2(0.5f, 1f);
            _rosterContent.anchoredPosition = Vector2.zero;
            _rosterContent.sizeDelta = new Vector2(0f, 0f);

            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(200f, 160f);
            grid.spacing = new Vector2(10f, 10f);
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = _rosterContent;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
        }

        private void RefreshAll()
        {
            if (_profile == null || _catalog == null) return;

            PlayerProfile profile = _profile.Current;
            if (profile == null) return;

            if (coinsLabel != null) coinsLabel.text = "Monedas: " + profile.coins;
            if (profileLabel != null)
            {
                int owned = profile.owned != null ? profile.owned.Count : 0;
                int party = profile.party != null ? profile.party.Count : 0;
                profileLabel.text = $"Perfil: {owned}/{_catalog.All.Count} waifus · Party {party}/{MaxParty}";
            }

            RebuildRoster(profile);
            _partyPanelView?.Refresh(profile.party, _catalog, profile);
            RefreshSynergy(profile);
            // La recomendación sale de la party: se recalcula en cada refresh, no se congela.
            ResolveRecommendation();
            EnsureContractSelection();
            UpdateCardSelection();
            UpdateStartState(profile);
        }

        /// <summary>
        /// Sinergia real de la party: breakpoints, badge de ultimate y tooltip. Todo sale de
        /// <see cref="SynergyModel"/>; la vista sólo pinta el texto.
        /// </summary>
        private void RefreshSynergy(PlayerProfile profile)
        {
            if (_partyPanelView == null) return;

            List<WaifuKit> kits = BuildPartyKits(profile);
            PartyModifiers modifiers = SynergyModel.Build(kits);
            UltimateInfo ultimate = modifiers.Ultimate;

            _partyPanelView.SetSynergySummary("Sinergia: " + SynergyModel.Summary(kits));
            _partyPanelView.SetUltimateBadge(SynergyModel.BadgeText(ultimate, kits),
                ultimate.IsReady && ultimate.Live);
            _partyPanelView.SetUltimateTooltip(SynergyModel.Tooltip(kits));
        }

        private List<WaifuKit> BuildPartyKits(PlayerProfile profile)
        {
            var kits = new List<WaifuKit>(MaxParty);
            if (profile == null || profile.party == null || _catalog == null) return kits;

            foreach (string id in profile.party)
            {
                WaifuDefinitionSO definition = _catalog.Find(id);
                if (definition == null) continue;

                int stars = StarsOf(profile, definition.Id);
                if (stars <= 0) stars = definition.BaseStars;

                kits.Add(new WaifuKit
                {
                    Id = definition.Id,
                    Faction = definition.Faction,
                    Role = definition.Role,
                    Element = definition.Element,
                    Active = definition.SkillArchetype,
                    Passive = definition.PassiveId,
                    Stars = WaifuStars.Clamp(stars),
                    ChargeCost = definition.ChargeCost,
                    CooldownMatches = definition.CooldownMatches,
                    BasePotency = definition.Potency,
                });
            }

            return kits;
        }

        private void RebuildRoster(PlayerProfile profile)
        {
            if (_rosterContent == null) return;

            for (int i = _rosterContent.childCount - 1; i >= 0; i--)
            {
                GameObject child = _rosterContent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            _chips.Clear();

            HashSet<string> owned = OwnedIds(profile);
            for (int i = 0; i < _catalog.All.Count; i++)
            {
                WaifuDefinitionSO definition = _catalog.All[i];
                if (definition == null) continue;

                bool isOwned = owned.Contains(definition.Id);
                bool inParty = PartyContains(profile, definition.Id);
                int stars = isOwned ? StarsOf(profile, definition.Id) : 0;
                _chips.Add(new RosterChipView(_rosterContent, definition, stars, isOwned, inParty,
                    ToggleParty));
            }
        }

        private void ToggleParty(WaifuDefinitionSO definition)
        {
            if (definition == null || _profile == null) return;

            PlayerProfile profile = _profile.Current;
            if (!IsOwned(profile, definition.Id)) return;

            profile.party ??= new List<string>();

            int index = IndexInParty(profile, definition.Id);
            if (index >= 0)
            {
                profile.party.RemoveAt(index);
            }
            else
            {
                if (profile.party.Count >= MaxParty)
                {
                    _partyPanelView?.SetStartEnabled(false, "PARTY LLENA (4/4)");
                    return;
                }

                profile.party.Add(definition.Id);
            }

            _profile.Save();
        }

        private void RemoveFromParty(string waifuId)
        {
            if (_profile == null || string.IsNullOrWhiteSpace(waifuId)) return;

            PlayerProfile profile = _profile.Current;
            int index = IndexInParty(profile, waifuId);
            if (index < 0) return;

            profile.party.RemoveAt(index);
            _profile.Save();
        }

        private void SelectContract(ContractSO contract)
        {
            if (contract == null || contract.Locked || _profile == null) return;

            // Elegir no mueve el cursor de rotación: `lastContractId` significa "último
            // jugado" y lo escribe StartRun. Si no, clickear sin jugar rotaría el destacado.
            _selectedContract = contract;
            _contractChosenThisSession = true;

            // Sin Save() no hay Changed, así que el refresco de la selección es explícito:
            // si no, el pick cambia en memoria y la tarjeta resaltada no se mueve.
            UpdateCardSelection();
            UpdateStartState(_profile.Current);
        }

        /// <summary>
        /// Corre en cada <c>Changed</c>. Sin pick manual de la sesión, preselecciona el
        /// destacado (congelado por sesión) y sólo cae al primer jugable si no hay.
        /// </summary>
        private void EnsureContractSelection()
        {
            // Sin preselección: el jugador elige siempre. Sólo se limpia el pick si el
            // contrato salió del pool (reset) o dejó de estar disponible.
            if (_selectedContract == null) return;

            if (_selectedContract.Locked || !_pool.Contains(_selectedContract.Id))
                _selectedContract = null;
        }

        private void UpdateCardSelection()
        {
            foreach (ContractCardView card in _cards)
            {
                card.SetSelected(card.Contract == _selectedContract);
                card.SetRecommended(card.Contract == _recommendedContract);
            }
        }

        private void UpdateStartState(PlayerProfile profile)
        {
            if (_partyPanelView == null) return;

            string reason = StartBlocker(profile);
            _partyPanelView.SetStartEnabled(string.IsNullOrEmpty(reason), reason);
        }

        private string StartBlocker(PlayerProfile profile)
        {
            if (_pool.Count == 0) return "SIN CONTRATOS";
            if (_selectedContract == null) return "ELEGÍ UN CONTRATO";
            if (profile == null || profile.party == null) return "PARTY VACÍA";

            if (profile.party.Count != MaxParty) return $"FALTAN {MaxParty - profile.party.Count} WAIFUS";

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string id in profile.party)
            {
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) return "PARTY INVÁLIDA";
                if (!IsOwned(profile, id)) return "PARTY INVÁLIDA";
                if (_catalog.Find(id) == null) return "PARTY INVÁLIDA";
            }

            return null;
        }

        private void StartRun()
        {
            if (_profile == null || _selectedContract == null) return;

            PlayerProfile profile = _profile.Current;
            string blocker = StartBlocker(profile);
            if (!string.IsNullOrEmpty(blocker))
            {
                _partyPanelView?.SetStartEnabled(false, blocker);
                return;
            }

            profile.lastContractId = _selectedContract.Id;
            _profile.Save();

            RunConfig config = _selectedContract.ToRunConfig(Environment.TickCount, profile.party);
            _launcher?.Launch(new RunRequest(config));
        }

        private void ShowResetConfirm()
        {
            if (resetConfirmPanel != null) resetConfirmPanel.SetActive(true);
        }

        private void HideResetConfirm()
        {
            if (resetConfirmPanel != null) resetConfirmPanel.SetActive(false);
        }

        private void ConfirmReset()
        {
            HideResetConfirm();
            _contractChosenThisSession = false;

            // El reset borra el cursor de rotación: se invalidan pool y destacado para que
            // el próximo Changed los re-derive del perfil vacío.
            _pool.Clear();
            _recommendedContract = null;
            _recommendedContract = null;
            _selectedContract = null;
            _profile?.Reset();

            // El reset cambia el cursor: la ventana visible también.
            RebuildContractCards();
        }

        private static HashSet<string> OwnedIds(PlayerProfile profile)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (profile == null || profile.owned == null) return ids;

            foreach (StarterEntry entry in profile.owned)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.waifuId))
                    ids.Add(entry.waifuId);
            }

            return ids;
        }

        private static bool IsOwned(PlayerProfile profile, string id)
        {
            return !string.IsNullOrWhiteSpace(id) && OwnedIds(profile).Contains(id);
        }

        private static int StarsOf(PlayerProfile profile, string id)
        {
            if (profile == null || profile.owned == null || string.IsNullOrWhiteSpace(id)) return 0;

            foreach (StarterEntry entry in profile.owned)
            {
                if (entry != null && string.Equals(entry.waifuId, id, StringComparison.OrdinalIgnoreCase))
                    return entry.stars;
            }

            return 0;
        }

        private static bool PartyContains(PlayerProfile profile, string id)
        {
            return IndexInParty(profile, id) >= 0;
        }

        private static int IndexInParty(PlayerProfile profile, string id)
        {
            if (profile == null || profile.party == null || string.IsNullOrWhiteSpace(id)) return -1;

            for (int i = 0; i < profile.party.Count; i++)
            {
                if (string.Equals(profile.party[i], id, StringComparison.OrdinalIgnoreCase)) return i;
            }

            return -1;
        }
    }

    /// <summary>
    /// Helpers mínimos de uGUI/TMP compartidos por las vistas del menú meta. Posiciona
    /// en coordenadas de pantalla con origen arriba-izquierda (1920×1080).
    /// </summary>
    internal static class MetaUi
    {
        public static readonly Color Text = new Color(0.96f, 0.95f, 1f, 0.95f);
        public static readonly Color Muted = new Color(1f, 1f, 1f, 0.58f);
        public static readonly Color Accent = new Color(1f, 0.84f, 0.34f, 1f);

        public static GameObject Panel(Transform parent, string name, Color color,
            Vector2 topLeft, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            SetTopLeft(go.GetComponent<RectTransform>(), topLeft, size);
            go.GetComponent<Image>().color = color;
            return go;
        }

        public static Image Image(Transform parent, string name, Color color,
            Vector2 topLeft, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            SetTopLeft(go.GetComponent<RectTransform>(), topLeft, size);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static GameObject AccentBar(Transform parent, Vector2 topLeft, Vector2 size, Color color)
        {
            GameObject go = Panel(parent, "Accent", color, topLeft, size);
            go.GetComponent<Image>().raycastTarget = false;
            return go;
        }

        public static TextMeshProUGUI Label(Transform parent, string name, string text, float size,
            Color color, TextAlignmentOptions alignment, Vector2 topLeft, Vector2 rectSize,
            FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            SetTopLeft(go.GetComponent<RectTransform>(), topLeft, rectSize);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text ?? string.Empty;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        public static Button Button(Transform parent, string name, string text, Color background,
            Vector2 topLeft, Vector2 size, float fontSize, out TextMeshProUGUI label)
        {
            GameObject root = Panel(parent, name, background, topLeft, size);
            var image = root.GetComponent<Image>();
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.82f, 1f);
            colors.pressedColor = new Color(0.82f, 0.78f, 0.68f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.5f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            label = Label(root.transform, "Label", text, fontSize, new Color(0.08f, 0.06f, 0.10f, 1f),
                TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, FontStyles.Bold);
            Stretch(label.rectTransform);
            return button;
        }

        public static void SetTopLeft(RectTransform rect, Vector2 topLeft, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
