using UnityEngine;

public static class TutorialProgress
{
    private const string TutorialDayKey = "TutorialDay";

    public static int CurrentDay
    {
        get => PlayerPrefs.GetInt(TutorialDayKey, 0);
        set => PlayerPrefs.SetInt(TutorialDayKey, value);
    }

    public static bool IsCompleted =>
        CurrentDay <= 3;

    public static void NextDay()
    {
        CurrentDay++;
    }
}