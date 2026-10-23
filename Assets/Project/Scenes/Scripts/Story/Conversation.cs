using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ContentStory
{
    public string content;

    public Sprite imgCharacter;
}

[Serializable]
public class Character
{
    public string Name;

    public Sprite avatar;

    public int attack;

    public int health;

    public int defence;

    public bool isPlayer;
}

public class Conversation : MonoBehaviour
{
    [SerializeField] Image m_imgCharacter;

    [SerializeField] TextMeshProUGUI m_textContent;

    ContentStory m_data;
    public void Setup(ContentStory data)
    {
        m_data = data;

        if(m_data.imgCharacter != null)
        {
            m_imgCharacter.color = Color.white;
            m_imgCharacter.sprite = m_data.imgCharacter;
        }
        else
        {
            m_imgCharacter.color = new Color(1, 1, 1, 0);
            m_imgCharacter.sprite = null;
        }


        m_textContent.text = m_data.content;
    }

    

    public void OnClickContent()
    {
        GameManager.instance.PlayAudio("Click2");
        StoryController.instance.SetConversation();
    }    
}
