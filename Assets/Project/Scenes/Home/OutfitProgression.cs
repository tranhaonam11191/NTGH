using System;
using System.Collections.Generic;
using NTGH.Demo.OutfitSystem;
using UnityEngine;

public sealed class OutfitProgression
{
    private readonly IReadOnlyList<OutfitData> m_outfits;

    public event Action<int> RankChanged;
    public event Action<string> OutfitChanged;

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

        // Ensure current_outfit_id is populated for new or migrated saves.
        if (string.IsNullOrEmpty(User.data.current_outfit_id) || FindOutfitIndexById(User.data.current_outfit_id) < 0)
        {
            OutfitData defaultOutfit = m_outfits[CurrentRank - 1];
            if (defaultOutfit != null && !string.IsNullOrEmpty(defaultOutfit.OutfitId))
            {
                User.data.current_outfit_id = defaultOutfit.OutfitId;
                User.Save();
            }
        }
    }

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
        if (index < 0 || index >= m_outfits.Count || index >= CurrentRank)
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
