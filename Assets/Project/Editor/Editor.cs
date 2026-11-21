using UnityEngine;
using UnityEditor;

public class EditorUNITY : MonoBehaviour
{
    [MenuItem("AllTool/ClearPlayerPrefs")]
    static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        User.data = null;
        Debug.Log("ClearAll");
    }

}
