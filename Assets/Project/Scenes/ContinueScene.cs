using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinueScene : MonoBehaviour
{
    public void OnClickBackToHome()
    {        
        GameManager.instance.LoadScene("Home");
    }

    public void OnClickRePlay()
    {        
        PlayerPrefs.DeleteAll();
        GameManager.instance.LoadScene("Home");
    }
}
