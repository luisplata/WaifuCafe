using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace V2.Meta
{
    /// <summary>
    /// Tooltip de sinergias/ultimates del panel de party. En E2 sólo muestra el
    /// placeholder de gate; E4-05 le inyecta el texto real desde <c>SynergyModel</c>.
    /// </summary>
    public sealed class SynergyTooltip
    {
        private readonly GameObject _panel;
        private readonly TextMeshProUGUI _label;

        public SynergyTooltip(Transform parent, Vector2 topLeft, Vector2 size)
        {
            _panel = MetaUi.Panel(parent, "SynergyTooltip", new Color(0.04f, 0.03f, 0.07f, 0.97f),
                topLeft, size);
            _panel.transform.SetAsLastSibling();

            _label = MetaUi.Label(_panel.transform, "Text", string.Empty, 15f, MetaUi.Text,
                TextAlignmentOptions.TopLeft, new Vector2(12f, 10f),
                new Vector2(size.x - 24f, size.y - 20f));
            _panel.SetActive(false);
        }

        public void SetText(string text)
        {
            if (_label != null) _label.text = text ?? string.Empty;
        }

        /// <summary>Muestra el tooltip al pasar el mouse sobre <paramref name="target"/>.</summary>
        public void Bind(GameObject target, string text)
        {
            if (target == null) return;

            SetText(text);

            EventTrigger trigger = target.GetComponent<EventTrigger>();
            if (trigger == null) trigger = target.AddComponent<EventTrigger>();

            AddEntry(trigger, EventTriggerType.PointerEnter, _ => Show());
            AddEntry(trigger, EventTriggerType.PointerExit, _ => Hide());
        }

        public void Show() => _panel.SetActive(true);

        public void Hide() => _panel.SetActive(false);

        private static void AddEntry(EventTrigger trigger, EventTriggerType type,
            Action<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => action(data));
            trigger.triggers.Add(entry);
        }
    }
}
