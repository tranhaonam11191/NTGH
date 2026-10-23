using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/StoryData", order = 1)]
public class CreateData : ScriptableObject
{
    public List<CreateDataCharacter> Player = new List<CreateDataCharacter>();

    public List<CreateDataCharacter> Enemy = new List<CreateDataCharacter>();

    public List<ContentStory> Data = new List<ContentStory>();

    public CreateItemReward itemReward;
}