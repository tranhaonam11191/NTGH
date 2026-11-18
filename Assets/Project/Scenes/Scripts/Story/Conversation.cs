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

    [SerializeField, Min(1f)] float m_charactersPerSecond = 30f;

    float m_visibleCharacterCount;

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


        StartTyping(m_data.content);
    }
    ConversationData _conversationData;
    public bool IsComplete { get; private set; } = true;
    public void Setup(ConversationData data)
    {
        _conversationData = data;
    
        if(_conversationData.imgCharacter != null)
        {
            m_imgCharacter.color = Color.white;
            m_imgCharacter.sprite = _conversationData.imgCharacter;
        }
        else
        {
            m_imgCharacter.color = new Color(1, 1, 1, 0);
            m_imgCharacter.sprite = null;
        }

        StartTyping(_conversationData.content);
    }

    void StartTyping(string content)
    {
        if (m_textContent == null)
        {
            IsComplete = true;
            Debug.LogError($"Conversation '{name}' is missing m_textContent.", this);
            return;
        }

        m_textContent.text = content ?? string.Empty;
        m_textContent.maxVisibleCharacters = 0;
        m_textContent.ForceMeshUpdate(true);
        m_visibleCharacterCount = 0f;
        IsComplete = m_textContent.textInfo.characterCount == 0;
    }

    void Update()
    {
        if (IsComplete)
        {
            return;
        }

        m_visibleCharacterCount += Mathf.Max(1f, m_charactersPerSecond) * Time.unscaledDeltaTime;
        int characterCount = m_textContent.textInfo.characterCount;
        m_textContent.maxVisibleCharacters = Mathf.Min(Mathf.FloorToInt(m_visibleCharacterCount), characterCount);
        IsComplete = m_textContent.maxVisibleCharacters >= characterCount;
    }

    void CompleteTyping()
    {
        IsComplete = true;
        if (m_textContent != null)
        {
            m_textContent.maxVisibleCharacters = int.MaxValue;
        }
    }

    public void ResetForPool()
    {
        StopAllCoroutines();
        IsComplete = true;
        m_visibleCharacterCount = 0f;
        m_data = null;
        _conversationData = null;

        if (m_textContent != null)
        {
            m_textContent.text = string.Empty;
            m_textContent.maxVisibleCharacters = 0;
        }

        if (m_imgCharacter != null)
        {
            m_imgCharacter.sprite = null;
            m_imgCharacter.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    void OnDisable()
    {
        CompleteTyping();
    }

    public void SkipTyping ()
    {
        SOManager.instance.PlayAudio("Click2");
        CompleteTyping();
    }    
}
