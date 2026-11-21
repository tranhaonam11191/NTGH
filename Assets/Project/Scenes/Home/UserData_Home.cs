using System.Collections.Generic;
using UnityEngine;

public partial class UserData
{
    #region Home
    public Dictionary<string, int> house_parts = new Dictionary<string, int>();
    public int outfit_rank = 1;
    public string current_outfit_id = string.Empty;

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
        house_parts[part.ToString()] = Mathf.Max(1, partId);
    }

    public bool EnsureHomeDefaults()
    {
        bool changed = false;

        if (house_parts == null)
        {
            house_parts = new Dictionary<string, int>();
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
