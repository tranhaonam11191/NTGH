using System.IO;
using NTGH.UI;
using SS.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.Editor
{
    /// <summary>
    /// Builds the shared SS.UI SettingsPopup prefab + Resources/Screens ScreenReference,
    /// and places BtnSettings (reusing NeonButton_Icon.prefab) in Game.unity.
    /// </summary>
    public static class SettingsPopupBuilder
    {
        private const string FontPath = "Assets/Project/Font/NotoSans/NotoSans-Bold SDF.asset";
        private const string PanelFillPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_panel_fill.png";
        private const string BorderOuterPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_outer.png";
        private const string BorderInnerPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_inner.png";
        private const string CircleFillPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_circle_fill.png";
        private const string CircleBorderPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_circle_border.png";
        private const string CloseIconPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_close.png";
        private const string SettingsIconPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_settings.png";
        private const string NeonIconButtonPrefabPath = "Assets/Project/Prefabs/UI/Buttons/NeonButton_Icon.prefab";

        private const string SettingsFolder = "Assets/Project/Screens/SettingsPopup";
        private const string SettingsPrefabPath = SettingsFolder + "/SettingsPopup.prefab";
        private const string SettingsRefPath = "Assets/Resources/Screens/SettingsPopup.asset";
        private const string GameScenePath = "Assets/Project/Scenes/Game.unity";

        // Neon 90s palette
        private static readonly Color PanelFill = new Color(0.07f, 0.09f, 0.14f, 0.97f);
        private static readonly Color CyanOuter = new Color(0.16f, 0.92f, 0.88f, 1f);
        private static readonly Color GoldInner = new Color(1.00f, 0.78f, 0.30f, 0.35f);
        private static readonly Color RowFill = new Color(0.04f, 0.06f, 0.10f, 0.86f);
        private static readonly Color CrimsonFill = new Color(0.26f, 0.07f, 0.10f, 0.97f);
        private static readonly Color CrimsonOuter = new Color(1.00f, 0.25f, 0.38f, 1f);

        [MenuItem("NTGH/UI/Build Settings Popup & Add Game Settings Button")]
        public static void BuildAll()
        {
            BuildSettingsPopup();
            AddSettingsButtonToGameScene();
        }

        [MenuItem("NTGH/UI/Build Settings Popup Prefab")]
        public static void BuildSettingsPopup()
        {
            EnsureFolder("Assets/Resources/Screens");
            EnsureFolder(SettingsFolder);

            var root = new GameObject("SettingsPopup", typeof(RectTransform), typeof(ScreenController), typeof(SettingsPopupController));
            var rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            // Main Neon Panel (600 x 610 default; resized at runtime between Home 460 and Game 610)
            var panel = CreateNeonPanel(rootRt, "PanelRoot", new Vector2(600f, 610f), PanelFill, CyanOuter, GoldInner, out var borderOuter, out var borderInner);

            // Close Button (top-right corner)
            var btnClose = CreateCloseButton(panel, CyanOuter, new Color(0.08f, 0.12f, 0.18f, 1f));

            // Title: CÀI ĐẶT
            var titleRt = CreateUI("TxtTitle", panel);
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -30f);
            titleRt.sizeDelta = new Vector2(-120f, 54f);
            var txtTitle = MakeTMP(titleRt, "CÀI ĐẶT", 40f, FontStyles.Bold, TextAlignmentOptions.Center, CyanOuter);

            // Divider line under title
            var dividerRt = CreateUI("TitleDivider", panel);
            dividerRt.anchorMin = new Vector2(0f, 1f);
            dividerRt.anchorMax = new Vector2(1f, 1f);
            dividerRt.pivot = new Vector2(0.5f, 1f);
            dividerRt.anchoredPosition = new Vector2(0f, -96f);
            dividerRt.sizeDelta = new Vector2(-56f, 2f);
            var divImg = dividerRt.gameObject.AddComponent<Image>();
            divImg.color = new Color(0.16f, 0.92f, 0.88f, 0.35f);
            divImg.raycastTarget = false;

            // Audio Rows Container
            var sfxRow = CreateAudioToggleRow(panel, "RowSfx", -126f, "ÂM THANH", out var sfxToggle, out var sfxStateText, out var sfxTrackImg, out var sfxKnob);
            var bgmRow = CreateAudioToggleRow(panel, "RowBgm", -246f, "NHẠC NỀN", out var bgmToggle, out var bgmStateText, out var bgmTrackImg, out var bgmKnob);

            // Game-only Exit Section (bottom of panel)
            var exitSectionRt = CreateUI("ExitSection", panel);
            exitSectionRt.anchorMin = new Vector2(0f, 0f);
            exitSectionRt.anchorMax = new Vector2(1f, 0f);
            exitSectionRt.pivot = new Vector2(0.5f, 0f);
            exitSectionRt.anchoredPosition = new Vector2(0f, 36f);
            exitSectionRt.sizeDelta = new Vector2(-56f, 150f);

            var exitDivider = CreateUI("ExitDivider", exitSectionRt);
            exitDivider.anchorMin = new Vector2(0f, 1f);
            exitDivider.anchorMax = new Vector2(1f, 1f);
            exitDivider.pivot = new Vector2(0.5f, 1f);
            exitDivider.anchoredPosition = new Vector2(0f, -4f);
            exitDivider.sizeDelta = new Vector2(0f, 2f);
            var exitDivImg = exitDivider.gameObject.AddComponent<Image>();
            exitDivImg.color = new Color(1f, 0.25f, 0.38f, 0.35f);
            exitDivImg.raycastTarget = false;

            var btnExit = CreateExitGameButton(exitSectionRt);

            borderOuter.SetAsLastSibling();
            borderInner.SetAsLastSibling();
            btnClose.transform.SetAsLastSibling();

            var controller = root.GetComponent<SettingsPopupController>();
            controller.SetEditorReferences(
                panel,
                txtTitle,
                btnClose,
                sfxToggle,
                sfxStateText,
                sfxTrackImg,
                sfxKnob,
                bgmToggle,
                bgmStateText,
                bgmTrackImg,
                bgmKnob,
                exitSectionRt.gameObject,
                btnExit);

            SavePrefabAndScreenRef(root, SettingsPrefabPath, SettingsRefPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SettingsPopupBuilder] Built SettingsPopup prefab and ScreenReference.");
        }

        [MenuItem("NTGH/UI/Add Settings Button To Game Scene")]
        public static void AddSettingsButtonToGameScene()
        {
            var previousScenePath = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            var gc = Object.FindFirstObjectByType<GameController>();
            if (gc == null)
            {
                Debug.LogError("[SettingsPopupBuilder] GameController not found in Game.unity.");
                return;
            }

            var canvasRt = gc.transform.Find("Canvas") as RectTransform;
            if (canvasRt == null)
            {
                Debug.LogError("[SettingsPopupBuilder] GameController/Canvas not found in Game.unity.");
                return;
            }

            // Shift HeartDisplay slightly left to make room for BtnSettings at top-right corner
            var heartRt = canvasRt.Find("HeartDisplay") as RectTransform;
            if (heartRt != null)
            {
                heartRt.anchoredPosition = new Vector2(-104f, -24f);
                EditorUtility.SetDirty(heartRt);
            }

            // Remove existing BtnSettings if rebuilding
            var existing = canvasRt.Find("BtnSettings");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NeonIconButtonPrefabPath);
            if (btnPrefab == null)
            {
                Debug.LogError($"[SettingsPopupBuilder] Missing prefab: {NeonIconButtonPrefabPath}");
                return;
            }

            var btnObj = (GameObject)PrefabUtility.InstantiatePrefab(btnPrefab, canvasRt);
            btnObj.name = "BtnSettings";
            var btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 1f);
            btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(1f, 1f);
            btnRt.sizeDelta = new Vector2(68f, 68f);
            btnRt.anchoredPosition = new Vector2(-24f, -24f);
            btnRt.localScale = Vector3.one;

            // Style to match Home's BtnSettings (settings gear icon + cyan neon border)
            var iconTf = btnRt.Find("Icon");
            if (iconTf != null)
            {
                var iconImg = iconTf.GetComponent<Image>();
                if (iconImg != null)
                {
                    var gearSprite = LoadSprite(SettingsIconPath);
                    if (gearSprite != null) iconImg.sprite = gearSprite;
                    iconImg.color = Color.white;
                    iconImg.preserveAspect = true;
                }
            }

            var outerBorderTf = btnRt.Find("OuterBorder");
            if (outerBorderTf != null)
            {
                var outerImg = outerBorderTf.GetComponent<Image>();
                if (outerImg != null) outerImg.color = CyanOuter;
            }

            var innerBorderTf = btnRt.Find("InnerBorder");
            if (innerBorderTf != null)
            {
                innerBorderTf.gameObject.SetActive(false);
            }

            var fillTf = btnRt.Find("Fill");
            if (fillTf != null)
            {
                var fillImg = fillTf.GetComponent<Image>();
                if (fillImg != null) fillImg.color = new Color(0.08f, 0.12f, 0.18f, 0.94f);
            }

            // Wire persistent OnClick -> GameController.OnClickSettings
            var button = btnObj.GetComponent<Button>();
            if (button != null)
            {
                while (button.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEventTools.RemovePersistentListener(button.onClick, 0);
                }
                UnityEventTools.AddPersistentListener(button.onClick, gc.OnClickSettings);
            }

            btnRt.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SettingsPopupBuilder] Added BtnSettings to Game.unity and saved.");

            if (!string.IsNullOrEmpty(previousScenePath) && previousScenePath != GameScenePath)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }

        private static RectTransform CreateAudioToggleRow(
            RectTransform parent,
            string name,
            float topOffsetY,
            string label,
            out Toggle toggle,
            out TextMeshProUGUI stateText,
            out Image trackImage,
            out RectTransform knobRt)
        {
            var row = CreateUI(name, parent);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, topOffsetY);
            row.sizeDelta = new Vector2(-56f, 96f);
            AssignSliced(row.gameObject.AddComponent<Image>(), PanelFillPath, RowFill);

            var border = CreateUI("Border", row);
            StretchFull(border);
            var bImg = border.gameObject.AddComponent<Image>();
            AssignSliced(bImg, BorderInnerPath, new Color(0.16f, 0.92f, 0.88f, 0.30f));
            bImg.raycastTarget = false;

            // Row label
            var lblRt = CreateUI("TxtLabel", row);
            lblRt.anchorMin = new Vector2(0f, 0f);
            lblRt.anchorMax = new Vector2(0.55f, 1f);
            lblRt.offsetMin = new Vector2(28f, 0f);
            lblRt.offsetMax = new Vector2(0f, 0f);
            MakeTMP(lblRt, label, 28f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

            // State text ("BẬT" / "TẮT")
            var stateRt = CreateUI("TxtState", row);
            stateRt.anchorMin = new Vector2(0.52f, 0f);
            stateRt.anchorMax = new Vector2(0.75f, 1f);
            stateRt.offsetMin = Vector2.zero;
            stateRt.offsetMax = new Vector2(-8f, 0f);
            stateText = MakeTMP(stateRt, "BẬT", 22f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, CyanOuter);

            // Toggle Switch
            var toggleRt = CreateUI("Toggle", row);
            toggleRt.anchorMin = new Vector2(1f, 0.5f);
            toggleRt.anchorMax = new Vector2(1f, 0.5f);
            toggleRt.pivot = new Vector2(1f, 0.5f);
            toggleRt.anchoredPosition = new Vector2(-24f, 0f);
            toggleRt.sizeDelta = new Vector2(104f, 52f);

            trackImage = toggleRt.gameObject.AddComponent<Image>();
            AssignSliced(trackImage, PanelFillPath, new Color(0.10f, 0.52f, 0.50f, 1f));

            var trackBorder = CreateUI("TrackBorder", toggleRt);
            StretchFull(trackBorder);
            var tbImg = trackBorder.gameObject.AddComponent<Image>();
            AssignSliced(tbImg, BorderInnerPath, new Color(0.16f, 0.92f, 0.88f, 0.70f));
            tbImg.raycastTarget = false;

            knobRt = CreateUI("Knob", toggleRt);
            knobRt.anchorMin = new Vector2(0.5f, 0.5f);
            knobRt.anchorMax = new Vector2(0.5f, 0.5f);
            knobRt.pivot = new Vector2(0.5f, 0.5f);
            knobRt.anchoredPosition = new Vector2(24f, 0f);
            knobRt.sizeDelta = new Vector2(38f, 38f);
            var knobImg = knobRt.gameObject.AddComponent<Image>();
            knobImg.sprite = LoadSprite(CircleFillPath);
            knobImg.color = Color.white;
            knobImg.raycastTarget = false;

            toggle = toggleRt.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = trackImage;
            toggle.graphic = null;
            toggle.isOn = true;

            return row;
        }

        private static Button CreateExitGameButton(RectTransform parent)
        {
            var btnRt = CreateUI("BtnExitGame", parent);
            btnRt.anchorMin = new Vector2(0f, 0f);
            btnRt.anchorMax = new Vector2(1f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 6f);
            btnRt.sizeDelta = new Vector2(0f, 110f);

            var bgImg = btnRt.gameObject.AddComponent<Image>();
            AssignSliced(bgImg, PanelFillPath, CrimsonFill);

            var outer = CreateUI("BorderOuter", btnRt);
            StretchFull(outer);
            var outerImg = outer.gameObject.AddComponent<Image>();
            AssignSliced(outerImg, BorderOuterPath, CrimsonOuter);
            outerImg.raycastTarget = false;

            var inner = CreateUI("BorderInner", btnRt);
            StretchFull(inner, 4f);
            var innerImg = inner.gameObject.AddComponent<Image>();
            AssignSliced(innerImg, BorderInnerPath, new Color(1f, 0.65f, 0.70f, 0.35f));
            innerImg.raycastTarget = false;

            var titleRt = CreateUI("TxtExitTitle", btnRt);
            titleRt.anchorMin = new Vector2(0f, 0.44f);
            titleRt.anchorMax = new Vector2(1f, 0.94f);
            titleRt.offsetMin = new Vector2(16f, 0f);
            titleRt.offsetMax = new Vector2(-16f, 0f);
            MakeTMP(titleRt, "THOÁT GAME", 30f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            var subRt = CreateUI("TxtExitSub", btnRt);
            subRt.anchorMin = new Vector2(0f, 0.08f);
            subRt.anchorMax = new Vector2(1f, 0.46f);
            subRt.offsetMin = new Vector2(16f, 0f);
            subRt.offsetMax = new Vector2(-16f, 0f);
            MakeTMP(subRt, "Trừ 1 tim khi thoát giữa trận", 20f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.62f, 0.68f, 1f));

            var btn = btnRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            return btn;
        }

        private static RectTransform CreateNeonPanel(
            RectTransform parent,
            string name,
            Vector2 size,
            Color fillColor,
            Color outerColor,
            Color innerColor,
            out RectTransform borderOuter,
            out RectTransform borderInner)
        {
            var panel = CreateUI(name, parent);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = size;
            AssignSliced(panel.gameObject.AddComponent<Image>(), PanelFillPath, fillColor);

            borderOuter = CreateUI("BorderOuter", panel);
            StretchFull(borderOuter);
            var boImg = borderOuter.gameObject.AddComponent<Image>();
            AssignSliced(boImg, BorderOuterPath, outerColor);
            boImg.raycastTarget = false;

            borderInner = CreateUI("BorderInner", panel);
            StretchFull(borderInner, 6f);
            var biImg = borderInner.gameObject.AddComponent<Image>();
            AssignSliced(biImg, BorderInnerPath, innerColor);
            biImg.raycastTarget = false;

            return panel;
        }

        private static Button CreateCloseButton(RectTransform panel, Color borderColor, Color fillColor)
        {
            var closeRt = CreateUI("BtnClose", panel);
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(0.5f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-12f, -12f);
            closeRt.sizeDelta = new Vector2(68f, 68f);

            var bgImg = closeRt.gameObject.AddComponent<Image>();
            bgImg.sprite = LoadSprite(CircleFillPath);
            bgImg.color = fillColor;

            var ringRt = CreateUI("Border", closeRt);
            StretchFull(ringRt);
            var ringImg = ringRt.gameObject.AddComponent<Image>();
            ringImg.sprite = LoadSprite(CircleBorderPath);
            ringImg.color = borderColor;
            ringImg.raycastTarget = false;

            var iconRt = CreateUI("Icon", closeRt);
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(30f, 30f);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = LoadSprite(CloseIconPath);
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var btn = closeRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            return btn;
        }

        private static void SavePrefabAndScreenRef(GameObject root, string prefabPath, string refPath)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            var screenRef = AssetDatabase.LoadAssetAtPath<ScreenReference>(refPath);
            if (screenRef == null)
            {
                screenRef = ScriptableObject.CreateInstance<ScreenReference>();
                AssetDatabase.CreateAsset(screenRef, refPath);
            }

            var so = new SerializedObject(screenRef);
            var prop = so.FindProperty("m_ScreenPrefab") ?? so.FindProperty("ScreenPrefab") ?? so.FindProperty("screenPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                screenRef.ScreenPrefab = prefab;
            }

            EditorUtility.SetDirty(screenRef);
        }

        private static RectTransform CreateUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static void StretchFull(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static TextMeshProUGUI MakeTMP(
            RectTransform rt,
            string text,
            float size,
            FontStyles style,
            TextAlignmentOptions align,
            Color color)
        {
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void AssignSliced(Image img, string spritePath, Color color)
        {
            img.sprite = LoadSprite(spritePath);
            img.type = Image.Type.Sliced;
            img.color = color;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
                return;

            string parent = Path.GetDirectoryName(assetFolderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(assetFolderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
