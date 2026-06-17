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
            percentOfGame = localTime / timeToRun;
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
        comboManager.Configure(this);
        comboManager.onComboFinished += OnComboFinished;
    }

    private void OnComboFinished(int obj)
    {
        totalPoints += obj;
    }

    public void CustomerAttended(CustomerClientModel customer, FoodModel food)
    {
        comboManager.RegisterServed(food, customer);
    }

    public bool IsPatienceAltered()
    {
        return staffManager.IsPatienceAltered();
    }

    public float GetAlteredPatience()
    {
        return staffManager.GetAlteredPatience();
    }

    public bool IsComboBreaker()
    {
        return staffManager.IsComboBreaker();
    }

    public float GetAlteredEconomy()
    {
        return staffManager.GetAlteredEconomy();
    }

    public bool IsEconomyModify()
    {
        return staffManager.IsEconomyModify();
    }
}