using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainController : MonoBehaviour
{
    void Update()
    {
        if(Input.GetMouseButtonUp(0))
        {
            GameManager.instance.PlayAudio("Click");
            GameManager.instance.LoadScene("Home");
        }    
    }
}
