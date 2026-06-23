using UnityEngine;
using UnityEngine.SceneManagement;
using V2.Helpers;

public class MainManuRules : MonoBehaviour
{
    [SerializeField] private IntroMediator tutorialDia0, tutorialDia1, tutorialDia2, tutorialDia3, tutorialDia4;
    private TeaTime _mainMenuStates;


    void Start()
    {
        _mainMenuStates = this.tt().Pause()
            .Add(3)
            .Add(() =>
            {
                if (SaveManager.Instance.IsShowTutorial())
                {
                    switch (TutorialProgress.CurrentDay)
                    {
                        case 0:
                            tutorialDia0.PlayIntro();
                            tutorialDia0.OnFinish += () =>
                            {
                                TutorialProgress.NextDay();
                                _mainMenuStates.Play();
                            };
                            break;
                        case 1:
                            tutorialDia1.PlayIntro();
                            tutorialDia1.OnFinish += () => { SceneManager.LoadScene("WaifuSelector"); };
                            break;
                        case 2:
                            tutorialDia2.PlayIntro();
                            tutorialDia2.OnFinish += () => { SceneManager.LoadScene("WaifuSelector"); };
                            break;
                        case 3:
                            tutorialDia3.PlayIntro();
                            tutorialDia3.OnFinish += () => { SceneManager.LoadScene("WaifuSelector"); };
                            break;
                    }
                }
                else
                {
                    //Ya vamos por aqui cuando ya no es tutorial.
                    if (TutorialProgress.CurrentDay >= 3)
                    {
                        tutorialDia4.PlayIntro();
                        tutorialDia4.OnFinish += () =>
                        {
                            TutorialProgress.NextDay();
                            _mainMenuStates.Play();
                        };
                    }
                }
            });
        _mainMenuStates.Play();
    }
}