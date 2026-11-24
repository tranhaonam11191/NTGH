using System.Collections.Generic;
using NTGH.Teammates;

public partial class UserData
{
    public const int MaxTeammateSlots = TeammateCardDatabaseSO.MaxSquadSlots;

    #region Teammates
    public List<string> unlocked_teammate_ids = new List<string>();
    public List<string> equipped_teammate_ids = new List<string>();
    public List<string> unseen_teammate_ids = new List<string>();

    public bool IsTeammateUnlocked(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        EnsureTeammateDefaults();
        return unlocked_teammate_ids.Contains(cardId);
    }

    public bool UnlockTeammate(string cardId, bool markAsNew = false)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        EnsureTeammateDefaults();
        if (unlocked_teammate_ids.Contains(cardId)) return false;

        unlocked_teammate_ids.Add(cardId);
        if (markAsNew)
        {
            MarkTeammateAsNew(cardId);
        }
        return true;
    }

    public bool IsTeammateNew(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        EnsureTeammateDefaults();
        return unseen_teammate_ids.Contains(cardId);
    }

    public bool MarkTeammateAsNew(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        EnsureTeammateDefaults();
        if (unseen_teammate_ids.Contains(cardId)) return false;

        unseen_teammate_ids.Add(cardId);
        return true;
    }

    public bool ClearNewTeammate(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        EnsureTeammateDefaults();
        return unseen_teammate_ids.Remove(cardId);
    }

    public bool ClearAllNewTeammates()
    {
        EnsureTeammateDefaults();
        if (unseen_teammate_ids.Count == 0) return false;
        unseen_teammate_ids.Clear();
        return true;
    }

    public bool IsTeammateEquipped(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        EnsureTeammateDefaults();
        return equipped_teammate_ids.Contains(cardId);
    }

    public int GetEquippedTeammateSlot(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return -1;
        EnsureTeammateDefaults();
        for (int i = 0; i < equipped_teammate_ids.Count; i++)
        {
            if (equipped_teammate_ids[i] == cardId) return i;
        }
        return -1;
    }

    public string GetEquippedTeammateAt(int slotIndex)
    {
        EnsureTeammateDefaults();
        if (slotIndex < 0 || slotIndex >= equipped_teammate_ids.Count) return string.Empty;
        return equipped_teammate_ids[slotIndex] ?? string.Empty;
    }

    public int GetEquippedTeammateCount()
    {
        EnsureTeammateDefaults();
        int count = 0;
        for (int i = 0; i < equipped_teammate_ids.Count; i++)
        {
            if (!string.IsNullOrEmpty(equipped_teammate_ids[i])) count++;
        }
        return count;
    }

    public bool TryEquipTeammate(string cardId, int preferredSlot = -1)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        EnsureTeammateDefaults();
        if (!IsTeammateUnlocked(cardId)) return false;

        // Already equipped
        if (IsTeammateEquipped(cardId)) return true;

        if (preferredSlot >= 0 && preferredSlot < MaxTeammateSlots)
        {
            equipped_teammate_ids[preferredSlot] = cardId;
            return true;
        }

        for (int i = 0; i < MaxTeammateSlots; i++)
        {
            if (string.IsNullOrEmpty(equipped_teammate_ids[i]))
            {
                equipped_teammate_ids[i] = cardId;
                return true;
            }
        }

        return false; // All 4 slots full
    }

    public bool UnequipTeammate(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return false;
        EnsureTeammateDefaults();
        bool removed = false;
        for (int i = 0; i < equipped_teammate_ids.Count; i++)
        {
            if (equipped_teammate_ids[i] == cardId)
            {
                equipped_teammate_ids[i] = string.Empty;
                removed = true;
            }
        }
        return removed;
    }

    public bool UnequipTeammateAt(int slotIndex)
    {
        EnsureTeammateDefaults();
        if (slotIndex < 0 || slotIndex >= equipped_teammate_ids.Count) return false;
        if (string.IsNullOrEmpty(equipped_teammate_ids[slotIndex])) return false;

        equipped_teammate_ids[slotIndex] = string.Empty;
        return true;
    }

    public bool EnsureTeammateDefaults(TeammateCardDatabaseSO database = null)
    {
        bool changed = false;

        if (unlocked_teammate_ids == null)
        {
            unlocked_teammate_ids = new List<string>();
            changed = true;
        }

        if (equipped_teammate_ids == null)
        {
            equipped_teammate_ids = new List<string>();
            changed = true;
        }

        if (unseen_teammate_ids == null)
        {
            unseen_teammate_ids = new List<string>();
            changed = true;
        }

        while (equipped_teammate_ids.Count < MaxTeammateSlots)
        {
            equipped_teammate_ids.Add(string.Empty);
            changed = true;
        }

        if (equipped_teammate_ids.Count > MaxTeammateSlots)
        {
            equipped_teammate_ids.RemoveRange(MaxTeammateSlots, equipped_teammate_ids.Count - MaxTeammateSlots);
            changed = true;
        }

        if (database != null)
        {
            bool hadAnyUnlocked = unlocked_teammate_ids.Count > 0;
            var defaults = database.GetDefaultUnlockedCards();
            for (int i = 0; i < defaults.Count; i++)
            {
                var card = defaults[i];
                if (card == null || string.IsNullOrEmpty(card.Id)) continue;
                if (!unlocked_teammate_ids.Contains(card.Id))
                {
                    unlocked_teammate_ids.Add(card.Id);
                    changed = true;
                }
                if (!hadAnyUnlocked && GetEquippedTeammateCount() < MaxTeammateSlots && !IsTeammateEquipped(card.Id))
                {
                    TryEquipTeammate(card.Id);
                    changed = true;
                }
            }
        }

        return changed;
    }
    #endregion
}
