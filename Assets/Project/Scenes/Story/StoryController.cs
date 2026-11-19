using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoryController : MonoBehaviour
{
    readonly Queue<ConversationData> m_Lines = new Queue<ConversationData>();
    System.Action m_OnClosed;
    bool m_Closing;

    [SerializeField] ScrollRect _scrollView;
    [SerializeField] GameObject _storyPanel;
    [SerializeField] Conversation _playerConversation;
    [SerializeField] Conversation _enemyConversation;

    Conversation _currentConversation;
    ConversationPool _conversationPool;
    readonly List<Conversation> _activeConversations = new List<Conversation>();

    public void Setup(IEnumerable<ConversationData> lines, System.Action onClosed)
    {
        m_OnClosed = onClosed;
        _conversationPool = ConversationPool.Instance;
        foreach (var line in lines)
        {
            if (line != null) m_Lines.Enqueue(line);
        }
        ShowConversation();
    }

    void ShowConversation()
    {
        if (m_Lines.Count == 0)
        {
            m_Closing = true;
            StartCoroutine(CloseStory());
            return;
        }

        var conversationData = m_Lines.Dequeue();
        Conversation prefab = conversationData.isPlayer ? _playerConversation : _enemyConversation;
        Conversation conversation = _conversationPool.Rent(prefab, _storyPanel.transform);
        _activeConversations.Add(conversation);
        conversation.Setup(conversationData);
        _currentConversation = conversation;
        StartCoroutine(ScrollToLatestConversation());
    }

    void ReturnConversations()
    {
        if (_conversationPool != null)
        {
            foreach (Conversation conversation in _activeConversations)
            {
                _conversationPool.Return(conversation);
            }
        }

        _activeConversations.Clear();
        _currentConversation = null;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        ReturnConversations();
    }

    IEnumerator ScrollToLatestConversation()
    {
        // Wait for the new conversation to participate in the UI layout.
        yield return null;

        if (_scrollView == null || _scrollView.content == null)
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollView.content);
        _scrollView.StopMovement();
        _scrollView.verticalNormalizedPosition = 0f;
    }
    void Update()
    {
        if (m_Closing || _currentConversation == null) return;
        if (Input.GetMouseButtonDown(0))
        {
            if (!_currentConversation.IsComplete) _currentConversation.SkipTyping();
            else ShowConversation();
        }
        else if (m_Lines.Count == 0 && _currentConversation.IsComplete)
        {
            ShowConversation();
        }
    }

    IEnumerator CloseStory()
    {
        yield return new WaitForSecondsRealtime(1f);
        ReturnConversations();
        var onClosed = m_OnClosed;
        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(gameObject.scene).completed += _ => onClosed?.Invoke();
    }
}
