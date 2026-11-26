using System.IO;
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
    /// Builds the SS.UI Debug screen prefab + Resources/Screens/Debug.asset ScreenReference,
    /// and places the editor-only BtnDebug in Game.unity.
    /// </summary>
    public static class DebugScreenBuilder
    {
        private const string FontPath = "Assets/Project/Font/NotoSans/NotoSans-Bold SDF.asset";
        private const string PanelFillPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_panel_fill.png";
        private const string BorderOuterPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_outer.png";
        private const string BorderInnerPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_inner.png";
        private const string CircleFillPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_circle_fill.png";
        private const string CircleBorderPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_circle_border.png";
        private const string CloseIconPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_close.png";

        private const string DebugFolder = "Assets/Project/Screens/Debug";
        private const string DebugPrefabPath = DebugFolder + "/Debug.prefab";
        private const string DebugRefPath = "Assets/Resources/Screens/Debug.asset";
        private const string GameScenePath = "Assets/Project/Scenes/Game.unity";

        // Neon 90s palette
        private static readonly Color PanelFill = new Color(0.07f, 0.09f, 0.14f, 0.97f);
        private static readonly Color CyanOuter = new Color(0.16f, 0.92f, 0.88f, 1f);
        private static readonly Color GoldAccent = new Color(1.00f, 0.78f, 0.30f, 1f);
        private static readonly Color GoldInner = new Color(1.00f, 0.78f, 0.30f, 0.35f);
        private static readonly Color RowFill = new Color(0.04f, 0.06f, 0.10f, 0.88f);
        private static readonly Color InputFill = new Color(0.03f, 0.05f, 0.08f, 0.96f);
        private static readonly Color GoldBtnFill = new Color(0.32f, 0.23f, 0.06f, 0.96f);
        private static readonly Color HeartBtnFill = new Color(0.30f, 0.08f, 0.14f, 0.96f);
        private static readonly Color HeartAccent = new Color(1.00f, 0.32f, 0.48f, 1f);
        private static readonly Color CyanBtnFill = new Color(0.06f, 0.25f, 0.28f, 0.96f);
        private static readonly Color DebugBadgeFill = new Color(0.24f, 0.08f, 0.28f, 0.95f);
        private static readonly Color DebugBadgeBorder = new Color(0.95f, 0.36f, 0.88f, 1f);

        [MenuItem("NTGH/UI/Build Debug Screen & Add Game Debug Button")]
        public static void BuildAll()
        {
            BuildDebugScreenPrefab();
            AddDebugButtonToGameScene();
        }

        [MenuItem("NTGH/UI/Build Debug Screen Prefab")]
        public static void BuildDebugScreenPrefab()
        {
            EnsureFolder("Assets/Resources/Screens");
            EnsureFolder(DebugFolder);

            var root = new GameObject("Debug", typeof(RectTransform), typeof(ScreenController), typeof(DebugController));
            var rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            // Main Neon Panel
            var panel = CreateNeonPanel(rootRt, "PanelRoot", new Vector2(620f, 760f), PanelFill, CyanOuter, GoldInner, out var borderOuter, out var borderInner);

            // Close Button
            var btnClose = CreateCloseButton(panel, CyanOuter, new Color(0.08f, 0.12f, 0.18f, 1f));

            // Title
            var titleRt = CreateUI("TxtTitle", panel);
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -28f);
            titleRt.sizeDelta = new Vector2(-120f, 54f);
            var txtTitle = MakeTMP(titleRt, "DEBUG", 40f, FontStyles.Bold, TextAlignmentOptions.Center, CyanOuter);

            // Divider
            var dividerRt = CreateUI("TitleDivider", panel);
            dividerRt.anchorMin = new Vector2(0f, 1f);
            dividerRt.anchorMax = new Vector2(1f, 1f);
            dividerRt.pivot = new Vector2(0.5f, 1f);
            dividerRt.anchoredPosition = new Vector2(0f, -92f);
            dividerRt.sizeDelta = new Vector2(-56f, 2f);
            var divImg = dividerRt.gameObject.AddComponent<Image>();
            divImg.color = new Color(0.16f, 0.92f, 0.88f, 0.35f);
            divImg.raycastTarget = false;

            // Row 1: Gold (+1,000)
            CreateResourceRow(
                panel,
                "RowGold",
                -122f,
                "TIỀN",
                "0 Gold",
                "+1,000",
                GoldBtnFill,
                GoldAccent,
                out var txtGold,
                out var btnAddGold);

            // Row 2: Hearts (+1, max 5)
            CreateResourceRow(
                panel,
                "RowHearts",
                -244f,
                "TIM (MAX 5)",
                "5 / 5 Tim",
                "+1 TIM",
                HeartBtnFill,
                HeartAccent,
                out var txtHeart,
                out var btnAddHeart);

            // Section 3: Level Selector (calculated via ArcsData)
            CreateLevelSection(
                panel,
                -370f,
                out var txtCurrentLevel,
                out var inputLevel,
                out var btnGoToLevel);

            // Status / Feedback Text
            var statusRt = CreateUI("TxtStatus", panel);
            statusRt.anchorMin = new Vector2(0f, 0f);
            statusRt.anchorMax = new Vector2(1f, 0f);
            statusRt.pivot = new Vector2(0.5f, 0f);
            statusRt.anchoredPosition = new Vector2(0f, 24f);
            statusRt.sizeDelta = new Vector2(-56f, 44f);
            var txtStatus = MakeTMP(statusRt, string.Empty, 20f, FontStyles.Bold, TextAlignmentOptions.Center, CyanOuter);

            borderOuter.SetAsLastSibling();
            borderInner.SetAsLastSibling();
            btnClose.transform.SetAsLastSibling();

            var controller = root.GetComponent<DebugController>();
            controller.SetEditorReferences(
                txtTitle,
                btnClose,
                txtGold,
                btnAddGold,
                txtHeart,
                btnAddHeart,
                txtCurrentLevel,
                inputLevel,
                btnGoToLevel,
                txtStatus);

            SavePrefabAndScreenRef(root, DebugPrefabPath, DebugRefPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DebugScreenBuilder] Built Debug.prefab and Resources/Screens/Debug.asset.");
        }

        [MenuItem("NTGH/UI/Add Debug Button To Game Scene")]
        public static void AddDebugButtonToGameScene()
        {
            var previousScenePath = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            var gc = Object.FindFirstObjectByType<GameController>();
            if (gc == null)
            {
                Debug.LogError("[DebugScreenBuilder] GameController not found in Game.unity.");
                return;
            }

            var canvasRt = gc.transform.Find("Canvas") as RectTransform;
            if (canvasRt == null)
            {
                Debug.LogError("[DebugScreenBuilder] GameController/Canvas not found in Game.unity.");
                return;
            }

            // Remove existing BtnDebug if rebuilding
            var existing = canvasRt.Find("BtnDebug");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var btnRt = CreateUI("BtnDebug", canvasRt);
            btnRt.anchorMin = new Vector2(1f, 1f);
            btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(1f, 1f);
            btnRt.sizeDelta = new Vector2(88f, 44f);
            btnRt.anchoredPosition = new Vector2(-24f, -104f);
            btnRt.localScale = Vector3.one;

            var bgImg = btnRt.gameObject.AddComponent<Image>();
            AssignSliced(bgImg, PanelFillPath, DebugBadgeFill);

            var outer = CreateUI("OuterBorder", btnRt);
            StretchFull(outer);
            var outerImg = outer.gameObject.AddComponent<Image>();
            AssignSliced(outerImg, BorderOuterPath, DebugBadgeBorder);
            outerImg.raycastTarget = false;

            var labelRt = CreateUI("TxtLabel", btnRt);
            StretchFull(labelRt);
            MakeTMP(labelRt, "DEBUG", 18f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            var button = btnRt.gameObject.AddComponent<Button>();
            button.targetGraphic = bgImg;

            var debugButton = btnRt.gameObject.AddComponent<DebugButton>();
            debugButton.SetEditorReferences(btnRt.gameObject);

            UnityEventTools.AddPersistentListener(button.onClick, debugButton.OnClickDebugButton);

            btnRt.SetAsLastSibling();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DebugScreenBuilder] Added BtnDebug to Game.unity and saved.");

            if (!string.IsNullOrEmpty(previousScenePath) && previousScenePath != GameScenePath)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }

        private static void CreateResourceRow(
            RectTransform parent,
            string name,
            float topOffsetY,
            string label,
            string initialValue,
            string buttonText,
            Color btnFillColor,
            Color btnBorderColor,
            out TextMeshProUGUI valueText,
            out Button actionButton)
        {
            var row = CreateUI(name, parent);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, topOffsetY);
            row.sizeDelta = new Vector2(-56f, 104f);
            AssignSliced(row.gameObject.AddComponent<Image>(), PanelFillPath, RowFill);

            var border = CreateUI("Border", row);
            StretchFull(border);
            var bImg = border.gameObject.AddComponent<Image>();
            AssignSliced(bImg, BorderInnerPath, new Color(btnBorderColor.r, btnBorderColor.g, btnBorderColor.b, 0.35f));
            bImg.raycastTarget = false;

            var lblRt = CreateUI("TxtLabel", row);
            lblRt.anchorMin = new Vector2(0f, 0.52f);
            lblRt.anchorMax = new Vector2(0.62f, 0.94f);
            lblRt.offsetMin = new Vector2(24f, 0f);
            lblRt.offsetMax = Vector2.zero;
            MakeTMP(lblRt, label, 20f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.70f, 0.78f, 0.88f, 1f));

            var valRt = CreateUI("TxtValue", row);
            valRt.anchorMin = new Vector2(0f, 0.08f);
            valRt.anchorMax = new Vector2(0.62f, 0.56f);
            valRt.offsetMin = new Vector2(24f, 0f);
            valRt.offsetMax = Vector2.zero;
            valueText = MakeTMP(valRt, initialValue, 28f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, btnBorderColor);

            actionButton = CreateActionButton(
                row,
                "BtnAction",
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-20f, 0f),
                new Vector2(170f, 64f),
                buttonText,
                24f,
                btnFillColor,
                btnBorderColor);
        }

        private static void CreateLevelSection(
            RectTransform parent,
            float topOffsetY,
            out TextMeshProUGUI currentLevelText,
            out TMP_InputField levelInputField,
            out Button goToLevelButton)
        {
            var box = CreateUI("LevelSection", parent);
            box.anchorMin = new Vector2(0f, 1f);
            box.anchorMax = new Vector2(1f, 1f);
            box.pivot = new Vector2(0.5f, 1f);
            box.anchoredPosition = new Vector2(0f, topOffsetY);
            box.sizeDelta = new Vector2(-56f, 296f);
            AssignSliced(box.gameObject.AddComponent<Image>(), PanelFillPath, RowFill);

            var border = CreateUI("Border", box);
            StretchFull(border);
            var bImg = border.gameObject.AddComponent<Image>();
            AssignSliced(bImg, BorderInnerPath, new Color(0.16f, 0.92f, 0.88f, 0.35f));
            bImg.raycastTarget = false;

            // Section Title
            var headerRt = CreateUI("TxtSectionHeader", box);
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -16f);
            headerRt.sizeDelta = new Vector2(-40f, 34f);
            MakeTMP(headerRt, "CHỌN LEVEL (THEO ARCSDATA)", 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, CyanOuter);

            // Current Level Info
            var infoRt = CreateUI("TxtCurrentLevel", box);
            infoRt.anchorMin = new Vector2(0f, 1f);
            infoRt.anchorMax = new Vector2(1f, 1f);
            infoRt.pivot = new Vector2(0.5f, 1f);
            infoRt.anchoredPosition = new Vector2(0f, -54f);
            infoRt.sizeDelta = new Vector2(-40f, 44f);
            currentLevelText = MakeTMP(infoRt, "Hiện tại: Lv 1 (Arc 1 - Lv 1)", 20f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);
            currentLevelText.textWrappingMode = TextWrappingModes.Normal;

            // Input Field
            var inputRt = CreateUI("InputLevel", box);
            inputRt.anchorMin = new Vector2(0f, 1f);
            inputRt.anchorMax = new Vector2(1f, 1f);
            inputRt.pivot = new Vector2(0.5f, 1f);
            inputRt.anchoredPosition = new Vector2(0f, -112f);
            inputRt.sizeDelta = new Vector2(-40f, 68f);
            var inputBg = inputRt.gameObject.AddComponent<Image>();
            AssignSliced(inputBg, PanelFillPath, InputFill);

            var inputBorder = CreateUI("Border", inputRt);
            StretchFull(inputBorder);
            var ibImg = inputBorder.gameObject.AddComponent<Image>();
            AssignSliced(ibImg, BorderInnerPath, new Color(0.16f, 0.92f, 0.88f, 0.55f));
            ibImg.raycastTarget = false;

            var textAreaRt = CreateUI("Text Area", inputRt);
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(18f, 8f);
            textAreaRt.offsetMax = new Vector2(-18f, -8f);
            textAreaRt.gameObject.AddComponent<RectMask2D>();

            var placeholderRt = CreateUI("Placeholder", textAreaRt);
            StretchFull(placeholderRt);
            var placeholderTmp = MakeTMP(placeholderRt, "Nhập số level (VD: 3) hoặc Arc-Lv (VD: 1-3)", 20f, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, new Color(0.55f, 0.62f, 0.72f, 0.85f));

            var textRt = CreateUI("Text", textAreaRt);
            StretchFull(textRt);
            var textTmp = MakeTMP(textRt, string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

            levelInputField = inputRt.gameObject.AddComponent<TMP_InputField>();
            levelInputField.targetGraphic = inputBg;
            levelInputField.textViewport = textAreaRt;
            levelInputField.textComponent = textTmp;
            levelInputField.placeholder = placeholderTmp;
            levelInputField.contentType = TMP_InputField.ContentType.Standard;
            levelInputField.lineType = TMP_InputField.LineType.SingleLine;
            levelInputField.characterLimit = 16;
            levelInputField.caretColor = CyanOuter;

            // Go To Level Button
            goToLevelButton = CreateActionButton(
                box,
                "BtnGoToLevel",
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 20f),
                new Vector2(-40f, 74f),
                "ĐI TỚI LEVEL",
                26f,
                CyanBtnFill,
                CyanOuter);
        }

        private static Button CreateActionButton(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 sizeDelta,
            string label,
            float fontSize,
            Color fillColor,
            Color borderColor)
        {
            var btnRt = CreateUI(name, parent);
            btnRt.anchorMin = anchorMin;
            btnRt.anchorMax = anchorMax;
            btnRt.pivot = pivot;
            btnRt.anchoredPosition = anchoredPos;
            btnRt.sizeDelta = sizeDelta;

            var bgImg = btnRt.gameObject.AddComponent<Image>();
            AssignSliced(bgImg, PanelFillPath, fillColor);

            var outer = CreateUI("BorderOuter", btnRt);
            StretchFull(outer);
            var outerImg = outer.gameObject.AddComponent<Image>();
            AssignSliced(outerImg, BorderOuterPath, borderColor);
            outerImg.raycastTarget = false;

            var lblRt = CreateUI("TxtLabel", btnRt);
            StretchFull(lblRt);
            MakeTMP(lblRt, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

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
