using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using V2.Customer;
using V2.Food;
using V2.Helpers;

public enum DayObjective
{
    None,
    CustomerMatch,
    FoodMatch,
    DoubleMatch
}

public class GameRules : MonoBehaviour, IGameRules
{
    [SerializeField] private float timeToIntro;
    [SerializeField] private CustomerSpawnerCoroutine customerManager;
    [SerializeField] private StaffSpawnerManager staffManager;
    [SerializeField] private float timeToRun;
    [SerializeField] private Slider timeSlider;
    [SerializeField] private ComboManager comboManager;
    [SerializeField] private float percentOfGame;
    [SerializeField] private IntroMediator endGameCinematic;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private float timeToEndGame = 4f;
    [SerializeField] private DayObjective dayObjective;
    [SerializeField] private string objectiveLabel;
    [SerializeField] private TextMeshProUGUI objectiveHudText;
    [SerializeField] private GameObject continuePanel;
    [SerializeField] private int objectiveFailLimit = 3;
    public event Action<int> OnObjectiveProgress;
    public float Percent => percentOfGame;

    private bool objectiveMet;
    private int objectiveRemaining;
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
        endGame.Pause().Add(() =>
        {
            customerManager.Stop();

            int best = Mathf.Max(totalPoints, PlayerPrefs.GetInt("BestScore", 0));
            PlayerPrefs.SetInt("BestScore", best);
            PlayerPrefs.Save();
            if (scoreText) scoreText.text = $"Puntos: {totalPoints} · Récord: {best}";
        }).Add(timeToEndGame).Add(() =>
        {
            //Aqui es donde validamos si cumple o no con el objetivo del dia
            if (SaveManager.Instance != null && SaveManager.Instance.IsShowTutorial())
            {
                if (dayObjective == DayObjective.None || objectiveMet)
                {
                    // Objetivo cumplido (o sin objetivo definido): avanza de día
                    PlayerPrefs.SetInt("TutorialFailStreak", 0);
                    PlayerPrefs.Save();
                    TutorialProgress.NextDay();
                    endGameCinematic.PlayIntro();
                    endGameCinematic.OnFinish += () =>
                    {
                        SceneManager.LoadScene(0);
                    };
                }
                else
                {
                    // No cumplió el objetivo: derrota suave, repite el mismo día.
                    // Streak POR DÍA (self-healing): si el marcador de día no coincide
                    // con el día actual, el streak se reancla a 0 antes de incrementar.
                    // Esto cura residuales de PlayerPrefs y evita acumular entre días.
                    int day = TutorialProgress.CurrentDay;
                    int streak = PlayerPrefs.GetInt("TutorialFailStreak", 0);
                    if (PlayerPrefs.GetInt("TutorialFailStreakDay", -1) != day) streak = 0;
                    streak++;
                    PlayerPrefs.SetInt("TutorialFailStreak", streak);
                    PlayerPrefs.SetInt("TutorialFailStreakDay", day);
                    PlayerPrefs.Save();

                    if (streak >= objectiveFailLimit && continuePanel)
                    {
                        // Umbral alcanzado: el jugador decide reintentar o continuar.
                        continuePanel.SetActive(true);
                    }
                    else
                    {
                        // Fallos 1..limit-1: mensaje visible + reload DESPUÉS de ~1.5s.
                        // El reload nunca ocurre en el mismo frame que el mensaje
                        // (causa raíz del mensaje invisible previo).
                        if (scoreText) scoreText.text = "No lograste el objetivo: " + objectiveLabel;
                        this.tt().Add(1.5f).Add(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
                    }
                }
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });

        intro.Play();
        comboManager.Configure(this);
        comboManager.onComboFinished += OnComboFinished;
        comboManager.onMatchCompleted += OnMatchCompleted;
        comboManager.onDoubleMatchCompleted += OnDoubleMatchCompleted;

        objectiveMet = false;
        objectiveRemaining = ObjectiveTarget;
        // El SaveManager puede no haber corrido su Start aún (orden de ejecución);
        // fallback a FindFirstObjectByType para no depender del orden.
        var saveManager = SaveManager.Instance != null
            ? SaveManager.Instance
            : UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        if (objectiveHudText)
        {
            if (saveManager && saveManager.IsShowTutorial() && dayObjective != DayObjective.None)
            {
                UpdateObjectiveHud();
            }
            else
            {
                // Vía-libre (o día sin objetivo): sin HUD de objetivo
                objectiveHudText.text = string.Empty;
            }
        }
    }

    private void OnComboFinished(int obj)
    {
        totalPoints += obj;
        UpdateScoreText();
    }

    private void OnMatchCompleted(ComboType type)
    {
        // El objetivo del día se cumple SOLO vía el contador (objectiveMet = objectiveRemaining <= 0).
        // Un match de cualquier tipo (ej. Casual en un día de VIP) ya NO marca objectiveMet:
        // solo cuenta el tipo exacto del objetivo, decrementado en CustomerAttended /
        // OnDoubleMatchCompleted. El reward/puntaje de los combos es aparte (OnComboFinished).
    }

    private void OnDoubleMatchCompleted()
    {
        // Un double match involucra customer + food: satisface DoubleMatch y
        // también los objetivos simples del tipo involucrado (spec lo acepta).
        // Guard: solo en tutorial. En vía-libre (CurrentDay >= 4) la escena
        // reusada TutorialDay_3 serializa DoubleMatch, pero NO hay objetivo:
        // IsShowTutorial() es el mismo semáforo que usan Start y endGame.
        if (objectiveRemaining <= 0) return;
        if (SaveManager.Instance == null || !SaveManager.Instance.IsShowTutorial()) return;
        objectiveRemaining = 0;
        objectiveMet = objectiveRemaining <= 0; // derivado SOLO del contador (invariante)
        UpdateObjectiveHud();
        OnObjectiveProgress?.Invoke(0);
    }

    private int ObjectiveTarget => dayObjective switch
    {
        DayObjective.CustomerMatch => 3,
        DayObjective.FoodMatch => 3,
        DayObjective.DoubleMatch => 1,
        _ => 0 // None/vía-libre → sin contador
    };

    private void DecrementObjective()
    {
        objectiveRemaining--;
        objectiveMet = objectiveRemaining <= 0;
        UpdateObjectiveHud();
        OnObjectiveProgress?.Invoke(objectiveRemaining);
    }

    private void ResetObjective()
    {
        if (objectiveRemaining == ObjectiveTarget) return; // sin cambio → sin evento
        objectiveRemaining = ObjectiveTarget;
        objectiveMet = false;
        UpdateObjectiveHud();
        OnObjectiveProgress?.Invoke(objectiveRemaining);
    }

    private void UpdateObjectiveHud()
    {
        if (!objectiveHudText) return;
        if (objectiveRemaining <= 0)
        {
            objectiveHudText.text = "Objetivo cumplido ✓";
            return;
        }
        string unit = dayObjective switch
        {
            DayObjective.CustomerMatch => "VIP",
            DayObjective.FoodMatch => objectiveRemaining == 1 ? "Bebida" : "Bebidas",
            DayObjective.DoubleMatch => "Double Match",
            _ => ""
        };
        objectiveHudText.text = $"Objetivo: {objectiveRemaining} {unit}";
    }

    /// <summary>
    /// Botón del panel "Continuar igual": resetea el streak y avanza de día
    /// como si hubiera pasado el objetivo, sin penalización.
    /// </summary>
    public void ContinueAnyway()
    {
        PlayerPrefs.SetInt("TutorialFailStreak", 0);
        PlayerPrefs.Save();
        if (continuePanel) continuePanel.SetActive(false);
        TutorialProgress.NextDay();
        endGameCinematic.PlayIntro();
        endGameCinematic.OnFinish += () =>
        {
            SceneManager.LoadScene(0);
        };
    }

    /// <summary>
    /// Botón del panel "Reintentar": recarga el mismo día SIN resetear el streak.
    /// Tras alcanzar el umbral, cada fallo vuelve a mostrar el panel
    /// (el jugador conserva la salida "Continuar" siempre disponible).
    /// </summary>
    public void RetryDay()
    {
        if (continuePanel) continuePanel.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void AddPoints(int points)
    {
        totalPoints += points;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText) scoreText.text = $"Puntos: {totalPoints}";
    }

    public void CustomerAttended(CustomerClientModel customer, FoodModel food)
    {
        // Lock: una vez cumplido (remaining <= 0) no se re-arma ni se sigue contando.
        // El lock envuelve SOLO la lógica del objetivo; RegisterServed queda intacto.
        // Guard IsShowTutorial: en vía-libre (CurrentDay >= 4) no hay objetivo aunque la
        // escena reusada serialize un dayObjective (mismo semáforo que Start y endGame).
        bool objectiveActive = SaveManager.Instance != null && SaveManager.Instance.IsShowTutorial();
        if (objectiveActive && objectiveRemaining > 0)
        {
            bool matches = dayObjective switch
            {
                DayObjective.CustomerMatch => customer.customerIdentify == CustomerIdentify.VIP,
                DayObjective.FoodMatch => food.foodModelType == FoodModelType.Bebida,
                _ => false
            };
            if (matches) DecrementObjective();
            else ResetObjective();
        }
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