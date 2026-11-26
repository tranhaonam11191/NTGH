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

    public IReadOnlyList<Arc> Arcs => m_Arcs;

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

    public int GetTotalLevelCount()
    {
        int count = 0;
        if (m_Arcs == null) return 0;

        for (int i = 0; i < m_Arcs.Count; i++)
        {
            var arc = m_Arcs[i];
            if (arc == null || arc.levels == null) continue;

            for (int j = 0; j < arc.levels.Count; j++)
            {
                if (arc.levels[j] != null) count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Computes the flat 0-based level index from a 0-based arcIndex and 0-based levelIndexInArc.
    /// </summary>
    public bool TryGetLevelByArcAndIndex(int arcIndex, int levelIndexInArc, out int flatIndex, out LevelData level)
    {
        flatIndex = -1;
        level = null;

        if (m_Arcs == null || arcIndex < 0 || arcIndex >= m_Arcs.Count || levelIndexInArc < 0)
            return false;

        int runningIndex = 0;
        for (int a = 0; a < m_Arcs.Count; a++)
        {
            var arc = m_Arcs[a];
            if (arc == null || arc.levels == null)
            {
                if (a == arcIndex) return false;
                continue;
            }

            int validInArc = 0;
            for (int l = 0; l < arc.levels.Count; l++)
            {
                var candidate = arc.levels[l];
                if (candidate == null) continue;

                if (a == arcIndex && validInArc == levelIndexInArc)
                {
                    flatIndex = runningIndex;
                    level = candidate;
                    return true;
                }

                validInArc++;
                runningIndex++;
            }

            if (a == arcIndex)
                return false;
        }

        return false;
    }

    /// <summary>
    /// Resolves a flat 0-based level index into its 0-based arcIndex, 0-based levelIndexInArc, and LevelData.
    /// </summary>
    public bool TryGetArcAndLevelIndex(int flatIndex, out int arcIndex, out int levelIndexInArc, out LevelData level)
    {
        arcIndex = -1;
        levelIndexInArc = -1;
        level = null;

        if (m_Arcs == null || flatIndex < 0)
            return false;

        int runningIndex = 0;
        for (int a = 0; a < m_Arcs.Count; a++)
        {
            var arc = m_Arcs[a];
            if (arc == null || arc.levels == null) continue;

            int validInArc = 0;
            for (int l = 0; l < arc.levels.Count; l++)
            {
                var candidate = arc.levels[l];
                if (candidate == null) continue;

                if (runningIndex == flatIndex)
                {
                    arcIndex = a;
                    levelIndexInArc = validInArc;
                    level = candidate;
                    return true;
                }

                validInArc++;
                runningIndex++;
            }
        }

        return false;
    }

    /// <summary>
    /// Parses either a 1-based global level number (e.g. "5") or an "arc-level" pair (e.g. "1-3" or "2.1"),
    /// computing the exact flat index, arc index, and level index within that arc from ArcsData.
    /// </summary>
    public bool TryParseLevelInput(string input, out int flatIndex, out int arcIndex, out int levelIndexInArc, out LevelData level)
    {
        flatIndex = -1;
        arcIndex = -1;
        levelIndexInArc = -1;
        level = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string trimmed = input.Trim();
        char[] separators = { '-', '.', ':', '/', ' ' };
        string[] parts = trimmed.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0], out int arcNumber) && int.TryParse(parts[1], out int levelInArcNumber))
            {
                if (arcNumber > 0 && levelInArcNumber > 0 &&
                    TryGetLevelByArcAndIndex(arcNumber - 1, levelInArcNumber - 1, out flatIndex, out level))
                {
                    arcIndex = arcNumber - 1;
                    levelIndexInArc = levelInArcNumber - 1;
                    return true;
                }
            }
            return false;
        }

        if (parts.Length == 1 && int.TryParse(parts[0], out int globalLevelNumber))
        {
            if (globalLevelNumber > 0 &&
                TryGetArcAndLevelIndex(globalLevelNumber - 1, out arcIndex, out levelIndexInArc, out level))
            {
                flatIndex = globalLevelNumber - 1;
                return true;
            }
        }

        return false;
    }
}
