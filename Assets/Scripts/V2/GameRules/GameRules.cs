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
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI acumuladoText;
    [SerializeField] private ComboManager comboManager;
    [SerializeField] private float percentOfGame;
    public float Percent => percentOfGame;
    private float localTime;
    private int totalPoints;
    private FoodModelType? currentComboType;
    private int currentComboCount;

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
            percentOfGame =  localTime / timeToRun;
            timeSlider.value = percentOfGame;
            if (localTime >= timeToRun)
            {
                handler.Break();
                localTime = 0f;
            }
        }).Add(() => { endGame.Play(); });

        endGame = this.tt();
        endGame.Pause().Add(timeToIntro).Add(() => { SceneManager.LoadScene(0); });

        intro.Play();
        pointsText.text = $"Points: {0}";
        comboText.text = $"Combo: x{0}";
        acumuladoText.text = $"Acumulado: {0}";

        comboManager.onComboFinished += OnComboFinished;
    }

    private void OnComboFinished(int obj)
    {
        totalPoints += obj;
        pointsText.text = $"Points: {totalPoints}";
    }

    public void CustomerAttended(CustomerClientModel customer, FoodModel food)
    {
        var comboCount = comboManager.RegisterServedFood(food, customer);
        acumuladoText.text = $"Acumulado: {comboCount.Item3}";
        comboText.text = $"Combo: x{comboCount.Item1} , {comboCount.Item2}";
    }
}