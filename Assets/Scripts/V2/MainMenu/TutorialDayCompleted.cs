using UnityEngine;
using UnityEngine.SceneManagement;
using V2.Helpers;

public class TutorialDayCompleted : MonoBehaviour
{
    [SerializeField] private string transitionScene;

    public void CompleteDay()
    {
        TutorialProgress.NextDay();

        SceneManager.LoadScene(transitionScene);
    }
}