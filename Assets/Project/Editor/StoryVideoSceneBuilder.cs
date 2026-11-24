#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

[InitializeOnLoad]
public static class StoryVideoSceneBuilder
{
    private const string ScenePath = "Assets/Project/Scenes/StoryVideo/StoryVideo.unity";
    private const string VideoPath = "Assets/Project/Scenes/StoryVideo/NTGH_Full_Intro_Clean_Lite.mp4";
    private const string CuesPath = "Assets/Project/Scenes/StoryVideo/NTGH_Full_Intro_Cues.json";
    private const string Level01Path = "Assets/Project/Scenes/Data/DauBu/Level01.asset";
    private const string FontPath = "Assets/Project/Font/NotoSans/NotoSans-Bold SDF.asset";

    static StoryVideoSceneBuilder()
    {
        EditorApplication.delayCall += AutoBuildIfMissing;
    }

    private static void AutoBuildIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode)
        {
            return;
        }

        if (!File.Exists(ScenePath))
        {
            BuildSceneAndWireLevel01();
        }
    }

    [MenuItem("NTGH/Story Video/Build StoryVideo Scene & Wire Level01")]
    public static void BuildSceneAndWireLevel01()
    {
        EnsureDirectory(Path.GetDirectoryName(ScenePath));

        Scene previousActive = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        try
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            // 1. Main Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            camGo.AddComponent<AudioListener>();

            // 2. EventSystem (matches Main.unity StandaloneInputModule)
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();

            // 3. VideoPlayer
            var videoGo = new GameObject("VideoPlayer");
            var videoPlayer = videoGo.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.waitForFirstFrame = true;

            // 4. Canvas (720x1600, matchWidthOrHeight = 1, matching Main.unity)
            var canvasGo = new GameObject("Canvas");
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                              AdditionalCanvasShaderChannels.Normal |
                                              AdditionalCanvasShaderChannels.Tangent;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1600f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // 4a. Black Background
            var bgGo = CreateUiObject("Background", canvasGo.transform);
            StretchFull(bgGo.GetComponent<RectTransform>());
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = Color.black;
            bgImg.raycastTarget = false;

            // 4b. RawImage for VideoPlayer (720x1600 centered, matching Main.unity)
            var rawGo = CreateUiObject("RawImage", canvasGo.transform);
            var rawRect = rawGo.GetComponent<RectTransform>();
            rawRect.anchorMin = new Vector2(0.5f, 0.5f);
            rawRect.anchorMax = new Vector2(0.5f, 0.5f);
            rawRect.pivot = new Vector2(0.5f, 0.5f);
            rawRect.anchoredPosition = Vector2.zero;
            rawRect.sizeDelta = new Vector2(720f, 1600f);
            var rawImage = rawGo.AddComponent<RawImage>();
            rawImage.color = new Color(1f, 1f, 1f, 0f);
            rawImage.raycastTarget = false;

            // 4c. 3 Text Objects: Top, Center, Bottom
            var textTop = CreateTmpText(
                name: "TextTop",
                parent: canvasGo.transform,
                font: font,
                fontSize: 42f,
                color: new Color(0.95f, 0.88f, 0.72f, 1f),
                anchorMin: new Vector2(0.06f, 0.78f),
                anchorMax: new Vector2(0.94f, 0.90f),
                alignment: TextAlignmentOptions.Center);

            var textCenter = CreateTmpText(
                name: "TextCenter",
                parent: canvasGo.transform,
                font: font,
                fontSize: 56f,
                color: new Color(0.94f, 0.89f, 0.78f, 1f),
                anchorMin: new Vector2(0.06f, 0.42f),
                anchorMax: new Vector2(0.94f, 0.58f),
                alignment: TextAlignmentOptions.Center);

            var textBottom = CreateTmpText(
                name: "TextBottom",
                parent: canvasGo.transform,
                font: font,
                fontSize: 32f,
                color: new Color(0.96f, 0.94f, 0.89f, 1f),
                anchorMin: new Vector2(0.06f, 0.11f),
                anchorMax: new Vector2(0.94f, 0.27f),
                alignment: TextAlignmentOptions.Center);

            // 4d. Tap To Continue Prompt
            var textTapToContinue = CreateTmpText(
                name: "TextTapToContinue",
                parent: canvasGo.transform,
                font: font,
                fontSize: 34f,
                color: new Color(1f, 0.94f, 0.76f, 1f),
                anchorMin: new Vector2(0.10f, 0.03f),
                anchorMax: new Vector2(0.90f, 0.09f),
                alignment: TextAlignmentOptions.Center);
            textTapToContinue.text = "Tap To Continue";
            textTapToContinue.gameObject.SetActive(false);

            // 4e. Full-screen transparent Tap Button
            var tapGo = CreateUiObject("TapArea", canvasGo.transform);
            StretchFull(tapGo.GetComponent<RectTransform>());
            var tapImg = tapGo.AddComponent<Image>();
            tapImg.color = new Color(0f, 0f, 0f, 0f);
            tapImg.raycastTarget = true;
            var tapButton = tapGo.AddComponent<Button>();
            tapButton.transition = Selectable.Transition.None;

            // 4f. Top-right Skip Button (placed above TapArea so clicks always hit Skip first)
            var skipGo = CreateUiObject("SkipButton", canvasGo.transform);
            var skipRect = skipGo.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(1f, 1f);
            skipRect.anchorMax = new Vector2(1f, 1f);
            skipRect.pivot = new Vector2(1f, 1f);
            skipRect.anchoredPosition = new Vector2(-28f, -44f);
            skipRect.sizeDelta = new Vector2(168f, 64f);

            var skipBg = skipGo.AddComponent<Image>();
            skipBg.color = new Color(0.05f, 0.05f, 0.08f, 0.68f);
            skipBg.raycastTarget = true;

            var skipButton = skipGo.AddComponent<Button>();
            skipButton.targetGraphic = skipBg;

            var skipLabel = CreateTmpText(
                name: "Label",
                parent: skipGo.transform,
                font: font,
                fontSize: 28f,
                color: new Color(0.96f, 0.92f, 0.80f, 1f),
                anchorMin: Vector2.zero,
                anchorMax: Vector2.one,
                alignment: TextAlignmentOptions.Center);
            skipLabel.text = "Skip >>";

            // 5. StoryVideoController
            var ctrlGo = new GameObject("StoryVideoController");
            var controller = ctrlGo.AddComponent<StoryVideoController>();

            var so = new SerializedObject(controller);
            so.FindProperty("m_videoPlayer").objectReferenceValue = videoPlayer;
            so.FindProperty("m_rawImage").objectReferenceValue = rawImage;
            so.FindProperty("m_background").objectReferenceValue = bgImg;
            so.FindProperty("m_textTop").objectReferenceValue = textTop;
            so.FindProperty("m_textCenter").objectReferenceValue = textCenter;
            so.FindProperty("m_textBottom").objectReferenceValue = textBottom;
            so.FindProperty("m_textTapToContinue").objectReferenceValue = textTapToContinue;
            so.FindProperty("m_tapButton").objectReferenceValue = tapButton;
            so.FindProperty("m_skipButton").objectReferenceValue = skipButton;
            so.FindProperty("m_nearEndSeconds").floatValue = 2f;
            so.FindProperty("m_videoFadeInDuration").floatValue = 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[StoryVideoSceneBuilder] Created scene at {ScenePath}");
        }
        finally
        {
            if (previousActive.IsValid())
            {
                SceneManager.SetActiveScene(previousActive);
            }
            EditorSceneManager.CloseScene(scene, true);
        }

        AddSceneToBuildSettings(ScenePath);
        WireLevel01();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("NTGH/Story Video/Reset Watched Story Videos")]
    public static void ResetWatchedStoryVideos()
    {
        User.EnsureLoaded();
        if (User.data != null)
        {
            User.data.EnsureStoryVideoInitialized();
            User.data.watched_story_videos.Clear();
            User.Save();
            Debug.Log("[StoryVideoSceneBuilder] Cleared watched_story_videos in UserData.");
        }
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == scenePath))
        {
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[StoryVideoSceneBuilder] Added {scenePath} to EditorBuildSettings.");
    }

    private static void WireLevel01()
    {
        var level01 = AssetDatabase.LoadAssetAtPath<LevelData>(Level01Path);
        if (level01 == null)
        {
            Debug.LogWarning($"[StoryVideoSceneBuilder] Level01 not found at {Level01Path}.");
            return;
        }

        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
        var cues = AssetDatabase.LoadAssetAtPath<TextAsset>(CuesPath);

        if (clip == null)
        {
            Debug.LogWarning($"[StoryVideoSceneBuilder] VideoClip not imported yet at {VideoPath}. Re-run menu after import.");
            return;
        }

        level01.videoClip = clip;
        level01.textJson = cues;
        EditorUtility.SetDirty(level01);
        Debug.Log($"[StoryVideoSceneBuilder] Assigned '{clip.name}' and '{(cues != null ? cues.name : "null")}' to Level01.");
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static TextMeshProUGUI CreateTmpText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        float fontSize,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        TextAlignmentOptions alignment)
    {
        var go = CreateUiObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
        }
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        tmp.text = string.Empty;
        return tmp;
    }

    private static void EnsureDirectory(string dir)
    {
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
#endif
