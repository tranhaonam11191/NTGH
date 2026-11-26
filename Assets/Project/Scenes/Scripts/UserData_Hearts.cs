using System;
using Newtonsoft.Json;
using UnityEngine;

public partial class UserData
{
    #region Hearts
    public const int MAX_HEARTS = 5;
    public const int HEART_REGEN_SECONDS = 60 * 60; // 60 minutes per heart

    int _hearts = MAX_HEARTS;
    long _heart_regen_start_utc_ticks = 0;
    bool _pending_battle_heart = false;
    long _pending_battle_utc_ticks = 0;

    [JsonProperty("hearts")]
    public int hearts
    {
        get => _hearts;
        protected set
        {
            int clamped = Mathf.Clamp(value, 0, MAX_HEARTS);
            if (_hearts == clamped)
            {
                return;
            }

            _hearts = clamped;
            User.NotifyHeartsChanged(_hearts);
        }
    }

    [JsonProperty("heart_regen_start_utc_ticks")]
    public long heart_regen_start_utc_ticks
    {
        get => _heart_regen_start_utc_ticks;
        protected set => _heart_regen_start_utc_ticks = value < 0 ? 0 : value;
    }

    [JsonProperty("pending_battle_heart")]
    public bool pending_battle_heart
    {
        get => _pending_battle_heart;
        protected set => _pending_battle_heart = value;
    }

    [JsonProperty("pending_battle_utc_ticks")]
    public long pending_battle_utc_ticks
    {
        get => _pending_battle_utc_ticks;
        protected set => _pending_battle_utc_ticks = value < 0 ? 0 : value;
    }

    public bool IsHeartsFull()
    {
        RefreshHearts();
        return _hearts >= MAX_HEARTS;
    }

    public bool HasHeart()
    {
        RefreshHearts();
        return _hearts > 0;
    }

    /// <summary>
    /// Updates heart count based on elapsed UTC time since regeneration started.
    /// Returns true if persisted state changed and should be saved.
    /// </summary>
    public bool RefreshHearts(DateTime? utcNowOverride = null)
    {
        DateTime now = utcNowOverride ?? DateTime.UtcNow;
        bool changed = false;

        if (_hearts > MAX_HEARTS)
        {
            _hearts = MAX_HEARTS;
            changed = true;
        }
        else if (_hearts < 0)
        {
            _hearts = 0;
            changed = true;
        }

        if (_hearts >= MAX_HEARTS)
        {
            if (_heart_regen_start_utc_ticks != 0)
            {
                _heart_regen_start_utc_ticks = 0;
                changed = true;
            }

            return changed;
        }

        if (_heart_regen_start_utc_ticks <= 0)
        {
            _heart_regen_start_utc_ticks = now.Ticks;
            return true;
        }

        DateTime startUtc = new DateTime(_heart_regen_start_utc_ticks, DateTimeKind.Utc);
        double elapsedSeconds = (now - startUtc).TotalSeconds;

        // Guard against device clock moving backwards
        if (elapsedSeconds < 0d)
        {
            _heart_regen_start_utc_ticks = now.Ticks;
            return true;
        }

        if (elapsedSeconds >= HEART_REGEN_SECONDS)
        {
            int recovered = (int)(elapsedSeconds / HEART_REGEN_SECONDS);
            int previousHearts = _hearts;
            _hearts = Mathf.Min(MAX_HEARTS, _hearts + recovered);

            if (_hearts >= MAX_HEARTS)
            {
                _heart_regen_start_utc_ticks = 0;
            }
            else
            {
                _heart_regen_start_utc_ticks = startUtc.AddSeconds((long)recovered * HEART_REGEN_SECONDS).Ticks;
            }

            changed = true;
            if (_hearts != previousHearts)
            {
                User.NotifyHeartsChanged(_hearts);
            }
        }

        return changed;
    }

    /// <summary>
    /// Consumes hearts (default 1 on defeat). Starts the 60-minute timer if transitioning from full.
    /// </summary>
    public bool ConsumeHeart(int amount = 1, DateTime? utcNowOverride = null)
    {
        if (amount <= 0)
        {
            return false;
        }

        DateTime now = utcNowOverride ?? DateTime.UtcNow;
        RefreshHearts(now);

        if (_hearts <= 0)
        {
            return false;
        }

        bool wasFull = _hearts >= MAX_HEARTS;
        _hearts = Mathf.Max(0, _hearts - amount);

        if (wasFull && _hearts < MAX_HEARTS)
        {
            _heart_regen_start_utc_ticks = now.Ticks;
        }

        User.NotifyHeartsChanged(_hearts);
        return true;
    }

    /// <summary>
    /// Adds hearts up to MAX_HEARTS (e.g. +1 from rewarded ad).
    /// Clears regeneration timer if hearts become full.
    /// </summary>
    public bool AddHearts(int amount, DateTime? utcNowOverride = null)
    {
        if (amount <= 0)
        {
            return false;
        }

        DateTime now = utcNowOverride ?? DateTime.UtcNow;
        RefreshHearts(now);

        int previousHearts = _hearts;
        _hearts = Mathf.Clamp(_hearts + amount, 0, MAX_HEARTS);

        if (_hearts >= MAX_HEARTS)
        {
            _heart_regen_start_utc_ticks = 0;
        }

        if (_hearts != previousHearts)
        {
            User.NotifyHeartsChanged(_hearts);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Refills hearts to MAX_HEARTS (e.g. purchased with coin/gold).
    /// </summary>
    public bool RefillFullHearts(DateTime? utcNowOverride = null)
    {
        DateTime now = utcNowOverride ?? DateTime.UtcNow;
        RefreshHearts(now);

        bool changed = _hearts < MAX_HEARTS || _heart_regen_start_utc_ticks != 0;
        int previousHearts = _hearts;
        _hearts = MAX_HEARTS;
        _heart_regen_start_utc_ticks = 0;

        if (_hearts != previousHearts)
        {
            User.NotifyHeartsChanged(_hearts);
        }

        return changed;
    }

    /// <summary>
    /// Seconds remaining until the next heart regenerates (0 if already full).
    /// </summary>
    public int GetSecondsUntilNextHeart(DateTime? utcNowOverride = null)
    {
        DateTime now = utcNowOverride ?? DateTime.UtcNow;
        RefreshHearts(now);

        if (_hearts >= MAX_HEARTS || _heart_regen_start_utc_ticks <= 0)
        {
            return 0;
        }

        DateTime startUtc = new DateTime(_heart_regen_start_utc_ticks, DateTimeKind.Utc);
        double elapsedSeconds = (now - startUtc).TotalSeconds;
        double remaining = Math.Max(0d, HEART_REGEN_SECONDS - elapsedSeconds);
        return Mathf.CeilToInt((float)remaining);
    }

    public static string FormatHeartTimer(int totalSeconds)
    {
        int clamped = Mathf.Max(0, totalSeconds);
        int minutes = clamped / 60;
        int seconds = clamped % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    public bool EnsureHeartDefaults()
    {
        return RefreshHearts();
    }

    /// <summary>
    /// Marks that a battle has started and reserves 1 heart penalty if the app is closed mid-battle.
    /// Does not modify <see cref="hearts"/> so HeartDisplayView and in-session retry checks stay unaffected.
    /// </summary>
    public bool BeginBattleHeartReservation(DateTime? utcNowOverride = null)
    {
        DateTime now = utcNowOverride ?? DateTime.UtcNow;
        RefreshHearts(now);

        if (_hearts <= 0)
        {
            return false;
        }

        _pending_battle_heart = true;
        _pending_battle_utc_ticks = now.Ticks;
        return true;
    }

    /// <summary>
    /// Clears the pending mid-battle heart reservation (e.g. when the player wins the battle).
    /// </summary>
    public bool ClearPendingBattleHeart()
    {
        if (!_pending_battle_heart && _pending_battle_utc_ticks == 0)
        {
            return false;
        }

        _pending_battle_heart = false;
        _pending_battle_utc_ticks = 0;
        return true;
    }

    /// <summary>
    /// Clears any pending battle reservation and immediately consumes hearts (e.g. on defeat or Settings exit).
    /// </summary>
    public bool CommitPendingBattleHeartAndConsume(int amount = 1, DateTime? utcNowOverride = null)
    {
        bool cleared = ClearPendingBattleHeart();
        bool consumed = ConsumeHeart(amount, utcNowOverride);
        return cleared || consumed;
    }

    /// <summary>
    /// Applies the 1-heart penalty for an abandoned battle from a previous session (force-close / crash).
    /// Clamps the battle-start UTC timestamp safely so the 60-minute regen timer is accurate and never rewinds.
    /// </summary>
    public bool ResolveAbandonedBattleHeart(DateTime? utcNowOverride = null)
    {
        if (!_pending_battle_heart)
        {
            if (_pending_battle_utc_ticks != 0)
            {
                _pending_battle_utc_ticks = 0;
                return true;
            }

            return false;
        }

        DateTime now = utcNowOverride ?? DateTime.UtcNow;
        long targetTicks = _pending_battle_utc_ticks > 0 ? _pending_battle_utc_ticks : now.Ticks;

        if (targetTicks > now.Ticks)
        {
            targetTicks = now.Ticks;
        }

        if (_hearts < MAX_HEARTS && _heart_regen_start_utc_ticks > 0 && targetTicks < _heart_regen_start_utc_ticks)
        {
            targetTicks = _heart_regen_start_utc_ticks;
        }

        DateTime penaltyUtc = new DateTime(targetTicks, DateTimeKind.Utc);
        _pending_battle_heart = false;
        _pending_battle_utc_ticks = 0;

        ConsumeHeart(1, penaltyUtc);
        RefreshHearts(now);
        return true;
    }
    #endregion
}

public static partial class User
{
    public static event Action<int> OnHeartsChanged;

    internal static void NotifyHeartsChanged(int hearts)
    {
        OnHeartsChanged?.Invoke(hearts);
    }

    public static bool RefreshHeartsAndSave()
    {
        EnsureLoaded();
        if (data != null && data.RefreshHearts())
        {
            Save();
            return true;
        }

        return false;
    }

    public static bool BeginBattleHeartReservationAndSave()
    {
        EnsureLoaded();
        if (data != null && data.BeginBattleHeartReservation())
        {
            Save();
            return true;
        }

        return false;
    }

    public static bool ClearPendingBattleHeartAndSave()
    {
        EnsureLoaded();
        if (data != null && data.ClearPendingBattleHeart())
        {
            Save();
            return true;
        }

        return false;
    }

    public static bool CommitPendingBattleHeartAndSave(int amount = 1)
    {
        EnsureLoaded();
        if (data != null && data.CommitPendingBattleHeartAndConsume(amount))
        {
            Save();
            return true;
        }

        return false;
    }

    public static bool ResolveAbandonedBattleHeartAndSave()
    {
        EnsureLoaded();
        if (data != null && data.ResolveAbandonedBattleHeart())
        {
            Save();
            return true;
        }

        return false;
    }
}
