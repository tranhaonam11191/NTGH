using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SS.UI;
using System.Linq;
using UnityEngine.UI;

public class StoryController : MonoBehaviour
{
    StoryConversations _storyConversations;
    List<ConversationData> _conversations = new List<ConversationData>();
    int _numConversation;

    [SerializeField] ScrollRect _scrollView;
    [SerializeField] GameObject _storyPanel;
    [SerializeField] Conversation _playerConversation;
    [SerializeField] Conversation _enemyConversation;

    Conversation _currentConversation;
    ConversationPool _conversationPool;
    readonly List<Conversation> _activeConversations = new List<Conversation>();

    public void Setup(StoryConversations storyConversations)
    {
        StopAllCoroutines();
        ReturnConversations();
        isStartGoToGame = false;
        _conversationPool = ConversationPool.Instance;
        _storyConversations = storyConversations;
        _conversations = _storyConversations.conversations.ToList();
        _numConversation = 0;

        ShowConversation();
    }
    void ShowConversation()
    {
        if (_conversations.Count == 0)
        {
            return;
        }

        if (_currentConversation != null && !_currentConversation.IsComplete)
        {
            return;
        }

        ConversationData conversationData = _conversations[0];
        _conversations.RemoveAt(0);

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
    bool isStartGoToGame;
    void Update()
    {
        if (!isStartGoToGame && _conversations != null && _conversations.Count == 0 && _currentConversation != null && _currentConversation.IsComplete)
        {
            isStartGoToGame = true;
            OnStartGoToGame();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (_currentConversation != null && _currentConversation.IsComplete)
            {
                _currentConversation.SkipTyping();
                ShowConversation();
            }
        }
        void OnStartGoToGame()
        {
            StartCoroutine(GoToGame());
            IEnumerator GoToGame()
            {
                yield return new WaitForSeconds(1f);
                ReturnConversations();
                SS.UI.Core.Load<GameController>(GameController.SCENE_NAME);
            }
        }
    }
}


