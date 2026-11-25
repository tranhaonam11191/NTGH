using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NTGH.Boosters;
using UnityEngine;

public partial class UserData
{
    #region Boosters
    [JsonProperty("booster_counts")]
    public Dictionary<string, int> booster_counts = new Dictionary<string, int>();

    [JsonProperty("booster_granted_ids")]
    public List<string> booster_granted_ids = new List<string>();

    public void EnsureBoosterStorage()
    {
        if (booster_counts == null)
        {
            booster_counts = new Dictionary<string, int>();
        }

        if (booster_granted_ids == null)
        {
            booster_granted_ids = new List<string>();
        }
    }

    public static string GetBoosterKey(BoosterType type)
    {
        return type.ToString();
    }

    public int GetBoosterCount(BoosterType type)
    {
        EnsureBoosterStorage();
        string key = GetBoosterKey(type);
        return booster_counts.TryGetValue(key, out int count) ? Mathf.Max(0, count) : 0;
    }

    public int AddBooster(BoosterType type, int amount)
    {
        if (amount <= 0)
        {
            return GetBoosterCount(type);
        }

        EnsureBoosterStorage();
        string key = GetBoosterKey(type);
        int current = booster_counts.TryGetValue(key, out int existing) ? Mathf.Max(0, existing) : 0;
        int next = current + amount;
        booster_counts[key] = next;
        User.NotifyBoosterCountChanged(type, next);
        return next;
    }

    public bool TryConsumeBooster(BoosterType type)
    {
        EnsureBoosterStorage();
        string key = GetBoosterKey(type);
        int current = booster_counts.TryGetValue(key, out int existing) ? Mathf.Max(0, existing) : 0;
        if (current <= 0)
        {
            return false;
        }

        int next = current - 1;
        booster_counts[key] = next;
        User.NotifyBoosterCountChanged(type, next);
        return true;
    }

    /// <summary>
    /// Grants initial free charges once when the player reaches a booster's unlock level.
    /// Returns true if persisted state changed.
    /// </summary>
    public bool EnsureBoosterDefaults(BoosterDatabaseSO database, int currentLevelNumber)
    {
        EnsureBoosterStorage();
        if (database == null || database.Boosters == null)
        {
            return false;
        }

        bool changed = false;
        int levelNum = Mathf.Max(1, currentLevelNumber);

        for (int i = 0; i < database.Boosters.Count; i++)
        {
            BoosterConfig config = database.Boosters[i];
            if (config == null || !config.IsUnlockedAtLevel(levelNum))
            {
                continue;
            }

            string key = GetBoosterKey(config.type);
            if (!booster_granted_ids.Contains(key))
            {
                booster_granted_ids.Add(key);
                int starter = Mathf.Max(0, config.initialFreeCount);
                if (starter > 0)
                {
                    int current = booster_counts.TryGetValue(key, out int existing) ? Mathf.Max(0, existing) : 0;
                    int next = current + starter;
                    booster_counts[key] = next;
                    User.NotifyBoosterCountChanged(config.type, next);
                }
                else if (!booster_counts.ContainsKey(key))
                {
                    booster_counts[key] = 0;
                }

                changed = true;
            }
        }

        return changed;
    }
    #endregion
}

public static partial class User
{
    public static event Action<BoosterType, int> OnBoosterCountChanged;

    internal static void NotifyBoosterCountChanged(BoosterType type, int count)
    {
        OnBoosterCountChanged?.Invoke(type, count);
    }
}
