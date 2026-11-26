using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public partial class UserData
{
    #region Currency
    int _gold = 0;

    [JsonProperty]
    public int gold
    {
        get => _gold;
        protected set
        {
            int clamped = Mathf.Max(0, value);
            if (_gold == clamped)
            {
                return;
            }

            _gold = clamped;
            User.NotifyGoldChanged(_gold);
        }
    }

    public void GoldTransaction(int amount)
    {
        gold += amount;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        gold += amount;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || _gold < amount)
        {
            return false;
        }

        gold -= amount;
        return true;
    }
    #endregion

    #region Progression
    public int exp = 0;
    public int level = 0;

    public void AddExp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        exp += amount;
    }
    #endregion

    #region Inventory
    public string current_weapon_id = string.Empty;
    public List<string> unlocked_item_ids = new List<string>();

    public bool IsItemUnlocked(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        EnsureInventoryInitialized();
        return unlocked_item_ids.Contains(itemId);
    }

    public bool UnlockItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        EnsureInventoryInitialized();
        if (unlocked_item_ids.Contains(itemId))
        {
            return false;
        }

        unlocked_item_ids.Add(itemId);
        return true;
    }

    public void SetCurrentWeapon(string weaponId)
    {
        current_weapon_id = weaponId ?? string.Empty;
    }

    public void EnsureInventoryInitialized()
    {
        if (unlocked_item_ids == null)
        {
            unlocked_item_ids = new List<string>();
        }

        if (current_weapon_id == null)
        {
            current_weapon_id = string.Empty;
        }
    }
    #endregion

    #region Migration
    public bool legacy_migrated = false;
    #endregion

    #region Watched Conversations
    public List<string> watched_conversations = new List<string>();

    public bool IsConversationWatched(string conversationKey)
    {
        if (string.IsNullOrEmpty(conversationKey))
        {
            return false;
        }

        EnsureConversationsInitialized();
        return watched_conversations.Contains(conversationKey);
    }

    public bool MarkConversationWatched(string conversationKey)
    {
        if (string.IsNullOrEmpty(conversationKey))
        {
            return false;
        }

        EnsureConversationsInitialized();
        if (watched_conversations.Contains(conversationKey))
        {
            return false;
        }

        watched_conversations.Add(conversationKey);
        return true;
    }

    public bool MarkConversationsWatched(IEnumerable<string> conversationKeys)
    {
        if (conversationKeys == null)
        {
            return false;
        }

        bool changed = false;
        foreach (string key in conversationKeys)
        {
            if (MarkConversationWatched(key))
            {
                changed = true;
            }
        }

        return changed;
    }

    public void EnsureConversationsInitialized()
    {
        if (watched_conversations == null)
        {
            watched_conversations = new List<string>();
        }
    }
    #endregion
}

public static partial class User
{
    public static event Action<int> OnGoldChanged;

    internal static void NotifyGoldChanged(int gold)
    {
        OnGoldChanged?.Invoke(gold);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureLoaded();
        ResolveAbandonedBattleHeartAndSave();
    }

    public static void EnsureLoaded()
    {
        if (data == null)
        {
            Load();
        }

        MigrateLegacyPlayerPrefs();
    }

    public static void ResetData()
    {
        PlayerPrefs.DeleteAll();
        data = new UserData
        {
            legacy_migrated = true
        };
        data.EnsureInventoryInitialized();
        data.EnsureConversationsInitialized();
        data.EnsureHomeDefaults();
        data.EnsureHeartDefaults();
        data.EnsureBoosterStorage();
        Save();
        NotifyGoldChanged(data.gold);
        NotifyHeartsChanged(data.hearts);
    }

    public static void MigrateLegacyPlayerPrefs()
    {
        if (data == null)
        {
            return;
        }

        bool changed = false;

        data.EnsureInventoryInitialized();
        data.EnsureConversationsInitialized();
        data.EnsureBoosterStorage();
        if (data.EnsureHomeDefaults())
        {
            changed = true;
        }

        if (data.EnsureHeartDefaults())
        {
            changed = true;
        }

        if (!data.legacy_migrated)
        {
            if (PlayerPrefs.HasKey(Const.GOLD))
            {
                data.GoldTransaction(PlayerPrefs.GetInt(Const.GOLD, 0) - data.gold);
            }

            if (PlayerPrefs.HasKey(Const.EXP))
            {
                data.exp = PlayerPrefs.GetInt(Const.EXP, 0);
            }

            if (PlayerPrefs.HasKey(Const.NUM_STORY))
            {
                data.level = Mathf.Max(0, PlayerPrefs.GetInt(Const.NUM_STORY, 0));
            }

            if (PlayerPrefs.HasKey(Const.CURRENTWEAPON))
            {
                string legacyWeapon = PlayerPrefs.GetString(Const.CURRENTWEAPON, string.Empty);
                data.SetCurrentWeapon(legacyWeapon);
                if (!string.IsNullOrEmpty(legacyWeapon))
                {
                    data.UnlockItem(legacyWeapon);
                }
            }

            for (HomeHouseTheme.Part part = HomeHouseTheme.Part.Awning; part <= HomeHouseTheme.Part.Wall; part++)
            {
                string legacyHouseKey = "Home.House." + part;
                if (PlayerPrefs.HasKey(legacyHouseKey))
                {
                    data.SetHousePartId(part, Mathf.Clamp(PlayerPrefs.GetInt(legacyHouseKey, 1), 1, 3));
                }
            }

            if (PlayerPrefs.HasKey("DauBu.OutfitRank"))
            {
                data.outfit_rank = Mathf.Max(1, PlayerPrefs.GetInt("DauBu.OutfitRank", 1));
            }

            if (PlayerPrefs.HasKey("Demo_CurrentOutfitId"))
            {
                data.current_outfit_id = PlayerPrefs.GetString("Demo_CurrentOutfitId", string.Empty);
            }

            if (PlayerPrefs.HasKey("BGM_VOLUME"))
            {
                data.bgm_volume = PlayerPrefs.GetFloat("BGM_VOLUME", 1f);
            }

            if (PlayerPrefs.HasKey("SFX_VOLUME"))
            {
                data.sfx_volume = PlayerPrefs.GetFloat("SFX_VOLUME", 1f);
            }

            data.legacy_migrated = true;
            changed = true;
        }

        if (changed)
        {
            Save();
        }
    }

    public static void MigrateLegacyUnlockedItems(IEnumerable<string> knownItemIds)
    {
        if (knownItemIds == null)
        {
            return;
        }

        EnsureLoaded();
        bool changed = false;

        foreach (string itemId in knownItemIds)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                continue;
            }

            if (PlayerPrefs.HasKey(itemId))
            {
                if (data.UnlockItem(itemId))
                {
                    changed = true;
                }

                PlayerPrefs.DeleteKey(itemId);
            }
        }

        if (changed)
        {
            Save();
        }
    }
}
