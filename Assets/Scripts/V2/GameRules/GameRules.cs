using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using V2.Customer;
using V2.Food;

public class GameRules : MonoBehaviour, IGameRules
{
    [SerializeField] private float timeToIntro;
    [SerializeField] private CustomerSpawnerCoroutine customerManager;
    [SerializeField] private StaffSpawnerManager staffManager;
    [SerializeField] private float timeToRun;
    [SerializeField] private Slider timeSlider;
    [SerializeField] private TextMeshProUGUI pointsText;
    private float localTime;
    private int totalPoints;

    private TeaTime intro, game, pause, endGame;

    private void Start()
    {
        intro = this.tt();
        intro.Pause().Add(timeToIntro).Add(() => { game.Play(); });

        game = this.tt();
        game.Pause().Add(() =>
        {
            customerManager.Configure(this);
            staffManager.Configure(this);
        }).Loop(handler =>
        {
            localTime += handler.deltaTime;
            timeSlider.value = localTime / timeToRun;
            if (localTime >= timeToRun)
            {
                handler.Break();
                localTime = 0f;
            }
        }).Add(() => { endGame.Play(); });

        endGame = this.tt();
        endGame.Pause().Add(timeToIntro).Add(() => { SceneManager.LoadScene(0); });

        intro.Play();
        pointsText.text = $"Points: {totalPoints}";
    }

    public void CustomerAttended(CustomerClientModel customer, FoodModel food)
    {
        totalPoints += customer.pointsToAttend + food.pointsToAttend;
        pointsText.text = $"Points: {totalPoints}";
    }
}