using System;
using System.Collections.Generic;
using NTGH.Teammates;
using UnityEngine;

[CreateAssetMenu(fileName = "StoryConversation", menuName = "NTGH/Story Conversation")]
public class StoryConversations : ScriptableObject
{
    public ConversationData[] conversations;
}
[Serializable]
public class ConversationData
{
    public const int MaxRewardCardsPerLine = 3;

    public string content;

    public Sprite imgCharacter;
    public bool isPlayer;

    [Header("Free Card Reward (Optional)")]
    [Tooltip("Kéo 1-3 thẻ đồng đội tặng miễn phí khi thoại chạy đến dòng này.")]
    public List<TeammateCardSO> rewardTeammateCards = new List<TeammateCardSO>();

    public bool HasDialogueContent => !string.IsNullOrWhiteSpace(content) || imgCharacter != null;
    public bool HasRewardCards => rewardTeammateCards != null && rewardTeammateCards.Count > 0;
}
