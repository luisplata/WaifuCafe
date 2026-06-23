using TMPro;
using UnityEngine;

public class DialogsAnim : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI intro;

    public void SetText(string text)
    {
        intro.text = text;
    }
}