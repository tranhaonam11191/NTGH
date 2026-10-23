using UnityEngine;
using UnityEditor;

public class EditorUNITY : MonoBehaviour
{
    [MenuItem("AllTool/ClearPlayerPrefs")]
    static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("ClearAll");
    }

}
