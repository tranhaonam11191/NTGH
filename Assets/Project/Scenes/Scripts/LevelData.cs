using System;
using System.Collections.Generic;
using UnityEngine;

public enum ConversationTrigger
{
    Start, PlayerHealth, EnemyHealth, End, TimeAfterFirstClick
}

[Serializable]
public class LevelConversation
{
    public ConversationTrigger trigger;
    [Range(0f, 1f), Tooltip("Fraction of maximum HP remaining")] public float healthThreshold;
    [Min(0f), Tooltip("Seconds of battle time since the first click")] public float delaySeconds;
    public List<ConversationData> lines = new List<ConversationData>();

    public bool Matches(ConversationTrigger moment, float previous, float current)
    {
        if (trigger != moment) return false;
        if (moment == ConversationTrigger.Start || moment == ConversationTrigger.End) return true;
        if (moment == ConversationTrigger.TimeAfterFirstClick) return current >= delaySeconds;
        return current < previous && previous >= healthThreshold && current <= healthThreshold;
    }
}

[CreateAssetMenu(fileName = "Level", menuName = "NTGH/Level Data")]
public class LevelData : ScriptableObject
{
    public string displayName;
    public CreateDataCharacter defaultPlayer;
    public CreateDataCharacter enemy;
    public List<LevelConversation> conversations = new List<LevelConversation>();
    public CreateItemReward itemReward;

    public bool HasCombatants => defaultPlayer != null && defaultPlayer.Character != null &&
        defaultPlayer.Character.health > 0 && enemy != null && enemy.Character != null &&
        enemy.Character.health > 0;
}
