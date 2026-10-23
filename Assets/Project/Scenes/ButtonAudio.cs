using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonAudio : MonoBehaviour
{
    Button m_btn;
    void Awake()
    {
        m_btn = GetComponent<Button>();
        m_btn.onClick.AddListener(Play);
    }


    void Play()
    {
        Debug.Log("Play");
        if (GameManager.instance != null)
        {
            Debug.Log("GameManager.instance != null");
            GameManager.instance.PlayAudio("Click");            
        }
    }
}
