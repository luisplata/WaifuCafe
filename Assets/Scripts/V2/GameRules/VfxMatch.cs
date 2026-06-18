using PrimeTween;
using TMPro;
using UnityEngine;

public class VfxMatch : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private TextMeshProUGUI matchText;

    private Tween _scaleTween;

    private void Awake()
    {
        container.SetActive(false);
        container.transform.localScale = Vector3.zero;
    }

    public void PlayMatchVfx(ComboType comboType)
    {
        Play(
            $"{comboType.ToString().ToUpper()} MATCH!",
            Color.white,
            1.5f
        );
    }

    public void PlayDoubleMatchVfx()
    {
        Play(
            "DOUBLE MATCH!!!",
            Color.yellow,
            2f
        );
    }

    private void Play(string text, Color color, float scale)
    {
        _scaleTween.Stop();

        container.SetActive(true);

        matchText.text = text;
        matchText.color = color;

        container.transform.localScale = Vector3.zero;

        Sequence.Create()
            .Group(Tween.Scale(container.transform, Vector3.one * scale, 0.25f, Ease.OutBack))
            .ChainDelay(0.4f)
            .Chain(Tween.Scale(container.transform, Vector3.zero, 0.2f, Ease.InBack))
            .OnComplete(() => { container.SetActive(false); });
    }
}