using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StoryConversation", menuName = "NTGH/Story Conversation")]
public class StoryConversations : ScriptableObject
{
    public ConversationData[] conversations;
}
[Serializable]
public class ConversationData
{
    public string content;

    public Sprite imgCharacter;
    public bool isPlayer;    
}
