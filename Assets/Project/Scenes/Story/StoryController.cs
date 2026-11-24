using System;
using System.Collections;
using System.Collections.Generic;
using NTGH.Teammates;
using NTGH.UI;
using UnityEngine;
using UnityEngine.UI;
using SS.UI;

public class StoryController : MonoBehaviour, IKeyBack
{
    readonly Queue<ConversationData> m_Lines = new Queue<ConversationData>();
    readonly List<string> m_NewlyUnlockedCardIds = new List<string>();
    Action<IReadOnlyList<string>> m_OnClosed;
    ConversationData m_CurrentLineData;
    bool m_WaitingForReward;
    bool m_Closing;
    const float ScrollDuration = 0.16f;
    const float RevealDuration = 0.12f;

    [SerializeField] ScrollRect _scrollView;
    [SerializeField] GameObject _storyPanel;
    [SerializeField] Conversation _playerConversation;
    [SerializeField] Conversation _enemyConversation;

    Conversation _currentConversation;
    ConversationPool _conversationPool;
    readonly List<Conversation> _activeConversations = new List<Conversation>();

    public static void Show(IEnumerable<ConversationData> lines, Action onClosed, bool showBattleShield = false)
    {
        Show(lines, _ => onClosed?.Invoke(), showBattleShield);
    }

    public static void Show(
        IEnumerable<ConversationData> lines,
        Action<IReadOnlyList<string>> onClosedWithNewCards,
        bool showBattleShield = false)
    {
        var hideAnimation = showBattleShield ? ScreenAnimation.FadeHide : ScreenAnimation.None;
        Core.Add<StoryController>("Story", ScreenAnimation.None, hideAnimation,
            onScreenLoad: story => story.Setup(lines, onClosedWithNewCards), hasShield: showBattleShield, shieldAlpha: 0.5f);
    }

    void Setup(IEnumerable<ConversationData> lines, Action<IReadOnlyList<string>> onClosed)
    {
        m_OnClosed = onClosed;
        m_NewlyUnlockedCardIds.Clear();
        m_CurrentLineData = null;
        m_WaitingForReward = false;
        m_Closing = false;
        _conversationPool = ConversationPool.Instance;
        if (lines != null)
        {
            foreach (var line in lines)
            {
                if (line != null) m_Lines.Enqueue(line);
            }
        }
        ShowConversation();
    }

    void ShowConversation()
    {
        while (m_Lines.Count > 0)
        {
            var conversationData = m_Lines.Dequeue();
            if (!conversationData.HasDialogueContent)
            {
                var rewardOnlyCards = CollectUnownedRewardCards(conversationData);
                if (rewardOnlyCards.Count > 0)
                {
                    _currentConversation = null;
                    m_CurrentLineData = null;
                    ShowRewardsBatched(rewardOnlyCards, 0, ShowConversation);
                    return;
                }
                continue;
            }

            m_CurrentLineData = conversationData;
            Conversation prefab = conversationData.isPlayer ? _playerConversation : _enemyConversation;
            Conversation conversation = _conversationPool.Rent(prefab, _storyPanel.transform);
            _activeConversations.Add(conversation);
            _currentConversation = null;
            conversation.Visibility.alpha = 0f;
            conversation.Setup(conversationData);
            StartCoroutine(ScrollAndReveal(conversation));
            return;
        }

        m_Closing = true;
        StartCoroutine(CloseStory());
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

    IEnumerator ScrollAndReveal(Conversation conversation)
    {
        // The hidden row participates in the normal canvas layout pass.
        yield return null;

        if (_scrollView.content.rect.height > _scrollView.viewport.rect.height)
        {
            _scrollView.StopMovement();
            float start = Mathf.Clamp01(_scrollView.verticalNormalizedPosition);
            if (start > 0f)
            {
                for (float elapsed = 0f; elapsed < ScrollDuration; elapsed += Time.unscaledDeltaTime)
                {
                    float remaining = 1f - elapsed / ScrollDuration;
                    _scrollView.verticalNormalizedPosition = start * remaining * remaining;
                    yield return null;
                }
                _scrollView.verticalNormalizedPosition = 0f;
            }
        }

        for (float elapsed = 0f; elapsed < RevealDuration; elapsed += Time.unscaledDeltaTime)
        {
            conversation.Visibility.alpha = elapsed / RevealDuration;
            yield return null;
        }
        conversation.Visibility.alpha = 1f;
        _currentConversation = conversation;
    }
    void Update()
    {
        if (m_Closing || m_WaitingForReward || _currentConversation == null) return;
        if (Input.GetMouseButtonDown(0))
        {
            Advance();
        }
        else if (m_Lines.Count == 0 && _currentConversation.IsComplete)
        {
            TryShowPendingRewardsAndContinue();
        }
    }

    public void OnKeyBack() => Advance();

    void Advance()
    {
        if (m_Closing || m_WaitingForReward || _currentConversation == null) return;
        if (!_currentConversation.IsComplete) _currentConversation.SkipTyping();
        else TryShowPendingRewardsAndContinue();
    }

    void TryShowPendingRewardsAndContinue()
    {
        var line = m_CurrentLineData;
        m_CurrentLineData = null;

        var unownedCards = CollectUnownedRewardCards(line);
        if (unownedCards.Count > 0)
        {
            ShowRewardsBatched(unownedCards, 0, ShowConversation);
            return;
        }

        ShowConversation();
    }

    List<TeammateCardSO> CollectUnownedRewardCards(ConversationData line)
    {
        var result = new List<TeammateCardSO>();
        if (line == null || !line.HasRewardCards) return result;

        User.EnsureLoaded();
        var seenIds = new HashSet<string>();

        for (int i = 0; i < line.rewardTeammateCards.Count; i++)
        {
            var card = line.rewardTeammateCards[i];
            if (card == null || string.IsNullOrEmpty(card.Id)) continue;
            if (!seenIds.Add(card.Id)) continue;
            if (User.data != null && User.data.IsTeammateUnlocked(card.Id)) continue;

            result.Add(card);
        }

        return result;
    }

    void ShowRewardsBatched(List<TeammateCardSO> cards, int startIndex, Action onAllDone)
    {
        if (cards == null || startIndex >= cards.Count)
        {
            m_WaitingForReward = false;
            onAllDone?.Invoke();
            return;
        }

        m_WaitingForReward = true;

        int batchCount = Mathf.Min(TeammateCardRewardController.MaxCardsPerReveal, cards.Count - startIndex);
        var batch = new List<TeammateCardSO>(batchCount);
        for (int i = 0; i < batchCount; i++)
        {
            batch.Add(cards[startIndex + i]);
        }

        // Cards are granted & saved immediately when TeammateCardRewardScreen is shown.
        UnlockRewardBatch(batch);

        var database = SOManager.instance != null ? SOManager.instance.TeammateDatabase : null;
        TeammateCardRewardController.Open(
            database,
            batch,
            onClaimed: () =>
            {
                int nextIndex = startIndex + batchCount;
                if (nextIndex < cards.Count)
                {
                    ShowRewardsBatched(cards, nextIndex, onAllDone);
                }
                else
                {
                    m_WaitingForReward = false;
                    onAllDone?.Invoke();
                }
            },
            callerCanvas: GetComponentInParent<Canvas>());
    }

    void UnlockRewardBatch(List<TeammateCardSO> batch)
    {
        if (batch == null || batch.Count == 0) return;

        User.EnsureLoaded();
        if (User.data == null) return;

        bool changed = false;
        for (int i = 0; i < batch.Count; i++)
        {
            var card = batch[i];
            if (card == null || string.IsNullOrEmpty(card.Id)) continue;

            if (User.data.UnlockTeammate(card.Id, markAsNew: true))
            {
                changed = true;
                if (!m_NewlyUnlockedCardIds.Contains(card.Id))
                {
                    m_NewlyUnlockedCardIds.Add(card.Id);
                }
            }
        }

        if (changed)
        {
            User.Save();
        }
    }

    IEnumerator CloseStory()
    {
        yield return new WaitForSecondsRealtime(1f);
        ReturnConversations();
        var onClosed = m_OnClosed;
        var unlockedCopy = new List<string>(m_NewlyUnlockedCardIds);
        Core.Close(this, () => onClosed?.Invoke(unlockedCopy));
    }
}
