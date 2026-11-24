using System.Collections.Generic;

public partial class UserData
{
    #region Story Video
    public List<string> watched_story_videos = new List<string>();

    public bool IsStoryVideoWatched(string videoKey)
    {
        if (string.IsNullOrEmpty(videoKey))
        {
            return false;
        }

        EnsureStoryVideoInitialized();
        return watched_story_videos.Contains(videoKey);
    }

    public bool MarkStoryVideoWatched(string videoKey)
    {
        if (string.IsNullOrEmpty(videoKey))
        {
            return false;
        }

        EnsureStoryVideoInitialized();
        if (watched_story_videos.Contains(videoKey))
        {
            return false;
        }

        watched_story_videos.Add(videoKey);
        return true;
    }

    public void EnsureStoryVideoInitialized()
    {
        if (watched_story_videos == null)
        {
            watched_story_videos = new List<string>();
        }
    }
    #endregion
}
