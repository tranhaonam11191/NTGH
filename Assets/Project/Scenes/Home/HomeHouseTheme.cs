using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HomeHouseTheme : MonoBehaviour
{
    public enum Part { Awning, Door, Wall }
    public enum Level { Dilapidated = 1, Patched = 2, Renovated = 3 }

    [Serializable]
    private class Layer
    {
        public Image image;
        public Sprite dilapidated;
        public Sprite patched;
        public Sprite renovated;

        public void Show(Level level)
        {
            image.sprite = level switch
            {
                Level.Dilapidated => dilapidated,
                Level.Patched => patched,
                Level.Renovated => renovated,
                _ => throw new ArgumentOutOfRangeException(nameof(level))
            };
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
