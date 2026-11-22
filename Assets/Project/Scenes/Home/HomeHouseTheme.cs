using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HomeHouseTheme : MonoBehaviour
{
    public enum Part { Awning, Door, Wall }
    public enum Level { Dilapidated = 1, Patched = 2, Renovated = 3 }

    public readonly struct HouseItemSpec
    {
        public readonly Part Part;
        public readonly Level Level;
        public readonly string DisplayName;
        public readonly string TierLabel;
        public readonly CustomizeUnlockMethod UnlockMethod;
        public readonly int PriceGold;
        public readonly int AdsRequired;

        public HouseItemSpec(
            Part part,
            Level level,
            string displayName,
            string tierLabel,
            CustomizeUnlockMethod unlockMethod,
            int priceGold,
            int adsRequired)
        {
            Part = part;
            Level = level;
            DisplayName = displayName;
            TierLabel = tierLabel;
            UnlockMethod = unlockMethod;
            PriceGold = priceGold;
            AdsRequired = Mathf.Max(1, adsRequired);
        }
    }

    public event Action<Part, Level> PartChanged;

    [Serializable]
    private class Layer
    {
        public Image image;
        public Sprite dilapidated;
        public Sprite patched;
        public Sprite renovated;

        public Sprite GetSprite(Level level) => level switch
        {
            Level.Dilapidated => dilapidated,
            Level.Patched => patched,
            Level.Renovated => renovated,
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };

        public void Show(Level level)
        {
            image.sprite = GetSprite(level);
        }
    }

    [SerializeField] private Layer m_awning;
    [SerializeField] private Layer m_door;
    [SerializeField] private Layer m_wall;

    private void Awake()
    {
        User.EnsureLoaded();
        Restore(Part.Awning);
        Restore(Part.Door);
        Restore(Part.Wall);
    }

    public static string GetPartDisplayName(Part part) => part switch
    {
        Part.Awning => "Mái Hiên",
        Part.Door => "Cửa Cuốn",
        Part.Wall => "Tường Nhà",
        _ => part.ToString()
    };

    public static HouseItemSpec GetItemSpec(Part part, Level level)
    {
        return (part, level) switch
        {
            (Part.Awning, Level.Dilapidated) => new HouseItemSpec(part, level, "Mái Bạt Rách", "Cấp 1 • Cũ Nát", CustomizeUnlockMethod.Free, 0, 1),
            (Part.Awning, Level.Patched) => new HouseItemSpec(part, level, "Mái Hiên Chắp Vá", "Cấp 2 • Nâng Cấp", CustomizeUnlockMethod.Gold, 300, 1),
            (Part.Awning, Level.Renovated) => new HouseItemSpec(part, level, "Mái Sọc Xanh Trắng", "Cấp 3 • Khang Trang", CustomizeUnlockMethod.WatchAd, 800, 1),

            (Part.Door, Level.Dilapidated) => new HouseItemSpec(part, level, "Cửa Sắt Rỉ Sét", "Cấp 1 • Cũ Nát", CustomizeUnlockMethod.Free, 0, 1),
            (Part.Door, Level.Patched) => new HouseItemSpec(part, level, "Cửa Sắt Sơn Lại", "Cấp 2 • Nâng Cấp", CustomizeUnlockMethod.Gold, 450, 1),
            (Part.Door, Level.Renovated) => new HouseItemSpec(part, level, "Cửa Cuốn Xanh Mới", "Cấp 3 • Khang Trang", CustomizeUnlockMethod.WatchAd, 1200, 2),

            (Part.Wall, Level.Dilapidated) => new HouseItemSpec(part, level, "Tường Loang Lổ", "Cấp 1 • Cũ Nát", CustomizeUnlockMethod.Free, 0, 1),
            (Part.Wall, Level.Patched) => new HouseItemSpec(part, level, "Tường Trát Xi Măng", "Cấp 2 • Nâng Cấp", CustomizeUnlockMethod.Gold, 600, 1),
            (Part.Wall, Level.Renovated) => new HouseItemSpec(part, level, "Tường Gạch Men Mới", "Cấp 3 • Khang Trang", CustomizeUnlockMethod.WatchAd, 1600, 2),
            _ => new HouseItemSpec(part, level, $"{part} {(int)level}", $"Cấp {(int)level}", CustomizeUnlockMethod.Free, 0, 1)
        };
    }

    public Sprite GetPartSprite(Part part, Level level) => GetLayer(part).GetSprite(level);

    public bool IsPartLevelUnlocked(Part part, Level level)
    {
        if (GetItemSpec(part, level).UnlockMethod == CustomizeUnlockMethod.Free)
            return true;

        User.EnsureLoaded();
        return User.data.IsHouseItemUnlocked(part, level);
    }

    public int GetAdProgress(Part part, Level level)
    {
        User.EnsureLoaded();
        return User.data.GetCustomizeAdProgress(GetAdProgressKey(part, level));
    }

    public bool TryUnlockWithGold(Part part, Level level, bool autoEquip = true)
    {
        if (IsPartLevelUnlocked(part, level))
            return false;

        HouseItemSpec spec = GetItemSpec(part, level);
        User.EnsureLoaded();
        if (!User.data.TrySpendGold(spec.PriceGold))
            return false;

        User.data.UnlockHouseItem(part, level);
        if (autoEquip)
        {
            SetPartLevel(part, level);
        }
        else
        {
            User.Save();
        }

        return true;
    }

    public bool TryRecordAdWatch(Part part, Level level, out bool unlockedNow, bool autoEquip = true)
    {
        unlockedNow = false;
        if (IsPartLevelUnlocked(part, level))
            return false;

        HouseItemSpec spec = GetItemSpec(part, level);
        User.EnsureLoaded();
        int watched = User.data.IncrementCustomizeAdProgress(GetAdProgressKey(part, level));
        if (watched >= spec.AdsRequired)
        {
            unlockedNow = true;
            User.data.UnlockHouseItem(part, level);
            if (autoEquip)
            {
                SetPartLevel(part, level);
            }
            else
            {
                User.Save();
            }
        }
        else
        {
            User.Save();
        }

        return true;
    }

    private static string GetAdProgressKey(Part part, Level level) => $"house_{part}_{(int)level}";

    public Level GetPartLevel(Part part)
    {
        User.EnsureLoaded();
        return (Level)Mathf.Clamp(User.data.GetHousePartId(part, (int)Level.Dilapidated), (int)Level.Dilapidated, (int)Level.Renovated);
    }

    public void SetTheme(Level level)
    {
        SetPartLevel(Part.Awning, level);
        SetPartLevel(Part.Door, level);
        SetPartLevel(Part.Wall, level);
    }

    public void SetPartLevel(Part part, Level level)
    {
        User.EnsureLoaded();
        GetLayer(part).Show(level);
        User.data.SetHousePartId(part, (int)level);
        User.Save();
        PartChanged?.Invoke(part, level);
    }

    // Visibility is independent of the owned level (e.g. for a preview).
    public void SetPartVisible(Part part, bool visible)
    {
        GetLayer(part).image.enabled = visible;
    }

    // Complete each tier in awning, door, wall order. False at the final tier.
    public bool UpgradeNext()
    {
        for (int next = (int)Level.Patched; next <= (int)Level.Renovated; next++)
        {
            for (Part part = Part.Awning; part <= Part.Wall; part++)
            {
                if ((int)GetPartLevel(part) >= next) continue;
                SetPartLevel(part, (Level)next);
                return true;
            }
        }
        return false;
    }

    private void Restore(Part part) => GetLayer(part).Show(GetPartLevel(part));

    private Layer GetLayer(Part part) => part switch
    {
        Part.Awning => m_awning,
        Part.Door => m_door,
        Part.Wall => m_wall,
        _ => throw new ArgumentOutOfRangeException(nameof(part))
    };
}
