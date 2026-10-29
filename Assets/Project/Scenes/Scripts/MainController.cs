using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SS.UI;

public class MainController : MonoBehaviour
{
    void Update()
    {
        if(Input.GetMouseButtonUp(0))
        {
            GameManager.instance.PlayAudio("Click");
            SS.UI.Core.Load<HomeController>("Home");
        }    
    }
}
