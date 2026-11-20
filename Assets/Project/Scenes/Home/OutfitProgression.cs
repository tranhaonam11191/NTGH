using System;
using System.Collections.Generic;
using NTGH.Demo.OutfitSystem;
using UnityEngine;

public sealed class OutfitProgression
{
    private const string RankKey = "DauBu.OutfitRank";
    private readonly IReadOnlyList<OutfitData> m_outfits;

    public event Action<int> RankChanged;
    public int CurrentRank => Mathf.Clamp(PlayerPrefs.GetInt(RankKey, 1), 1, m_outfits.Count);
    public OutfitData NextOutfit => CurrentRank < m_outfits.Count ? m_outfits[CurrentRank] : null;

    public OutfitProgression(IReadOnlyList<OutfitData> outfits)
    {
        if (outfits == null || outfits.Count == 0)
            throw new ArgumentException("At least one outfit is required.", nameof(outfits));

        m_outfits = outfits;
    }

    public bool TryUpgrade()
    {
        int rank = CurrentRank;
        if (rank >= m_outfits.Count)
            return false;

        OutfitData next = m_outfits[rank];
        int gold = PlayerPrefs.GetInt(Const.GOLD);
        if (next == null || next.PriceGold < 0 || gold < next.PriceGold)
            return false;

        PlayerPrefs.SetInt(Const.GOLD, gold - next.PriceGold);
        PlayerPrefs.SetInt(RankKey, rank + 1);
        PlayerPrefs.Save();
        RankChanged?.Invoke(rank + 1);
        return true;
    }
}
