using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS.UI;

public class ContinueScene : MonoBehaviour
{
    public void OnClickBackToHome()
    {        
        SOManager.LoadHome();
    }

    public void OnClickRePlay()
    {
        User.ResetData();
        SOManager.LoadHome();
    }
}
