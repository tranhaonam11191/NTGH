using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class MainController : MonoBehaviour
{
    [SerializeField] VideoPlayer m_videoPlayer;

    void Awake()
    {
        SS.UI.Core.Init();
    }

    IEnumerator Start()
    {
        if (m_videoPlayer == null)
        {
            yield break;
        }

        if (!m_videoPlayer.isPrepared)
        {
            m_videoPlayer.Prepare();
            yield return new WaitUntil(() => m_videoPlayer.isPrepared);
        }

        double remainingTime = m_videoPlayer.length - m_videoPlayer.time;
        if (remainingTime <= 0 || double.IsNaN(remainingTime) || double.IsInfinity(remainingTime))
        {
            Debug.LogWarning("Cannot determine the remaining video duration.", this);
            yield break;
        }

        m_videoPlayer.isLooping = false;
        m_videoPlayer.Play();

        while (remainingTime > 0)
        {
            yield return null;
            // Unscaled time keeps the intro running when timeScale is zero.
            // Cap long frames so a debugger pause does not consume the countdown.
            float elapsedTime = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            remainingTime = System.Math.Max(0d, remainingTime - elapsedTime * m_videoPlayer.playbackSpeed);
        }

        OnVideoCompleted(m_videoPlayer);
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void OnVideoCompleted(VideoPlayer videoPlayer)
    {
        SS.UI.Core.Load<HomeController>("Home");
    }
}
