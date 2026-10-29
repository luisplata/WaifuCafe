using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace V2.Meta
{
    /// <summary>
    /// Tour del manager (NPC no jugable): páginas de <see cref="GuideSO"/> en orden
    /// contratos → party → gacha → restaurante. No toca el roster ni la party; sólo
    /// presenta información y devuelve el control al terminar o al saltar.
    /// </summary>
    public sealed class GuidePanel
    {
        private static readonly Color BackdropColor = new Color(0.02f, 0.02f, 0.05f, 0.92f);
        private static readonly Color CardColor = new Color(0.10f, 0.09f, 0.15f, 0.98f);
        private static readonly Color PortraitColor = new Color(0.16f, 0.13f, 0.22f, 1f);

        private readonly Transform _parent;
        private GameObject _root;
        private Image _portrait;
        private TextMeshProUGUI _portraitFallback;
        private TextMeshProUGUI _managerName;
        private TextMeshProUGUI _pageTitle;
        private TextMeshProUGUI _pageBody;
        private TextMeshProUGUI _focusLabel;
        private TextMeshProUGUI _counter;
        private TextMeshProUGUI _nextLabel;
        private GuideSO _guide;
        private Action _onFinished;
        private int _index;

        public GuidePanel(Transform parent) => _parent = parent;

        public bool IsVisible => _root != null;

        public void Show(GuideSO guide, Action onFinished)
        {
            Hide();

            _guide = guide;
            _onFinished = onFinished;
            _index = 0;

            if (guide == null || guide.Pages == null || guide.Pages.Length == 0)
            {
                Finish();
                return;
            }

            Build(guide);
            ShowPage(0);
        }

        public void Hide()
        {
            if (_root == null) return;
            UnityEngine.Object.Destroy(_root);
            _root = null;
            _guide = null;
            _onFinished = null;
        }

        private void Build(GuideSO guide)
        {
            _root = MetaUi.Panel(_parent, "GuidePanel", BackdropColor, Vector2.zero, new Vector2(1920f, 1080f));
            MetaUi.Stretch(_root.GetComponent<RectTransform>());
            _root.transform.SetAsLastSibling();

            GameObject portraitFrame = MetaUi.Panel(_root.transform, "ManagerPanel", CardColor,
                new Vector2(140f, 170f), new Vector2(420f, 660f));

            _portrait = MetaUi.Image(portraitFrame.transform, "Portrait", Color.white,
                new Vector2(24f, 24f), new Vector2(372f, 500f));
            _portrait.preserveAspect = true;
            _portrait.raycastTarget = false;
            if (guide.Portrait != null)
            {
                _portrait.sprite = guide.Portrait;
            }
            else
            {
                _portrait.color = PortraitColor;
            }

            _managerName = MetaUi.Label(portraitFrame.transform, "ManagerName", guide.ManagerName, 28f,
                MetaUi.Accent, TextAlignmentOptions.Center, new Vector2(16f, 552f),
                new Vector2(388f, 36f), FontStyles.Bold);

            MetaUi.Label(portraitFrame.transform, "ManagerRole", "Gerente del café", 16f, MetaUi.Muted,
                TextAlignmentOptions.Center, new Vector2(16f, 592f), new Vector2(388f, 24f));

            GameObject body = MetaUi.Panel(_root.transform, "PagePanel", CardColor,
                new Vector2(620f, 170f), new Vector2(1160f, 660f));

            _focusLabel = MetaUi.Label(body.transform, "Focus", string.Empty, 18f, MetaUi.Accent,
                TextAlignmentOptions.TopLeft, new Vector2(36f, 30f), new Vector2(1088f, 26f),
                FontStyles.Bold);

            _pageTitle = MetaUi.Label(body.transform, "Title", string.Empty, 40f, MetaUi.Text,
                TextAlignmentOptions.TopLeft, new Vector2(36f, 66f), new Vector2(1088f, 54f),
                FontStyles.Bold);

            _pageBody = MetaUi.Label(body.transform, "Body", string.Empty, 26f, MetaUi.Text,
                TextAlignmentOptions.TopLeft, new Vector2(36f, 140f), new Vector2(1088f, 360f));

            _counter = MetaUi.Label(body.transform, "Counter", string.Empty, 18f, MetaUi.Muted,
                TextAlignmentOptions.TopLeft, new Vector2(36f, 520f), new Vector2(400f, 26f));

            Button skip = MetaUi.Button(body.transform, "Skip", "SALTAR TOUR",
                new Color(0.22f, 0.19f, 0.30f, 1f), new Vector2(792f, 552f),
                new Vector2(296f, 56f), 18f, out _);
            skip.onClick.AddListener(Finish);

            Button next = MetaUi.Button(body.transform, "Next", "SIGUIENTE", MetaUi.Accent,
                new Vector2(36f, 552f), new Vector2(360f, 56f), 22f, out _nextLabel);
            next.onClick.AddListener(Next);
        }

        private void ShowPage(int index)
        {
            if (_guide == null || _root == null) return;

            GuidePage[] pages = _guide.Pages;
            if (index < 0 || index >= pages.Length) return;

            GuidePage page = pages[index];
            _index = index;
            _pageTitle.text = page.Title;
            _pageBody.text = page.Body;
            _focusLabel.text = "ZONA: " + FocusName(page.Focus);
            _counter.text = (index + 1) + " / " + pages.Length;
            _nextLabel.text = index == pages.Length - 1 ? "EMPEZAR TUTORIAL" : "SIGUIENTE";
        }

        private void Next()
        {
            if (_guide == null) return;

            int last = _guide.Pages.Length - 1;
            if (_index >= last)
            {
                Finish();
                return;
            }

            ShowPage(_index + 1);
        }

        private void Finish()
        {
            Action callback = _onFinished;
            Hide();
            callback?.Invoke();
        }

        private static string FocusName(GuideFocus focus)
        {
            switch (focus)
            {
                case GuideFocus.Party: return "PARTY Y SINERGIA";
                case GuideFocus.Gacha: return "GACHA (BLOQUEADA)";
                case GuideFocus.Restaurant: return "RESTAURANTE (BLOQUEADO)";
                default: return "CONTRATOS";
            }
        }
    }
}
