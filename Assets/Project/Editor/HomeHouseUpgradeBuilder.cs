using NTGH.UI;
using SS.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NTGH.EditorTools
{
    public static class HomeHouseUpgradeBuilder
    {
        private const string NeonDir = "Assets/Project/Sprites/UI/Neon90s";
        private const string BasePopupPath = "Assets/Project/Prefabs/UI/Popups/NeonPopup_Base.prefab";
        private const string NeonButtonTextPath = "Assets/Project/Prefabs/UI/Buttons/NeonButton_Text.prefab";
        private const string PopupDir = "Assets/Project/Screens/NotEnoughGoldPopup";
        private const string PopupPrefabPath = PopupDir + "/NotEnoughGoldPopup.prefab";
        private const string ResourcesScreensDir = "Assets/Resources/Screens";
        private const string FontPath = "Assets/Project/Font/LibreBaskerville-Regular SDF.asset";
        private const string HomeScenePath = "Assets/Project/Scenes/Home/Home.unity";

        [MenuItem("NTGH/Build House Upgrade Mode & NotEnoughGold Popup")]
        public static void BuildAll()
        {
            GameObject notEnoughPrefab = BuildNotEnoughGoldPopupVariant();
            CreateScreenReference(notEnoughPrefab, NotEnoughGoldPopupController.ScreenResourceName);
            SetupHomeScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HomeHouseUpgradeBuilder] Built NotEnoughGoldPopup (Prefab Variant + ScreenReference) and wired Home.unity House Upgrade Mode!");
        }

        private static GameObject BuildNotEnoughGoldPopupVariant()
        {
            EnsureFolder(PopupDir);

            GameObject basePopup = AssetDatabase.LoadAssetAtPath<GameObject>(BasePopupPath);
            if (basePopup == null)
            {
                Debug.LogError($"[HomeHouseUpgradeBuilder] Base popup not found at {BasePopupPath}. Run ItemSelectPopupBuilder first.");
                return null;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite spPanelFill = LoadSprite($"{NeonDir}/ui_neon_panel_fill.png");
            Sprite spOuterBorder = LoadSprite($"{NeonDir}/ui_neon_border_outer.png");
            Sprite spInnerBorder = LoadSprite($"{NeonDir}/ui_neon_border_inner.png");
            Sprite spLabelFill = LoadSprite($"{NeonDir}/ui_neon_label_fill.png");
            Sprite spAdIcon = LoadSprite($"{NeonDir}/icon_neon_ad.png");

            GameObject variantInstance = (GameObject)PrefabUtility.InstantiatePrefab(basePopup);
            variantInstance.name = "NotEnoughGoldPopup";

            NeonPopupBase popupBase = variantInstance.GetComponent<NeonPopupBase>();
            Transform panelTransform = variantInstance.transform.Find("Panel");
            if (panelTransform != null)
            {
                RectTransform panelRt = panelTransform.GetComponent<RectTransform>();
                panelRt.sizeDelta = new Vector2(620f, 560f);
            }

            RectTransform contentContainer = popupBase.ContentContainer;
            LayoutElement contentLe = contentContainer.GetComponent<LayoutElement>();
            if (contentLe != null)
            {
                contentLe.minHeight = 280f;
            }

            // 1. Message Card Container
            RectTransform msgBox = CreateRect("MessageBox", contentContainer);
            LayoutElement msgBoxLe = msgBox.gameObject.AddComponent<LayoutElement>();
            msgBoxLe.flexibleHeight = 1f;
            msgBoxLe.minHeight = 160f;

            Image msgBg = CreateSlicedImage("MessageBg", msgBox, spPanelFill, new Color(0.03f, 0.05f, 0.09f, 0.85f));
            StretchFull(msgBg.rectTransform);
            msgBg.raycastTarget = false;

            Image msgBorder = CreateSlicedImage("MessageBorder", msgBox, spInnerBorder, new Color(0.16f, 0.92f, 0.88f, 0.45f));
            StretchFull(msgBorder.rectTransform, 2f, 2f, 2f, 2f);
            msgBorder.raycastTarget = false;

            TextMeshProUGUI messageText = CreateTMP(
                "MessageText",
                msgBox,
                font,
                "Bạn không đủ tiền để nâng cấp.\nXem quảng cáo để nhận thêm tiền!",
                22f,
                FontStyles.Normal,
                new Color(0.96f, 0.95f, 0.90f, 1f),
                TextAlignmentOptions.Center);
            messageText.textWrappingMode = TextWrappingModes.Normal;
            StretchFull(messageText.rectTransform, 22f, 22f, 16f, 16f);

            // 2. Action Buttons Row
            RectTransform actionArea = CreateRect("ActionArea", contentContainer);
            LayoutElement actionLe = actionArea.gameObject.AddComponent<LayoutElement>();
            actionLe.preferredHeight = 148f;
            actionLe.minHeight = 148f;
            actionLe.flexibleHeight = 0f;

            VerticalLayoutGroup actionVlg = actionArea.gameObject.AddComponent<VerticalLayoutGroup>();
            actionVlg.spacing = 14f;
            actionVlg.childAlignment = TextAnchor.MiddleCenter;
            actionVlg.childControlWidth = true;
            actionVlg.childControlHeight = false;
            actionVlg.childForceExpandWidth = true;
            actionVlg.childForceExpandHeight = false;

            // Watch Ad Primary CTA Button
            Button watchAdBtn = CreateNeonCtaButton(
                "BtnWatchAd",
                actionArea,
                font,
                spPanelFill,
                spOuterBorder,
                spInnerBorder,
                spAdIcon,
                "XEM QC  +500",
                76f,
                24f,
                new Color(0.16f, 0.06f, 0.26f, 0.96f),
                new Color(0.76f, 0.28f, 1.00f, 1f),
                new Color(0.16f, 0.92f, 0.88f, 1f),
                new Color(1.00f, 0.96f, 0.88f, 1f),
                out TextMeshProUGUI watchAdLabel);

            // Cancel Secondary Button
            Button cancelBtn = CreateNeonCtaButton(
                "BtnCancel",
                actionArea,
                font,
                spLabelFill,
                spOuterBorder,
                null,
                null,
                "ĐỂ SAU",
                56f,
                20f,
                new Color(0.08f, 0.10f, 0.14f, 0.90f),
                new Color(0.36f, 0.44f, 0.52f, 0.85f),
                new Color(0.36f, 0.44f, 0.52f, 0.40f),
                new Color(0.78f, 0.82f, 0.86f, 0.95f),
                out _);

            NotEnoughGoldPopupController controller = variantInstance.AddComponent<NotEnoughGoldPopupController>();
            controller.ConfigureReferences(popupBase, messageText, watchAdBtn, watchAdLabel, cancelBtn);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(variantInstance, PopupPrefabPath);
            Object.DestroyImmediate(variantInstance);
            return saved;
        }

        private static void SetupHomeScene()
        {
            Scene scene = EditorSceneManager.OpenScene(HomeScenePath, OpenSceneMode.Single);
            HomeController home = Object.FindFirstObjectByType<HomeController>();
            if (home == null)
            {
                Debug.LogError("[HomeHouseUpgradeBuilder] HomeController not found in Home.unity");
                return;
            }

            Transform canvasTr = home.transform.Find("Canvas");
            Transform homeHudTr = canvasTr != null ? canvasTr.Find("HomeHUD") : null;
            Transform homeBackdropTr = canvasTr != null ? canvasTr.Find("HomeBackdrop") : null;
            if (homeHudTr == null || homeBackdropTr == null)
            {
                Debug.LogError("[HomeHouseUpgradeBuilder] Missing HomeHUD or HomeBackdrop in Home.unity");
                return;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite spPanelFill = LoadSprite($"{NeonDir}/ui_neon_panel_fill.png");
            Sprite spOuterBorder = LoadSprite($"{NeonDir}/ui_neon_border_outer.png");
            Sprite spInnerBorder = LoadSprite($"{NeonDir}/ui_neon_border_inner.png");
            Sprite spDollarIcon = LoadSprite($"{NeonDir}/icon_neon_dollar.png");
            Sprite spCheckIcon = LoadSprite($"{NeonDir}/icon_neon_check.png");

            // 1. Reposition BtnPlay to bottom-left (symmetrical with BtnHouse on bottom-right)
            RectTransform btnPlayRt = homeHudTr.Find("BtnPlay") as RectTransform;
            if (btnPlayRt != null)
            {
                btnPlayRt.anchorMin = new Vector2(0.5f, 0f);
                btnPlayRt.anchorMax = new Vector2(0.5f, 0f);
                btnPlayRt.pivot = new Vector2(0.5f, 0.5f);
                btnPlayRt.sizeDelta = new Vector2(300f, 114f);
                btnPlayRt.anchoredPosition = new Vector2(-164f, 225f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(btnPlayRt);
            }

            // 2. Create or update BtnHouse on bottom-right using NeonButton_Text prefab
            Transform existingHouseTr = homeHudTr.Find("BtnHouse");
            GameObject btnHouseGo;
            if (existingHouseTr != null)
            {
                btnHouseGo = existingHouseTr.gameObject;
            }
            else
            {
                GameObject neonBtnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NeonButtonTextPath);
                btnHouseGo = neonBtnPrefab != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(neonBtnPrefab, homeHudTr)
                    : new GameObject("BtnHouse", typeof(RectTransform), typeof(Button), typeof(NeonButtonStyle));
                btnHouseGo.name = "BtnHouse";
            }

            RectTransform btnHouseRt = btnHouseGo.GetComponent<RectTransform>();
            btnHouseRt.anchorMin = new Vector2(0.5f, 0f);
            btnHouseRt.anchorMax = new Vector2(0.5f, 0f);
            btnHouseRt.pivot = new Vector2(0.5f, 0.5f);
            btnHouseRt.sizeDelta = new Vector2(300f, 114f);
            btnHouseRt.anchoredPosition = new Vector2(164f, 225f);
            btnHouseRt.localScale = Vector3.one;
            PrefabUtility.RecordPrefabInstancePropertyModifications(btnHouseRt);

            NeonButtonStyle houseStyle = btnHouseGo.GetComponent<NeonButtonStyle>();
            if (houseStyle != null)
            {
                houseStyle.SetPalette(
                    fillColor: new Color(0.05f, 0.16f, 0.22f, 0.94f),
                    outerBorderColor: new Color(0.16f, 0.92f, 0.88f, 1f),
                    innerBorderColor: new Color(1.00f, 0.80f, 0.32f, 1f),
                    iconColor: Color.white,
                    labelColor: new Color(1.00f, 0.96f, 0.86f, 1f));
                houseStyle.SetLabel("HOUSE");
                houseStyle.SetPulseEnabled(false);
                EditorUtility.SetDirty(houseStyle);
                PrefabUtility.RecordPrefabInstancePropertyModifications(houseStyle);
                if (houseStyle.LabelText != null)
                    PrefabUtility.RecordPrefabInstancePropertyModifications(houseStyle.LabelText);
            }

            Button btnHouse = btnHouseGo.GetComponent<Button>();
            if (btnHouse != null)
            {
                ClearPersistentOnClick(btnHouse);
                UnityEventTools.AddPersistentListener(btnHouse.onClick, home.OnClickHouse);
                EditorUtility.SetDirty(btnHouse);
                PrefabUtility.RecordPrefabInstancePropertyModifications(btnHouse);
            }

            if (btnHouseGo.GetComponent<ButtonAudio>() == null)
            {
                btnHouseGo.AddComponent<ButtonAudio>();
            }

            // 3. Create or rebuild BtnHouseBack at top-right of HomeHUD
            Transform existingBackTr = homeHudTr.Find("BtnHouseBack");
            if (existingBackTr != null)
            {
                Object.DestroyImmediate(existingBackTr.gameObject);
            }

            Button btnBack = CreateNeonCtaButton(
                "BtnHouseBack",
                homeHudTr,
                font,
                spPanelFill,
                spOuterBorder,
                spInnerBorder,
                null,
                "◀  QUAY LẠI",
                68f,
                20f,
                new Color(0.14f, 0.06f, 0.16f, 0.94f),
                new Color(0.16f, 0.92f, 0.88f, 1f),
                new Color(1.00f, 0.78f, 0.30f, 0.95f),
                new Color(1.00f, 0.96f, 0.86f, 1f),
                out _);

            RectTransform btnBackRt = btnBack.GetComponent<RectTransform>();
            btnBackRt.anchorMin = new Vector2(1f, 1f);
            btnBackRt.anchorMax = new Vector2(1f, 1f);
            btnBackRt.pivot = new Vector2(1f, 1f);
            btnBackRt.sizeDelta = new Vector2(174f, 68f);
            btnBackRt.anchoredPosition = new Vector2(-28f, -44f);
            btnBack.gameObject.SetActive(false);

            // 4. Create or rebuild HouseUpgradeOverlay inside HomeHUD with the exact same AspectRatioFitter
            // as HomeBackdrop so buttons stay aligned 1:1 with Awning, Door, Wall AND render above DauBu!
            Transform oldBackdropOverlay = homeBackdropTr.Find("HouseUpgradeOverlay");
            if (oldBackdropOverlay != null)
            {
                Object.DestroyImmediate(oldBackdropOverlay.gameObject);
            }

            Transform existingOverlay = homeHudTr.Find("HouseUpgradeOverlay");
            if (existingOverlay != null)
            {
                Object.DestroyImmediate(existingOverlay.gameObject);
            }

            RectTransform overlayRt = CreateRect("HouseUpgradeOverlay", homeHudTr);
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.pivot = new Vector2(0.5f, 0.5f);
            overlayRt.anchoredPosition = Vector2.zero;
            overlayRt.sizeDelta = Vector2.zero;

            AspectRatioFitter backdropFitter = homeBackdropTr.GetComponent<AspectRatioFitter>();
            if (backdropFitter != null)
            {
                AspectRatioFitter overlayFitter = overlayRt.gameObject.AddComponent<AspectRatioFitter>();
                overlayFitter.aspectMode = backdropFitter.aspectMode;
                overlayFitter.aspectRatio = backdropFitter.aspectRatio;
            }

            // Ensure BtnHouseBack renders above HouseUpgradeOverlay
            btnBackRt.SetAsLastSibling();

            HousePartUpgradeButton awningBtn = CreateHousePartUpgradeButton(
                "BtnUpgradeAwning",
                overlayRt,
                HomeHouseTheme.Part.Awning,
                new Vector2(0.30f, 0.78f),
                font,
                spPanelFill,
                spOuterBorder,
                spInnerBorder,
                spDollarIcon,
                spCheckIcon);

            HousePartUpgradeButton doorBtn = CreateHousePartUpgradeButton(
                "BtnUpgradeDoor",
                overlayRt,
                HomeHouseTheme.Part.Door,
                new Vector2(0.29f, 0.54f),
                font,
                spPanelFill,
                spOuterBorder,
                spInnerBorder,
                spDollarIcon,
                spCheckIcon);

            HousePartUpgradeButton wallBtn = CreateHousePartUpgradeButton(
                "BtnUpgradeWall",
                overlayRt,
                HomeHouseTheme.Part.Wall,
                new Vector2(0.68f, 0.63f),
                font,
                spPanelFill,
                spOuterBorder,
                spInnerBorder,
                spDollarIcon,
                spCheckIcon);

            awningBtn.gameObject.SetActive(false);
            doorBtn.gameObject.SetActive(false);
            wallBtn.gameObject.SetActive(false);

            // 5. Wire HomeHouseUpgradeMode on HomeController
            HomeHouseUpgradeMode upgradeMode = home.GetComponent<HomeHouseUpgradeMode>();
            if (upgradeMode == null)
            {
                upgradeMode = home.gameObject.AddComponent<HomeHouseUpgradeMode>();
            }

            RectTransform btnSettingsRt = homeHudTr.Find("BtnSettings") as RectTransform;
            RectTransform currencyRt = homeHudTr.Find("CurrencyDisplay") as RectTransform;
            RectTransform leftMenuRt = homeHudTr.Find("LeftMenuGroup") as RectTransform;
            HomeHouseTheme houseTheme = homeBackdropTr.GetComponent<HomeHouseTheme>();

            upgradeMode.ConfigureReferences(
                homeController: home,
                houseTheme: houseTheme,
                topLeftHud: btnSettingsRt,
                topRightHud: currencyRt,
                leftMenuGroup: leftMenuRt,
                bottomLeftBtn: btnPlayRt,
                bottomRightBtn: btnHouseRt,
                backButtonRect: btnBackRt,
                backButton: btnBack,
                awningUpgradeBtn: awningBtn,
                doorUpgradeBtn: doorBtn,
                wallUpgradeBtn: wallBtn);

            SerializedObject homeSo = new SerializedObject(home);
            SerializedProperty modeProp = homeSo.FindProperty("m_houseUpgradeMode");
            if (modeProp != null)
            {
                modeProp.objectReferenceValue = upgradeMode;
                homeSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(home);
            EditorUtility.SetDirty(upgradeMode);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static HousePartUpgradeButton CreateHousePartUpgradeButton(
            string name,
            Transform parent,
            HomeHouseTheme.Part part,
            Vector2 normalizedAnchor,
            TMP_FontAsset font,
            Sprite spPanelFill,
            Sprite spOuterBorder,
            Sprite spInnerBorder,
            Sprite spDollarIcon,
            Sprite spCheckIcon)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Button), typeof(ButtonAudio), typeof(NeonButtonStyle), typeof(HousePartUpgradeButton));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = normalizedAnchor;
            rt.anchorMax = normalizedAnchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(184f, 102f);
            rt.anchoredPosition = Vector2.zero;

            Image fill = CreateSlicedImage("Fill", rt, spPanelFill, new Color(0.05f, 0.16f, 0.20f, 0.94f));
            StretchFull(fill.rectTransform);
            fill.raycastTarget = true;

            Image outer = CreateSlicedImage("OuterBorder", rt, spOuterBorder, new Color(0.16f, 0.92f, 0.88f, 1f));
            StretchFull(outer.rectTransform, -5f, -5f, -5f, -5f);
            outer.raycastTarget = false;

            Image inner = CreateSlicedImage("InnerBorder", rt, spInnerBorder, new Color(1.00f, 0.80f, 0.30f, 1f));
            StretchFull(inner.rectTransform, 4f, 4f, 4f, 4f);
            inner.raycastTarget = false;

            // Vertical content stack
            RectTransform contentRt = CreateRect("Content", rt);
            StretchFull(contentRt, 12f, 12f, 8f, 8f);
            VerticalLayoutGroup vlg = contentRt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            TextMeshProUGUI partNameText = CreateTMP(
                "PartName",
                contentRt,
                font,
                HomeHouseTheme.GetPartDisplayName(part).ToUpperInvariant(),
                18f,
                FontStyles.Bold,
                new Color(1.00f, 0.96f, 0.86f, 1f),
                TextAlignmentOptions.Center);
            LayoutElement nameLe = partNameText.gameObject.AddComponent<LayoutElement>();
            nameLe.preferredHeight = 24f;

            TextMeshProUGUI levelText = CreateTMP(
                "LevelText",
                contentRt,
                font,
                "CẤP 1 → 2",
                13f,
                FontStyles.Normal,
                new Color(0.55f, 0.92f, 0.92f, 0.95f),
                TextAlignmentOptions.Center);
            LayoutElement lvlLe = levelText.gameObject.AddComponent<LayoutElement>();
            lvlLe.preferredHeight = 18f;

            // Price Row (Icon + Amount)
            RectTransform priceRow = CreateRect("PriceRow", contentRt);
            LayoutElement priceRowLe = priceRow.gameObject.AddComponent<LayoutElement>();
            priceRowLe.preferredHeight = 32f;

            HorizontalLayoutGroup priceHlg = priceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            priceHlg.spacing = 6f;
            priceHlg.childAlignment = TextAnchor.MiddleCenter;
            priceHlg.childControlWidth = false;
            priceHlg.childControlHeight = false;
            priceHlg.childForceExpandWidth = false;
            priceHlg.childForceExpandHeight = false;

            Image iconImg = new GameObject("ActionIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            iconImg.transform.SetParent(priceRow, false);
            iconImg.sprite = spDollarIcon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.color = new Color(1.00f, 0.90f, 0.45f, 1f);
            iconImg.rectTransform.sizeDelta = new Vector2(22f, 22f);

            TextMeshProUGUI priceText = CreateTMP(
                "PriceText",
                priceRow,
                font,
                "300",
                21f,
                FontStyles.Bold,
                new Color(1.00f, 0.90f, 0.45f, 1f),
                TextAlignmentOptions.MidlineLeft);
            ContentSizeFitter priceCsf = priceText.gameObject.AddComponent<ContentSizeFitter>();
            priceCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            priceCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = fill;
            btn.transition = Selectable.Transition.None;

            NeonButtonStyle style = go.GetComponent<NeonButtonStyle>();
            style.ConfigureReferences(fill, outer, inner, iconImg, partNameText);
            style.SetPalette(
                new Color(0.05f, 0.16f, 0.20f, 0.94f),
                new Color(0.16f, 0.92f, 0.88f, 1f),
                new Color(1.00f, 0.80f, 0.30f, 1f),
                new Color(1.00f, 0.90f, 0.45f, 1f),
                new Color(1.00f, 0.96f, 0.86f, 1f));

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            HousePartUpgradeButton upgradeBtn = go.GetComponent<HousePartUpgradeButton>();
            upgradeBtn.ConfigureReferences(
                part,
                btn,
                style,
                cg,
                partNameText,
                levelText,
                iconImg,
                priceText,
                priceRow,
                spDollarIcon,
                spCheckIcon);

            return upgradeBtn;
        }

        private static Button CreateNeonCtaButton(
            string name,
            Transform parent,
            TMP_FontAsset font,
            Sprite fillSprite,
            Sprite outerSprite,
            Sprite innerSprite,
            Sprite iconSprite,
            string label,
            float height,
            float fontSize,
            Color fillColor,
            Color outerColor,
            Color innerColor,
            Color labelColor,
            out TextMeshProUGUI labelText)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(ButtonAudio), typeof(NeonButtonStyle));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, height);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;

            Image fill = CreateSlicedImage("Fill", rt, fillSprite, fillColor);
            StretchFull(fill.rectTransform);
            fill.raycastTarget = true;

            Image outer = CreateSlicedImage("OuterBorder", rt, outerSprite, outerColor);
            StretchFull(outer.rectTransform, -5f, -5f, -5f, -5f);
            outer.raycastTarget = false;

            Image inner = null;
            if (innerSprite != null)
            {
                inner = CreateSlicedImage("InnerBorder", rt, innerSprite, innerColor);
                StretchFull(inner.rectTransform, 4f, 4f, 4f, 4f);
                inner.raycastTarget = false;
            }

            RectTransform rowRt = CreateRect("Row", rt);
            StretchFull(rowRt, 16f, 16f, 6f, 6f);
            HorizontalLayoutGroup hlg = rowRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Image iconImg = null;
            if (iconSprite != null)
            {
                iconImg = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                iconImg.transform.SetParent(rowRt, false);
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                iconImg.color = labelColor;
                iconImg.rectTransform.sizeDelta = new Vector2(30f, 30f);
            }

            labelText = CreateTMP("Label", rowRt, font, label, fontSize, FontStyles.Bold, labelColor, TextAlignmentOptions.Center);
            ContentSizeFitter csf = labelText.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = fill;
            btn.transition = Selectable.Transition.None;

            NeonButtonStyle style = go.GetComponent<NeonButtonStyle>();
            style.ConfigureReferences(fill, outer, inner, iconImg, labelText);
            style.SetPalette(fillColor, outerColor, innerColor, labelColor, labelColor);

            return btn;
        }

        private static void CreateScreenReference(GameObject prefab, string resourceName)
        {
            if (prefab == null) return;
            EnsureFolder(ResourcesScreensDir);
            string refPath = $"{ResourcesScreensDir}/{resourceName}.asset";

            ScreenReference screenRef = AssetDatabase.LoadAssetAtPath<ScreenReference>(refPath);
            if (screenRef == null)
            {
                screenRef = ScriptableObject.CreateInstance<ScreenReference>();
                AssetDatabase.CreateAsset(screenRef, refPath);
            }

            screenRef.ScreenPrefab = prefab;
            EditorUtility.SetDirty(screenRef);
        }

        private static void ClearPersistentOnClick(Button button)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image CreateSlicedImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI CreateTMP(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string text,
            float fontSize,
            FontStyles fontStyle,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
