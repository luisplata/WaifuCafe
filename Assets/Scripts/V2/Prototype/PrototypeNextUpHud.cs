using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using V2.Food;

/// <summary>
/// Prototipo: muestra los próximos N clientes en HUD para que el jugador pueda
/// planear el match-3. Crea su propia UI en código (sin tocar el Canvas a mano).
/// </summary>
public class PrototypeNextUpHud : MonoBehaviour
{
    [SerializeField] private int slots = 3;
    [SerializeField] private Vector2 slotSize = new(96f, 128f);
    [Header("Slots de editor (si están todos asignados, no se crea UI en código)")]
    [SerializeField] private List<Image> foodIconSlots = new();
    [SerializeField] private List<TextMeshProUGUI> labelSlots = new();
    [SerializeField] private TextMeshProUGUI waveBannerSlot;

    private CustomerSpawnerCoroutine spawner;
    private readonly List<Image> foodIcons = new();
    private readonly List<TextMeshProUGUI> labels = new();
    private TextMeshProUGUI waveBanner;

    [Header("Juice de cola (deslizar en vez de popup)")]
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private float slideDistance = 120f;
    [SerializeField] private float staggerPerSlot = 0.15f;
    private readonly List<RectTransform> slotRects = new();
    private readonly List<Vector2> slotBasePos = new();
    private float queueAnim = 10f;

    private void Start()
    {
        spawner = FindFirstObjectByType<CustomerSpawnerCoroutine>();
        if (spawner == null)
        {
            Debug.LogError("PrototypeNextUpHud: no hay CustomerSpawnerCoroutine en escena.");
            return;
        }
        BuildUiOrBindEditorSlots();
        foreach (var icon in foodIcons)
        {
            var slotRt = icon.transform.parent as RectTransform;
            slotRects.Add(slotRt);
            slotBasePos.Add(slotRt.anchoredPosition);
        }
        spawner.OnPreviewChanged += Refresh;
        Refresh();
        queueAnim = 10f;
    }

    private void BuildUiOrBindEditorSlots()
    {
        if (foodIconSlots.Count >= slots && labelSlots.Count >= slots && waveBannerSlot != null)
        {
            foodIcons.AddRange(foodIconSlots.GetRange(0, slots));
            labels.AddRange(labelSlots.GetRange(0, slots));
            waveBanner = waveBannerSlot;
            return;
        }
        BuildUi();
    }

    private void OnDestroy()
    {
        if (spawner != null) spawner.OnPreviewChanged -= Refresh;
    }

    private void BuildUi()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        var root = new GameObject("NextUp (Prototype)");
        root.transform.SetParent(canvas.transform, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(0f, 1f);
        rootRt.pivot = new Vector2(0f, 1f);
        rootRt.anchoredPosition = new Vector2(16f, -16f);
        rootRt.sizeDelta = new Vector2(slotSize.x * slots + 32f, slotSize.y + 64f);
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);

        var title = new GameObject("Title");
        title.transform.SetParent(root.transform, false);
        var titleRt = title.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -6f);
        titleRt.sizeDelta = new Vector2(0f, 36f);
        var titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "PRÓXIMOS";
        titleText.fontSize = 26;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        for (int i = 0; i < slots; i++)
        {
            var slot = new GameObject($"Slot{i}");
            slot.transform.SetParent(root.transform, false);
            var slotRt = slot.AddComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0f, 1f);
            slotRt.anchorMax = new Vector2(0f, 1f);
            slotRt.pivot = new Vector2(0f, 1f);
            slotRt.anchoredPosition = new Vector2(16f + i * slotSize.x, -48f);
            slotRt.sizeDelta = new Vector2(slotSize.x - 8f, slotSize.y);

            var icon = new GameObject("Food");
            icon.transform.SetParent(slot.transform, false);
            var iconRt = icon.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -4f);
            iconRt.sizeDelta = new Vector2(72f, 72f);
            var img = icon.AddComponent<Image>();
            img.preserveAspect = true;
            foodIcons.Add(img);

            var label = new GameObject("Label");
            label.transform.SetParent(slot.transform, false);
            var labelRt = label.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = new Vector2(0f, -80f);
            labelRt.sizeDelta = new Vector2(0f, 48f);
            var labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 22;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(1f, 0.85f, 0.9f);
            labels.Add(labelText);
        }

        var banner = new GameObject("WaveBanner");
        banner.transform.SetParent(canvas.transform, false);
        var bannerRt = banner.AddComponent<RectTransform>();
        bannerRt.anchorMin = new Vector2(0.5f, 1f);
        bannerRt.anchorMax = new Vector2(0.5f, 1f);
        bannerRt.pivot = new Vector2(0.5f, 1f);
        bannerRt.anchoredPosition = new Vector2(0f, -16f);
        bannerRt.sizeDelta = new Vector2(400f, 48f);
        waveBanner = banner.AddComponent<TextMeshProUGUI>();
        waveBanner.fontSize = 30;
        waveBanner.alignment = TextAlignmentOptions.Center;
        waveBanner.text = string.Empty;
    }

    private void Update()
    {
        UpdateQueueSlide();
        if (waveBanner == null) return;
        var waves = FindFirstObjectByType<PrototypeWaves>();
        if (waves == null || !waves.enabled)
        {
            waveBanner.text = string.Empty;
            return;
        }
        if (waves.IsPeak)
        {
            waveBanner.text = "▲ OLEADA ▲";
            waveBanner.color = new Color(1f, 0.45f, 0.5f);
        }
        else
        {
            waveBanner.text = "▽ calma ▽";
            waveBanner.color = new Color(0.6f, 0.75f, 1f);
        }
    }

    private void UpdateQueueSlide()
    {
        if (queueAnim >= 2f || slotRects.Count == 0) return;
        queueAnim += Time.deltaTime / Mathf.Max(0.05f, slideDuration);
        for (int i = 0; i < slotRects.Count && i < slotBasePos.Count; i++)
        {
            float p = Mathf.Clamp01(queueAnim - i * staggerPerSlot);
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            slotRects[i].anchoredPosition = slotBasePos[i] + new Vector2(0f, -(1f - eased) * slideDistance);
        }
    }

    private void Refresh()
    {
        queueAnim = 0f;
        if (spawner == null) return;
        var list = spawner.Preview;
        for (int i = 0; i < slots; i++)
        {
            if (i < list.Count)
            {
                var next = list[i];
                foodIcons[i].sprite = next.food?.foodSprite;
                foodIcons[i].color = Color.white;
                labels[i].text = $"{FoodShort(next.food?.foodModelType ?? FoodModelType.None)}\n{CustShort(next.customerPrefab)}";
            }
            else
            {
                foodIcons[i].sprite = null;
                foodIcons[i].color = new Color(1f, 1f, 1f, 0.15f);
                labels[i].text = "—";
            }
        }
    }

    private static string FoodShort(FoodModelType t) => t switch
    {
        FoodModelType.Desayuno => "DES",
        FoodModelType.Almuerzo => "ALM",
        FoodModelType.Bebida => "BEB",
        _ => "?"
    };

    private static string CustShort(CustomerClient c)
    {
        if (c == null || c.CustomerData == null) return "?";
        return c.CustomerData.customerIdentify switch
        {
            CustomerIdentify.VIP => "VIP",
            CustomerIdentify.Casual => "CAS",
            CustomerIdentify.Apurado => "RUSH",
            CustomerIdentify.Paciencia => "PAC",
            _ => "?"
        };
    }
}
