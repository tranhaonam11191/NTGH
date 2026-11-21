using Newtonsoft.Json;
using UnityEngine;

public partial class UserData
{
    public bool Init()
    {
        bool changed = false;

        return changed;
    }
}

public static partial class User
{
    public const string USER_DATA_KEY = "USER_DATA";

    public static UserData data;

    public static void Load()
    {
        var textData = PlayerPrefs.GetString(USER_DATA_KEY, string.Empty);
        var changed = false;

        if (!string.IsNullOrEmpty(textData))
        {
            data = JsonConvert.DeserializeObject<UserData>(textData);
        }

        if (data == null)
        {
            data = new UserData();
            changed = true;
        }

        if (data.Init())
        {
            changed = true;
        }

        if (changed)
        {
            Save();
        }
    }

    public static void Save()
    {
        if (data == null)
        {
            data = new UserData();
        }

        var textData = JsonConvert.SerializeObject(data);
        PlayerPrefs.SetString(USER_DATA_KEY, textData);
        PlayerPrefs.Save();
    }
}
