using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArcsData", menuName = "NTGH/Arcs Data")]
public class ArcsData : ScriptableObject
{
    [Serializable]
    public class Arc
    {
        public string name;
        public List<LevelData> levels = new List<LevelData>();
    }

    [SerializeField] List<Arc> m_Arcs = new List<Arc>();

    public List<LevelData> GetOrderedLevels()
    {
        var levels = new List<LevelData>();
        foreach (var arc in m_Arcs)
        {
            if (arc == null || arc.levels == null) continue;
            foreach (var level in arc.levels)
            {
                if (level != null) levels.Add(level);
            }
        }
        return levels;
    }
}
