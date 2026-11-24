using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class StoryVideoController : MonoBehaviour
{
    public const string SCENE_NAME = "StoryVideo";

    [Header("Video Components")]
    [SerializeField] private VideoPlayer m_videoPlayer;
    [SerializeField] private RawImage m_rawImage;
    [SerializeField] private Image m_background;

    [Header("Subtitle / Text Slots (Top, Center, Bottom)")]
    [SerializeField] private TextMeshProUGUI m_textTop;
    [SerializeField] private TextMeshProUGUI m_textCenter;
    [SerializeField] private TextMeshProUGUI m_textBottom;

    [Header("Continue & Skip Controls")]
    [SerializeField] private TextMeshProUGUI m_textTapToContinue;
    [SerializeField] private Button m_tapButton;
    [SerializeField] private Button m_skipButton;
    [SerializeField] private float m_nearEndSeconds = 2f;
    [SerializeField] private float m_videoFadeInDuration = 0.5f;
    [SerializeField] private double m_skipSeekEpsilon = 0.25d;

    [Header("Optional Standalone Fallback (Editor Test)")]
    [SerializeField] private TextAsset m_defaultTextJson;

    private readonly List<StoryVideoCue> m_cues = new List<StoryVideoCue>();
    private readonly List<double> m_sceneStarts = new List<double>();
    private string m_videoKey;
    private bool m_hasSetup;
    private bool m_isPlaying;
    private bool m_canContinue;
    private bool m_isLeaving;
    private bool m_markedWatched;

    private RenderTexture m_runtimeRenderTexture;
    private Tween m_videoFadeTween;
    private Tween m_continuePulseTween;
    private Coroutine m_playRoutine;

    private void Awake()
    {
        User.EnsureLoaded();
        SS.UI.Core.Init();

        SetSlotText(m_textTop, null);
        SetSlotText(m_textCenter, null);
        SetSlotText(m_textBottom, null);

        if (m_textTapToContinue != null)
        {
            m_textTapToContinue.gameObject.SetActive(false);
        }

        if (m_rawImage != null)
        {
            Color c = m_rawImage.color;
            c.a = 0f;
            m_rawImage.color = c;
        }

        if (m_tapButton != null)
        {
            m_tapButton.onClick.AddListener(OnTapContinue);
        }

        if (m_skipButton != null)
        {
            m_skipButton.onClick.AddListener(OnClickSkip);
            m_skipButton.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (!m_hasSetup && m_videoPlayer != null && m_videoPlayer.clip != null)
        {
            Setup(m_videoPlayer.clip, m_defaultTextJson, m_videoPlayer.clip.name);
        }
    }

    public void Setup(LevelData level)
    {
        if (level == null || level.videoClip == null)
        {
            GoToHome();
            return;
        }

        Setup(level.videoClip, level.textJson, level.GetStoryVideoKey());
    }

    public void Setup(VideoClip clip, TextAsset textJson, string videoKey)
    {
        m_hasSetup = true;
        m_videoKey = videoKey ?? string.Empty;
        m_canContinue = false;
        m_isLeaving = false;
        m_markedWatched = false;
        m_isPlaying = false;

        m_cues.Clear();
        m_sceneStarts.Clear();
        var parsed = StoryVideoCuesData.Parse(textJson);
        if (parsed != null && parsed.cues != null)
        {
            m_cues.AddRange(parsed.cues);
        }

        BuildSceneStarts(parsed);

        SetSlotText(m_textTop, null);
        SetSlotText(m_textCenter, null);
        SetSlotText(m_textBottom, null);

        if (m_textTapToContinue != null)
        {
            m_textTapToContinue.gameObject.SetActive(false);
        }

        if (clip == null || m_videoPlayer == null)
        {
            MarkWatched();
            GoToHome();
            return;
        }

        if (m_playRoutine != null)
        {
            StopCoroutine(m_playRoutine);
        }

        m_playRoutine = StartCoroutine(PlayVideoRoutine(clip));
    }

    private IEnumerator PlayVideoRoutine(VideoClip clip)
    {
        m_videoPlayer.loopPointReached -= OnVideoLoopPointReached;
        m_videoPlayer.loopPointReached += OnVideoLoopPointReached;

        m_videoPlayer.Stop();
        m_videoPlayer.source = VideoSource.VideoClip;
        m_videoPlayer.clip = clip;
        m_videoPlayer.isLooping = false;
        m_videoPlayer.playOnAwake = false;
        m_videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        EnsureRenderTexture((int)clip.width, (int)clip.height);

        m_videoPlayer.Prepare();
        yield return new WaitUntil(() => m_videoPlayer.isPrepared);

        if (m_isLeaving)
        {
            yield break;
        }

        m_videoPlayer.Play();
        m_isPlaying = true;

        if (m_skipButton != null)
        {
            m_skipButton.gameObject.SetActive(true);
        }

        if (m_rawImage != null)
        {
            m_videoFadeTween?.Kill();
            m_videoFadeTween = m_rawImage.DOFade(1f, m_videoFadeInDuration).SetUpdate(true);
        }
    }

    private void EnsureRenderTexture(int width, int height)
    {
        int rtWidth = width > 0 ? width : 720;
        int rtHeight = height > 0 ? height : 1600;

        if (m_runtimeRenderTexture != null &&
            (m_runtimeRenderTexture.width != rtWidth || m_runtimeRenderTexture.height != rtHeight))
        {
            ReleaseRenderTexture();
        }

        if (m_runtimeRenderTexture == null)
        {
            m_runtimeRenderTexture = new RenderTexture(rtWidth, rtHeight, 0, RenderTextureFormat.ARGB32)
            {
                name = "StoryVideo_RuntimeRT"
            };
            m_runtimeRenderTexture.Create();
        }

        m_videoPlayer.targetTexture = m_runtimeRenderTexture;
        if (m_rawImage != null)
        {
            m_rawImage.texture = m_runtimeRenderTexture;
        }
    }

    private void Update()
    {
        if (!m_isPlaying || m_videoPlayer == null)
        {
            return;
        }

        double currentTime = m_videoPlayer.time;
        UpdateSubtitleSlots(currentTime);

        if (!m_canContinue && m_videoPlayer.length > 0d)
        {
            double remaining = m_videoPlayer.length - currentTime;
            if (remaining <= m_nearEndSeconds)
            {
                ShowTapToContinue();
            }
        }
    }

    private void UpdateSubtitleSlots(double currentTime)
    {
        StoryVideoCue topCue = null;
        StoryVideoCue centerCue = null;
        StoryVideoCue bottomCue = null;

        for (int i = 0; i < m_cues.Count; i++)
        {
            var cue = m_cues[i];
            if (cue == null) continue;
            if (currentTime < cue.startSeconds || currentTime >= cue.endSeconds) continue;

            string kind = string.IsNullOrEmpty(cue.kind) ? "bottom" : cue.kind.Trim().ToLowerInvariant();
            switch (kind)
            {
                case "top":
                    topCue = cue;
                    break;
                case "center":
                    centerCue = cue;
                    break;
                default:
                    bottomCue = cue;
                    break;
            }
        }

        SetSlotText(m_textTop, topCue);
        SetSlotText(m_textCenter, centerCue);
        SetSlotText(m_textBottom, bottomCue);
    }

    private static void SetSlotText(TextMeshProUGUI target, StoryVideoCue cue)
    {
        if (target == null) return;

        if (cue == null || string.IsNullOrEmpty(cue.text))
        {
            if (target.gameObject.activeSelf)
            {
                target.gameObject.SetActive(false);
            }
            return;
        }

        string formatted = string.IsNullOrWhiteSpace(cue.speaker)
            ? cue.text
            : $"<color=#F0C97A><b>{cue.speaker}</b></color>\n{cue.text}";

        if (target.text != formatted)
        {
            target.text = formatted;
        }

        if (!target.gameObject.activeSelf)
        {
            target.gameObject.SetActive(true);
        }
    }

    private void OnVideoLoopPointReached(VideoPlayer source)
    {
        ShowTapToContinue();
    }

    private void ShowTapToContinue()
    {
        MarkWatched();

        if (m_canContinue)
        {
            return;
        }

        m_canContinue = true;

        if (m_textTapToContinue != null)
        {
            m_textTapToContinue.text = "Tap To Continue";
            m_textTapToContinue.gameObject.SetActive(true);

            Color c = m_textTapToContinue.color;
            c.a = 0f;
            m_textTapToContinue.color = c;

            m_continuePulseTween?.Kill();
            m_continuePulseTween = DOTween.Sequence()
                .Append(m_textTapToContinue.DOFade(1f, 0.45f))
                .Append(m_textTapToContinue.DOFade(0.35f, 0.65f).SetLoops(-1, LoopType.Yoyo))
                .SetUpdate(true);
        }
    }

    private void MarkWatched()
    {
        if (m_markedWatched || string.IsNullOrEmpty(m_videoKey))
        {
            return;
        }

        m_markedWatched = true;
        User.EnsureLoaded();
        if (User.data != null && User.data.MarkStoryVideoWatched(m_videoKey))
        {
            User.Save();
        }
    }

    public void OnTapContinue()
    {
        if (!m_canContinue || m_isLeaving)
        {
            return;
        }

        GoToHome();
    }

    public void OnClickSkip()
    {
        if (!m_isPlaying || m_isLeaving || m_videoPlayer == null || !m_videoPlayer.isPrepared)
        {
            return;
        }

        double currentTime = m_videoPlayer.time;
        double duration = m_videoPlayer.length;

        if (TryGetNextSceneTime(currentTime, duration, out double nextSceneTime))
        {
            m_videoPlayer.time = nextSceneTime;
            if (!m_videoPlayer.isPlaying)
            {
                m_videoPlayer.Play();
            }

            UpdateSubtitleSlots(nextSceneTime);

            if (!m_canContinue && duration > 0d && (duration - nextSceneTime) <= m_nearEndSeconds)
            {
                ShowTapToContinue();
            }
            return;
        }

        // Already on the final scene: mark watched and return straight to Home.
        MarkWatched();
        GoToHome();
    }

    private void BuildSceneStarts(StoryVideoCuesData parsed)
    {
        if (parsed != null && parsed.scenes != null && parsed.scenes.Count > 0)
        {
            for (int i = 0; i < parsed.scenes.Count; i++)
            {
                double t = parsed.scenes[i];
                if (t >= 0d)
                {
                    m_sceneStarts.Add(t);
                }
            }
        }
        else if (m_cues.Count > 0)
        {
            for (int i = 0; i < m_cues.Count; i++)
            {
                if (m_cues[i] != null && m_cues[i].startSeconds >= 0d)
                {
                    m_sceneStarts.Add(m_cues[i].startSeconds);
                }
            }
        }

        if (m_sceneStarts.Count == 0)
        {
            m_sceneStarts.Add(0d);
            return;
        }

        m_sceneStarts.Sort();
    }

    private bool TryGetNextSceneTime(double currentTime, double duration, out double nextSceneTime)
    {
        double threshold = currentTime + m_skipSeekEpsilon;
        for (int i = 0; i < m_sceneStarts.Count; i++)
        {
            double candidate = m_sceneStarts[i];
            if (candidate <= threshold)
            {
                continue;
            }

            if (duration > 0d && candidate >= duration - 0.1d)
            {
                break;
            }

            nextSceneTime = candidate;
            return true;
        }

        nextSceneTime = 0d;
        return false;
    }

    private void GoToHome()
    {
        if (m_isLeaving) return;
        m_isLeaving = true;
        m_isPlaying = false;

        if (m_skipButton != null)
        {
            m_skipButton.gameObject.SetActive(false);
        }

        m_continuePulseTween?.Kill();
        m_videoFadeTween?.Kill();

        if (m_videoPlayer != null && m_videoPlayer.isPlaying)
        {
            m_videoPlayer.Stop();
        }

        if (m_rawImage != null)
        {
            m_videoFadeTween = m_rawImage.DOFade(0f, 0.15f).SetUpdate(true).OnComplete(() =>
            {
                SS.UI.Core.Load<HomeController>("Home");
            });
        }
        else
        {
            SS.UI.Core.Load<HomeController>("Home");
        }
    }

    private void ReleaseRenderTexture()
    {
        if (m_videoPlayer != null && m_videoPlayer.targetTexture == m_runtimeRenderTexture)
        {
            m_videoPlayer.targetTexture = null;
        }

        if (m_rawImage != null && m_rawImage.texture == m_runtimeRenderTexture)
        {
            m_rawImage.texture = null;
        }

        if (m_runtimeRenderTexture != null)
        {
            m_runtimeRenderTexture.Release();
            Destroy(m_runtimeRenderTexture);
            m_runtimeRenderTexture = null;
        }
    }

    private void OnDestroy()
    {
        m_videoFadeTween?.Kill();
        m_continuePulseTween?.Kill();

        if (m_videoPlayer != null)
        {
            m_videoPlayer.loopPointReached -= OnVideoLoopPointReached;
        }

        if (m_tapButton != null)
        {
            m_tapButton.onClick.RemoveListener(OnTapContinue);
        }

        if (m_skipButton != null)
        {
            m_skipButton.onClick.RemoveListener(OnClickSkip);
        }

        ReleaseRenderTexture();
    }
}
