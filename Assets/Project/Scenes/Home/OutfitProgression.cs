using System;
using System.Collections.Generic;
using NTGH.Demo.OutfitSystem;
using UnityEngine;

public sealed class OutfitProgression
{
    private readonly IReadOnlyList<OutfitData> m_outfits;

    public event Action<int> RankChanged;
    public event Action<string> OutfitChanged;

    public IReadOnlyList<OutfitData> Outfits => m_outfits;

    public int CurrentRank
    {
        get
        {
            User.EnsureLoaded();
            return Mathf.Clamp(User.data.outfit_rank, 1, m_outfits.Count);
        }
    }

    public string CurrentOutfitId
    {
        get
        {
            User.EnsureLoaded();
            string savedId = User.data.current_outfit_id;
            if (!string.IsNullOrEmpty(savedId) && FindOutfitIndexById(savedId) >= 0)
            {
                return savedId;
            }

            OutfitData rankOutfit = m_outfits[CurrentRank - 1];
            return rankOutfit != null ? rankOutfit.OutfitId : string.Empty;
        }
    }

    public int CurrentOutfitIndex
    {
        get
        {
            int index = FindOutfitIndexById(CurrentOutfitId);
            return index >= 0 ? index : CurrentRank - 1;
        }
    }

    public OutfitData NextOutfit => CurrentRank < m_outfits.Count ? m_outfits[CurrentRank] : null;

    public OutfitProgression(IReadOnlyList<OutfitData> outfits)
    {
        if (outfits == null || outfits.Count == 0)
            throw new ArgumentException("At least one outfit is required.", nameof(outfits));

        m_outfits = outfits;
        User.EnsureLoaded();

        bool changed = false;

        // Sync existing rank-based and free unlocks into unlocked_outfit_ids.
        int unlockedRank = CurrentRank;
        for (int i = 0; i < m_outfits.Count; i++)
        {
            OutfitData outfit = m_outfits[i];
            if (outfit == null || string.IsNullOrEmpty(outfit.OutfitId))
                continue;

            if (i < unlockedRank || outfit.UnlockMethod == CustomizeUnlockMethod.Free)
            {
                if (User.data.UnlockOutfit(outfit.OutfitId))
                    changed = true;
            }
        }

        // Ensure current_outfit_id is populated for new or migrated saves.
        if (string.IsNullOrEmpty(User.data.current_outfit_id) || FindOutfitIndexById(User.data.current_outfit_id) < 0)
        {
            OutfitData defaultOutfit = m_outfits[CurrentRank - 1];
            if (defaultOutfit != null && !string.IsNullOrEmpty(defaultOutfit.OutfitId))
            {
                User.data.current_outfit_id = defaultOutfit.OutfitId;
                changed = true;
            }
        }

        if (changed)
            User.Save();
    }

    public bool IsUnlocked(int index)
    {
        if (index < 0 || index >= m_outfits.Count)
            return false;

        return IsUnlocked(m_outfits[index]);
    }

    public bool IsUnlocked(OutfitData outfit)
    {
        if (outfit == null)
            return false;

        if (outfit.UnlockMethod == CustomizeUnlockMethod.Free)
            return true;

        User.EnsureLoaded();
        if (!string.IsNullOrEmpty(outfit.OutfitId) && User.data.IsOutfitUnlocked(outfit.OutfitId))
            return true;

        int index = FindOutfitIndexById(outfit.OutfitId);
        return index >= 0 && index < CurrentRank;
    }

    public int GetAdProgress(OutfitData outfit)
    {
        if (outfit == null || string.IsNullOrEmpty(outfit.OutfitId))
            return 0;

        User.EnsureLoaded();
        return User.data.GetCustomizeAdProgress(GetAdProgressKey(outfit.OutfitId));
    }

    public bool TryUnlockWithGold(int index, bool autoEquip = true)
    {
        if (index < 0 || index >= m_outfits.Count)
            return false;

        OutfitData outfit = m_outfits[index];
        if (outfit == null || IsUnlocked(outfit))
            return false;

        User.EnsureLoaded();
        int price = Mathf.Max(0, outfit.PriceGold);
        if (!User.data.TrySpendGold(price))
            return false;

        UnlockAndOptionallyEquip(index, outfit, autoEquip);
        return true;
    }

    public bool TryRecordAdWatch(int index, out bool unlockedNow, bool autoEquip = true)
    {
        unlockedNow = false;
        if (index < 0 || index >= m_outfits.Count)
            return false;

        OutfitData outfit = m_outfits[index];
        if (outfit == null || IsUnlocked(outfit))
            return false;

        User.EnsureLoaded();
        string key = GetAdProgressKey(outfit.OutfitId);
        int watched = User.data.IncrementCustomizeAdProgress(key);
        if (watched >= outfit.AdsRequired)
        {
            unlockedNow = true;
            UnlockAndOptionallyEquip(index, outfit, autoEquip);
        }
        else
        {
            User.Save();
        }

        return true;
    }

    private void UnlockAndOptionallyEquip(int index, OutfitData outfit, bool autoEquip)
    {
        if (!string.IsNullOrEmpty(outfit.OutfitId))
            User.data.UnlockOutfit(outfit.OutfitId);

        bool rankUpdated = false;
        if (index + 1 > User.data.outfit_rank)
        {
            User.data.outfit_rank = index + 1;
            rankUpdated = true;
        }

        if (autoEquip && !string.IsNullOrEmpty(outfit.OutfitId))
        {
            User.data.current_outfit_id = outfit.OutfitId;
        }

        User.Save();

        if (rankUpdated)
            RankChanged?.Invoke(User.data.outfit_rank);

        if (autoEquip)
            OutfitChanged?.Invoke(CurrentOutfitId);
    }

    private static string GetAdProgressKey(string outfitId) => $"outfit_{outfitId}";

    public bool TryUpgrade()
    {
        User.EnsureLoaded();
        int rank = CurrentRank;
        if (rank >= m_outfits.Count)
            return false;

        OutfitData next = m_outfits[rank];
        if (next == null || next.PriceGold < 0 || !User.data.TrySpendGold(next.PriceGold))
            return false;

        User.data.outfit_rank = rank + 1;
        if (!string.IsNullOrEmpty(next.OutfitId))
        {
            User.data.UnlockOutfit(next.OutfitId);
            User.data.current_outfit_id = next.OutfitId;
        }

        User.Save();
        RankChanged?.Invoke(rank + 1);
        OutfitChanged?.Invoke(CurrentOutfitId);
        return true;
    }

    public bool EquipOutfit(string outfitId)
    {
        int index = FindOutfitIndexById(outfitId);
        if (index < 0)
            return false;

        return EquipOutfitByIndex(index);
    }

    public bool EquipOutfitByIndex(int index)
    {
        if (index < 0 || index >= m_outfits.Count || !IsUnlocked(index))
            return false;

        OutfitData outfit = m_outfits[index];
        if (outfit == null)
            return false;

        User.EnsureLoaded();
        User.data.current_outfit_id = outfit.OutfitId ?? string.Empty;
        User.Save();
        OutfitChanged?.Invoke(User.data.current_outfit_id);
        return true;
    }

    private int FindOutfitIndexById(string outfitId)
    {
        if (string.IsNullOrEmpty(outfitId))
            return -1;

        for (int i = 0; i < m_outfits.Count; i++)
        {
            if (m_outfits[i] != null && m_outfits[i].OutfitId == outfitId)
                return i;
        }

        return -1;
    }
}
