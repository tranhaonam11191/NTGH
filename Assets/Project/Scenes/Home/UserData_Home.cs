using System.Collections.Generic;
using UnityEngine;

public partial class UserData
{
    #region Home
    public Dictionary<string, int> house_parts = new Dictionary<string, int>();
    public int outfit_rank = 1;
    public string current_outfit_id = string.Empty;
    public List<string> unlocked_outfit_ids = new List<string>();
    public List<string> unlocked_house_items = new List<string>();
    public Dictionary<string, int> customize_ad_progress = new Dictionary<string, int>();

    public int GetHousePartId(HomeHouseTheme.Part part, int defaultId = 1)
    {
        EnsureHomeDefaults();
        string key = part.ToString();
        if (house_parts.TryGetValue(key, out int value))
        {
            return value;
        }

        return defaultId;
    }

    public void SetHousePartId(HomeHouseTheme.Part part, int partId)
    {
        EnsureHomeDefaults();
        int clamped = Mathf.Max(1, partId);
        house_parts[part.ToString()] = clamped;
        UnlockHouseItem(part, (HomeHouseTheme.Level)Mathf.Clamp(clamped, (int)HomeHouseTheme.Level.Dilapidated, (int)HomeHouseTheme.Level.Renovated));
    }

    public bool IsOutfitUnlocked(string outfitId)
    {
        if (string.IsNullOrEmpty(outfitId))
            return false;

        EnsureHomeDefaults();
        return unlocked_outfit_ids.Contains(outfitId);
    }

    public bool UnlockOutfit(string outfitId)
    {
        if (string.IsNullOrEmpty(outfitId))
            return false;

        EnsureHomeDefaults();
        if (unlocked_outfit_ids.Contains(outfitId))
            return false;

        unlocked_outfit_ids.Add(outfitId);
        return true;
    }

    public static string GetHouseItemKey(HomeHouseTheme.Part part, HomeHouseTheme.Level level)
    {
        return $"{part}_{(int)level}";
    }

    public bool IsHouseItemUnlocked(HomeHouseTheme.Part part, HomeHouseTheme.Level level)
    {
        if (level == HomeHouseTheme.Level.Dilapidated)
            return true;

        EnsureHomeDefaults();
        string key = GetHouseItemKey(part, level);
        if (unlocked_house_items.Contains(key))
            return true;

        // Backward compatibility: if player already has this or higher level equipped.
        int equipped = GetHousePartId(part, (int)HomeHouseTheme.Level.Dilapidated);
        return equipped >= (int)level;
    }

    public bool UnlockHouseItem(HomeHouseTheme.Part part, HomeHouseTheme.Level level)
    {
        EnsureHomeDefaults();
        string key = GetHouseItemKey(part, level);
        if (unlocked_house_items.Contains(key))
            return false;

        unlocked_house_items.Add(key);
        return true;
    }

    public int GetCustomizeAdProgress(string progressKey)
    {
        if (string.IsNullOrEmpty(progressKey))
            return 0;

        EnsureHomeDefaults();
        return customize_ad_progress.TryGetValue(progressKey, out int count) ? Mathf.Max(0, count) : 0;
    }

    public int IncrementCustomizeAdProgress(string progressKey)
    {
        if (string.IsNullOrEmpty(progressKey))
            return 0;

        EnsureHomeDefaults();
        int next = GetCustomizeAdProgress(progressKey) + 1;
        customize_ad_progress[progressKey] = next;
        return next;
    }

    public bool EnsureHomeDefaults()
    {
        bool changed = false;

        if (house_parts == null)
        {
            house_parts = new Dictionary<string, int>();
            changed = true;
        }

        if (unlocked_outfit_ids == null)
        {
            unlocked_outfit_ids = new List<string>();
            changed = true;
        }

        if (unlocked_house_items == null)
        {
            unlocked_house_items = new List<string>();
            changed = true;
        }

        if (customize_ad_progress == null)
        {
            customize_ad_progress = new Dictionary<string, int>();
            changed = true;
        }

        for (HomeHouseTheme.Part part = HomeHouseTheme.Part.Awning; part <= HomeHouseTheme.Part.Wall; part++)
        {
            string key = part.ToString();
            if (!house_parts.ContainsKey(key))
            {
                house_parts[key] = (int)HomeHouseTheme.Level.Dilapidated;
                changed = true;
            }

            string baseUnlockKey = GetHouseItemKey(part, HomeHouseTheme.Level.Dilapidated);
            if (!unlocked_house_items.Contains(baseUnlockKey))
            {
                unlocked_house_items.Add(baseUnlockKey);
                changed = true;
            }
        }

        if (outfit_rank < 1)
        {
            outfit_rank = 1;
            changed = true;
        }

        if (current_outfit_id == null)
        {
            current_outfit_id = string.Empty;
            changed = true;
        }

        return changed;
    }
    #endregion
}
