using UnityEditor;
using UnityEngine;

public class PlayerUtils
{
    [MenuItem("SS/Clear PlayerPrefs")]
    public static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        User.data = null;
        Debug.Log("Cleared PlayerPrefs");
    }

    [MenuItem("SS/Show User data")]
    public static void ShowUserData()
    {
        Debug.Log(PlayerPrefs.GetString(User.USER_DATA_KEY, string.Empty));
    }
}
