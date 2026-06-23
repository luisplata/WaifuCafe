using UnityEngine;
using UnityEngine.SceneManagement;
using V2.Helpers;

public class TutorialBootstrap : MonoBehaviour
{
    [SerializeField] private string introScene;
    [SerializeField] private string runScene;

    private void Start()
    {
        if (TutorialProgress.CurrentDay == 0)
        {
            SceneManager.LoadScene(introScene);
            return;
        }

        if (TutorialProgress.IsCompleted)
        {
            SceneManager.LoadScene(runScene);
            return;
        }

        SceneManager.LoadScene($"TutorialDay{TutorialProgress.CurrentDay}");
    }
}

