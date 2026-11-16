using UnityEngine;

/// <summary>
/// GameDifficulty — Sistem Tingkat Kesulitan Permainan BATH:
/// - EASY: Santai & Bersahabat (Musuh HP 75%, ATK 70%)
/// - NORMAL: Pengalaman Standar Dark Fantasy BATH (Musuh HP 100%, ATK 100%)
/// - HARD: Tantangan Ksatria Tangguh (Musuh HP 145%, ATK 135%)
/// - ABYSS: Neraka Kegelapan Abyssal Tanpa Ampun (Musuh HP 220%, ATK 185%)
/// </summary>
public enum DifficultyLevel
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Abyss = 3
}

public static class DifficultyManager
{
    private const string PrefKey = "GameDifficultyLevel";

    public static DifficultyLevel CurrentDifficulty
    {
        get => (DifficultyLevel)PlayerPrefs.GetInt(PrefKey, (int)DifficultyLevel.Normal);
        set
        {
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.SetString("GameDifficultyName", GetDifficultyTitle(value));
            PlayerPrefs.Save();
        }
    }

    public static float GetEnemyHealthMultiplier()
    {
        switch (CurrentDifficulty)
        {
            case DifficultyLevel.Easy:   return 0.75f;
            case DifficultyLevel.Normal: return 1.00f;
            case DifficultyLevel.Hard:   return 1.45f;
            case DifficultyLevel.Abyss:  return 2.20f;
            default:                     return 1.00f;
        }
    }

    public static float GetEnemyDamageMultiplier()
    {
        switch (CurrentDifficulty)
        {
            case DifficultyLevel.Easy:   return 0.70f;
            case DifficultyLevel.Normal: return 1.00f;
            case DifficultyLevel.Hard:   return 1.35f;
            case DifficultyLevel.Abyss:  return 1.85f;
            default:                     return 1.00f;
        }
    }

    public static string GetDifficultyTitle(DifficultyLevel diff)
    {
        switch (diff)
        {
            case DifficultyLevel.Easy:   return "EASY";
            case DifficultyLevel.Normal: return "NORMAL";
            case DifficultyLevel.Hard:   return "HARD";
            case DifficultyLevel.Abyss:  return "ABYSS";
            default:                     return "NORMAL";
        }
    }

    public static Color GetDifficultyColor(DifficultyLevel diff)
    {
        switch (diff)
        {
            case DifficultyLevel.Easy:   return new Color(0.35f, 0.90f, 0.50f, 1f); // Hijau Emerald
            case DifficultyLevel.Normal: return new Color(0.35f, 0.75f, 1.00f, 1f); // Biru Langit
            case DifficultyLevel.Hard:   return new Color(1.00f, 0.50f, 0.15f, 1f); // Oranye Menyala
            case DifficultyLevel.Abyss:  return new Color(0.85f, 0.25f, 1.00f, 1f); // Ungu Abyssal
            default:                     return Color.white;
        }
    }

    public static string GetDifficultyDesc(DifficultyLevel diff)
    {
        switch (diff)
        {
            case DifficultyLevel.Easy:
                return "Santai & Bersahabat  •  Musuh HP -25%, ATK -30%";
            case DifficultyLevel.Normal:
                return "Pengalaman Standar BATH  •  Musuh HP 100%, ATK 100%";
            case DifficultyLevel.Hard:
                return "Tantangan Ksatria Tangguh  •  Musuh HP +45%, ATK +35%";
            case DifficultyLevel.Abyss:
                return "Neraka Abyssal Tanpa Ampun!  •  Musuh HP +120%, ATK +85%";
            default:
                return "";
        }
    }
}
