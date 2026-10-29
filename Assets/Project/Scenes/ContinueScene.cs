using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS.UI;

public class ContinueScene : MonoBehaviour
{
    public void OnClickBackToHome()
    {        
        SS.UI.Core.Load<HomeController>("Home");
    }

    public void OnClickRePlay()
    {        
        PlayerPrefs.DeleteAll();
        SS.UI.Core.Load<HomeController>("Home");
    }
}
