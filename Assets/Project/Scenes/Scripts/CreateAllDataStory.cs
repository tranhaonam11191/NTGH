using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/AllStoryData", order = 2)]
public class CreateAllDataStory : ScriptableObject
{
    public List<CreateData> Data = new List<CreateData>();
}