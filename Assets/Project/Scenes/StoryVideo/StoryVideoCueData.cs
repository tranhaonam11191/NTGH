using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StoryVideoCue
{
    public string kind;
    public string speaker;
    public string text;
    public double startSeconds;
    public double endSeconds;
}

[Serializable]
public class StoryVideoCuesData
{
    public List<double> scenes = new List<double>();
    public List<StoryVideoCue> cues = new List<StoryVideoCue>();

    public static StoryVideoCuesData Parse(TextAsset jsonAsset)
    {
        if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
        {
            return new StoryVideoCuesData();
        }

        try
        {
            var parsed = JsonUtility.FromJson<StoryVideoCuesData>(jsonAsset.text);
            if (parsed == null)
            {
                return new StoryVideoCuesData();
            }

            if (parsed.scenes == null)
            {
                parsed.scenes = new List<double>();
            }

            if (parsed.cues == null)
            {
                parsed.cues = new List<StoryVideoCue>();
            }

            return parsed;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[StoryVideoCuesData] Failed to parse cues JSON '{jsonAsset.name}': {ex.Message}");
            return new StoryVideoCuesData();
        }
    }
}
