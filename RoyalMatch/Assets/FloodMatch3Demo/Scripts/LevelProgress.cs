using UnityEngine;

public static class LevelProgress
{
    private const string HighestUnlockedKey = "FloodMatch3_HighestUnlockedLevel";

    public static int HighestUnlockedLevel
    {
        get => Mathf.Max(0, PlayerPrefs.GetInt(HighestUnlockedKey, 0));
        private set
        {
            PlayerPrefs.SetInt(HighestUnlockedKey, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    public static bool IsUnlocked(int levelIndex)
    {
        return levelIndex <= HighestUnlockedLevel;
    }

    public static void UnlockNext(int completedLevelIndex, int totalLevels)
    {
        int next = Mathf.Clamp(completedLevelIndex + 1, 0, Mathf.Max(0, totalLevels - 1));
        if (next > HighestUnlockedLevel)
            HighestUnlockedLevel = next;
    }
}
