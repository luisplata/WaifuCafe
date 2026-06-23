using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using V2.Helpers;

public class DayTransitionMediator : MonoBehaviour
{
    [SerializeField] private CanvasGroup previousPanel;
    [SerializeField] private CanvasGroup currentPanel;

    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI mechanicText;

    [SerializeField] private Button continueButton;

    [SerializeField] private string nextScene;

    private void Start()
    {
        continueButton.gameObject.SetActive(false);

        SetupTexts();

        Play();
    }

    private void SetupTexts()
    {
        switch (TutorialProgress.CurrentDay)
        {
            case 1:
                dayText.text = "DAY 1";
                objectiveText.text = "Serve 3 Customers";
                mechanicText.text = "Learn how to assign orders";
                break;

            case 2:
                dayText.text = "DAY 2";
                objectiveText.text = "Create your first Match";
                mechanicText.text = "Matching customers creates bonuses";
                break;

            case 3:
                dayText.text = "DAY 3";
                objectiveText.text = "Create a Double Match";
                mechanicText.text = "Combine Customer + Order Matches";
                break;
        }
    }

    private void Play()
    {
        currentPanel.alpha = 0;

        Sequence.Create()

            .Group(
                Tween.Alpha(previousPanel, 0, 0.5f))

            .Group(
                Tween.Alpha(currentPanel, 1, 0.5f))

            .ChainDelay(0.5f)

            .ChainCallback(() =>
            {
                continueButton.gameObject.SetActive(true);
            });
    }

    public void Continue()
    {
        SceneManager.LoadScene(nextScene);
    }
}