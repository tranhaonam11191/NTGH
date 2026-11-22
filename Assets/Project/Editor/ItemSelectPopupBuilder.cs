using System.Collections.Generic;
using NTGH.Demo.OutfitSystem;
using NTGH.UI;
using SS.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NTGH.EditorTools
{
    public static class ItemSelectPopupBuilder
    {
        private const string NeonDir = "Assets/Project/Sprites/UI/Neon90s";
        private const string BasePopupDir = "Assets/Project/Prefabs/UI/Popups";
        private const string BasePopupPath = BasePopupDir + "/NeonPopup_Base.prefab";
        private const string VariantDir = "Assets/Project/Screens/ItemSelectPopup";
        private const string VariantPath = VariantDir + "/ItemSelectPopup.prefab";
        private const string CardPrefabPath = "Assets/Project/Screens/HomeCustomizePopup/CustomizeItemCard.prefab";
        private const string ResourcesScreensDir = "Assets/Resources/Screens";
        private const string CatalogsDir = "Assets/Project/Data/ItemCatalogs";
        private const string FontPath = "Assets/Project/Font/LibreBaskerville-Regular SDF.asset";
        private const string CurrencyPrefabPath = "Assets/Project/Prefabs/UI/CurrencyDisplay.prefab";
        private const string HomeHouseDir = "Assets/Project/Textures/HomeHouse";
        private const string HomeScenePath = "Assets/Project/Scenes/Home/Home.unity";

        [MenuItem("NTGH/Build Item Select Popup Variant & Catalogs")]
        public static void BuildAll()
        {
            GameObject basePopup = BuildNeonPopupBasePrefab();
            GameObject variantPopup = BuildItemSelectVariantPrefab(basePopup);
            CreateScreenReference(variantPopup);

            ItemCatalogSO outfitCatalog = BuildOutfitCatalog();
            ItemCatalogSO houseCatalog = BuildHouseCatalog();
            ItemCatalogSO weaponCatalog = BuildWeaponCatalog();

            WireHomeSceneCatalogs(outfitCatalog, houseCatalog, weaponCatalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ItemSelectPopupBuilder] Built NeonPopup_Base, ItemSelectPopup (Prefab Variant), ScreenReference, 3 ItemCatalogSO assets, and wired Home.unity!");
        }

        private static GameObject BuildNeonPopupBasePrefab()
        {
            EnsureFolder(BasePopupDir);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite spPanelFill = LoadSprite($"{NeonDir}/ui_neon_panel_fill.png");
            Sprite spOuterBorder = LoadSprite($"{NeonDir}/ui_neon_border_outer.png");
            Sprite spInnerBorder = LoadSprite($"{NeonDir}/ui_neon_border_inner.png");
            Sprite spLabelFill = LoadSprite($"{NeonDir}/ui_neon_label_fill.png");
            Sprite spClose = LoadSprite($"{NeonDir}/icon_neon_close.png");

            GameObject root = new GameObject("NeonPopup_Base", typeof(RectTransform), typeof(ScreenController));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            // Centered Neon 90s Modal Panel
            RectTransform panelRt = CreateRect("Panel", rootRt);
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(660f, 1140f);
            panelRt.anchoredPosition = Vector2.zero;

            Image panelFill = CreateSlicedImage("PanelFill", panelRt, spPanelFill, new Color(0.05f, 0.07f, 0.11f, 0.96f));
            StretchFull(panelFill.rectTransform);
            panelFill.raycastTarget = true;

            Image panelOuter = CreateSlicedImage("PanelOuterBorder", panelRt, spOuterBorder, new Color(0.16f, 0.92f, 0.88f, 1f));
            StretchFull(panelOuter.rectTransform, -8f, -8f, -8f, -8f);
            panelOuter.raycastTarget = false;

            Image panelInner = CreateSlicedImage("PanelInnerBorder", panelRt, spInnerBorder, new Color(1.00f, 0.78f, 0.28f, 0.95f));
            StretchFull(panelInner.rectTransform, 8f, 8f, 8f, 8f);
            panelInner.raycastTarget = false;

            // Body container inside Panel
            RectTransform bodyRt = CreateRect("Body", panelRt);
            StretchFull(bodyRt, 32f, 32f, 30f, 28f);
            VerticalLayoutGroup bodyVlg = bodyRt.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyVlg.spacing = 12f;
            bodyVlg.childAlignment = TextAnchor.UpperCenter;
            bodyVlg.childControlWidth = true;
            bodyVlg.childControlHeight = true;
            bodyVlg.childForceExpandWidth = true;
            bodyVlg.childForceExpandHeight = false;

            // 1. Header Row
            RectTransform headerRt = CreateRect("HeaderRow", bodyRt);
            LayoutElement headerLe = headerRt.gameObject.AddComponent<LayoutElement>();
            headerLe.preferredHeight = 62f;
            headerLe.minHeight = 62f;
            headerLe.flexibleHeight = 0f;

            TextMeshProUGUI titleText = CreateTMP("TitleText", headerRt, font, "POPUP TITLE", 28f, FontStyles.Bold, new Color(1.00f, 0.96f, 0.86f, 1f), TextAlignmentOptions.MidlineLeft);
            titleText.rectTransform.anchorMin = new Vector2(0f, 0f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.offsetMin = new Vector2(2f, 0f);
            titleText.rectTransform.offsetMax = new Vector2(-68f, 0f);

            Button closeBtn = CreateNeonIconButton("BtnClose", headerRt, spLabelFill, spOuterBorder, spClose,
                new Color(0.24f, 0.06f, 0.12f, 0.95f),
                new Color(1.00f, 0.24f, 0.45f, 1f),
                new Color(1.00f, 0.94f, 0.94f, 1f));
            RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.sizeDelta = new Vector2(54f, 54f);
            closeRt.anchoredPosition = Vector2.zero;

            // 2. Currency & Hint Row
            RectTransform currencyRow = CreateRect("CurrencyRow", bodyRt);
            LayoutElement currencyRowLe = currencyRow.gameObject.AddComponent<LayoutElement>();
            currencyRowLe.preferredHeight = 52f;
            currencyRowLe.minHeight = 52f;
            currencyRowLe.flexibleHeight = 0f;

            CurrencyDisplayView currencyView = null;
            GameObject currencyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CurrencyPrefabPath);
            if (currencyPrefab != null)
            {
                GameObject currencyInst = (GameObject)PrefabUtility.InstantiatePrefab(currencyPrefab, currencyRow);
                currencyInst.name = "CurrencyDisplay";
                RectTransform cRt = currencyInst.GetComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0f, 0.5f);
                cRt.anchorMax = new Vector2(0f, 0.5f);
                cRt.pivot = new Vector2(0f, 0.5f);
                cRt.anchoredPosition = Vector2.zero;
                cRt.localScale = Vector3.one * 0.88f;
                currencyView = currencyInst.GetComponent<CurrencyDisplayView>();
                if (currencyView != null && currencyView.AddButton != null)
                {
                    currencyView.AddButton.gameObject.SetActive(false);
                }
            }

            TextMeshProUGUI hintText = CreateTMP("HintText", currencyRow, font, "Mở khóa bằng Tiền hoặc Xem QC", 15f, FontStyles.Italic, new Color(0.16f, 0.92f, 0.88f, 0.92f), TextAlignmentOptions.MidlineRight);
            hintText.rectTransform.anchorMin = new Vector2(0.42f, 0f);
            hintText.rectTransform.anchorMax = new Vector2(1f, 1f);
            hintText.rectTransform.offsetMin = Vector2.zero;
            hintText.rectTransform.offsetMax = new Vector2(-2f, 0f);

            // 3. Content Container (extensible slot for Prefab Variants)
            RectTransform contentContainer = CreateRect("ContentContainer", bodyRt);
            LayoutElement contentLe = contentContainer.gameObject.AddComponent<LayoutElement>();
            contentLe.flexibleHeight = 1f;
            contentLe.minHeight = 400f;
            VerticalLayoutGroup contentVlg = contentContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 12f;
            contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;

            // 4. Toast Banner
            GameObject toastObj = new GameObject("ToastBanner", typeof(RectTransform), typeof(CanvasGroup));
            toastObj.transform.SetParent(panelRt, false);
            RectTransform toastRt = toastObj.GetComponent<RectTransform>();
            toastRt.anchorMin = new Vector2(0.5f, 0f);
            toastRt.anchorMax = new Vector2(0.5f, 0f);
            toastRt.pivot = new Vector2(0.5f, 0f);
            toastRt.sizeDelta = new Vector2(540f, 54f);
            toastRt.anchoredPosition = new Vector2(0f, 24f);

            CanvasGroup toastCg = toastObj.GetComponent<CanvasGroup>();
            toastCg.alpha = 0f;
            toastCg.blocksRaycasts = false;
            toastCg.interactable = false;

            Image toastFill = CreateSlicedImage("ToastFill", toastRt, spPanelFill, new Color(0.06f, 0.14f, 0.18f, 0.96f));
            StretchFull(toastFill.rectTransform);
            toastFill.raycastTarget = false;

            Image toastBorder = CreateSlicedImage("ToastBorder", toastRt, spOuterBorder, new Color(1.00f, 0.78f, 0.28f, 1f));
            StretchFull(toastBorder.rectTransform, -4f, -4f, -4f, -4f);
            toastBorder.raycastTarget = false;

            TextMeshProUGUI toastText = CreateTMP("ToastText", toastRt, font, "Đã mở khóa!", 20f, FontStyles.Bold, new Color(1.00f, 0.96f, 0.86f, 1f), TextAlignmentOptions.Center);
            StretchFull(toastText.rectTransform, 14f, 14f, 4f, 4f);

            NeonPopupBase popupBase = root.AddComponent<NeonPopupBase>();
            popupBase.ConfigureBaseReferences(
                panelRt,
                titleText,
                closeBtn,
                currencyRow.gameObject,
                currencyView,
                hintText,
                contentContainer,
                toastCg,
                toastText);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, BasePopupPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildItemSelectVariantPrefab(GameObject basePopupPrefab)
        {
            EnsureFolder(VariantDir);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite spOuterBorder = LoadSprite($"{NeonDir}/ui_neon_border_outer.png");
            Sprite spLabelFill = LoadSprite($"{NeonDir}/ui_neon_label_fill.png");
            CustomizeItemCardView cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath)?.GetComponent<CustomizeItemCardView>();

            // Instantiate base prefab so SaveAsPrefabAsset creates a genuine Unity Prefab Variant!
            GameObject variantInstance = (GameObject)PrefabUtility.InstantiatePrefab(basePopupPrefab);
            variantInstance.name = "ItemSelectPopup";

            NeonPopupBase popupBase = variantInstance.GetComponent<NeonPopupBase>();
            RectTransform contentContainer = popupBase.ContentContainer;

            // 1. Dynamic Group Filter Bar (hidden automatically when items have no groups)
            RectTransform filterBar = CreateRect("GroupFilterBar", contentContainer);
            LayoutElement filterBarLe = filterBar.gameObject.AddComponent<LayoutElement>();
            filterBarLe.preferredHeight = 48f;
            filterBarLe.minHeight = 48f;
            filterBarLe.flexibleHeight = 0f;

            HorizontalLayoutGroup filterHlg = filterBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            filterHlg.spacing = 10f;
            filterHlg.childControlWidth = true;
            filterHlg.childControlHeight = true;
            filterHlg.childForceExpandWidth = true;
            filterHlg.childForceExpandHeight = true;

            Button groupTabTemplate = CreateNeonTabTemplate("GroupTabTemplate", filterBar, font, spLabelFill, spOuterBorder, "NHÓM", 17f);
            groupTabTemplate.gameObject.SetActive(false);
            filterBar.gameObject.SetActive(false);

            // 2. Scrollable 2-Column Item Grid
            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(contentContainer, false);
            Image scrollBg = scrollObj.GetComponent<Image>();
            scrollBg.sprite = spLabelFill;
            scrollBg.type = Image.Type.Sliced;
            scrollBg.color = new Color(0.03f, 0.04f, 0.07f, 0.55f);

            LayoutElement scrollLe = scrollObj.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 400f;

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
            StretchFull(viewportRt, 4f, 4f, 4f, 4f);

            GameObject contentObj = new GameObject("GridContent", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewportRt, false);
            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 800f);

            GridLayoutGroup grid = contentObj.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(282f, 366f);
            grid.spacing = new Vector2(16f, 18f);
            grid.padding = new RectOffset(4, 4, 12, 24);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 28f;

            ItemSelectPopupController controller = variantInstance.AddComponent<ItemSelectPopupController>();
            controller.ConfigureItemSelectReferences(
                popupBase,
                filterBar.gameObject,
                filterBar,
                groupTabTemplate,
                scrollRect,
                contentRt,
                cardPrefab);

            GameObject savedVariant = PrefabUtility.SaveAsPrefabAsset(variantInstance, VariantPath);
            Object.DestroyImmediate(variantInstance);
            return savedVariant;
        }

        private static void CreateScreenReference(GameObject variantPrefab)
        {
            EnsureFolder(ResourcesScreensDir);
            string refPath = $"{ResourcesScreensDir}/ItemSelectPopup.asset";
            ScreenReference screenRef = AssetDatabase.LoadAssetAtPath<ScreenReference>(refPath);
            if (screenRef == null)
            {
                screenRef = ScriptableObject.CreateInstance<ScreenReference>();
                AssetDatabase.CreateAsset(screenRef, refPath);
            }

            SerializedObject so = new SerializedObject(screenRef);
            SerializedProperty prefabProp = so.FindProperty("_screenPrefab");
            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = variantPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(screenRef);
        }

        private static ItemCatalogSO BuildOutfitCatalog()
        {
            EnsureFolder(CatalogsDir);
            string[] outfitAssetPaths =
            {
                "Assets/Project/Demo/OutfitSystem/Data/OutfitData_Outfit_01_TuCuu.asset",
                "Assets/Project/Demo/OutfitSystem/Data/OutfitData_Outfit_02_HongCon.asset",
                "Assets/Project/Demo/OutfitSystem/Data/OutfitData_Outfit_03_SongHoaHongCon.asset",
                "Assets/Project/Demo/OutfitSystem/Data/OutfitData_Outfit_04_NhiLoNguyenSoai.asset",
                "Assets/Project/Demo/OutfitSystem/Data/OutfitData_Outfit_05_LongDau.asset"
            };

            List<SelectableItemEntry> entries = new List<SelectableItemEntry>();
            foreach (string path in outfitAssetPaths)
            {
                OutfitData data = AssetDatabase.LoadAssetAtPath<OutfitData>(path);
                if (data == null) continue;

                entries.Add(new SelectableItemEntry(
                    id: data.OutfitId,
                    displayName: data.DisplayName,
                    subtitle: $"{data.RankTitle} • +{data.BonusHealth} HP",
                    previewSprite: data.PreviewSprite,
                    unlockMethod: data.UnlockMethod,
                    priceGold: data.PriceGold,
                    adsRequired: data.AdsRequired,
                    groupLabel: string.Empty,
                    payload: data.OutfitId));
            }

            return SaveOrUpdateCatalog(
                $"{CatalogsDir}/ItemCatalog_Outfit.asset",
                defaultLabel: "TRANG PHỤC",
                equipActionLabel: "MẶC",
                equippedStatusLabel: "ĐANG MẶC",
                entries);
        }

        private static ItemCatalogSO BuildHouseCatalog()
        {
            EnsureFolder(CatalogsDir);
            List<SelectableItemEntry> entries = new List<SelectableItemEntry>();

            HomeHouseTheme.Part[] parts = { HomeHouseTheme.Part.Awning, HomeHouseTheme.Part.Door, HomeHouseTheme.Part.Wall };
            HomeHouseTheme.Level[] levels = { HomeHouseTheme.Level.Dilapidated, HomeHouseTheme.Level.Patched, HomeHouseTheme.Level.Renovated };

            foreach (HomeHouseTheme.Part part in parts)
            {
                string groupLabel = HomeHouseTheme.GetPartDisplayName(part);
                foreach (HomeHouseTheme.Level level in levels)
                {
                    HomeHouseTheme.HouseItemSpec spec = HomeHouseTheme.GetItemSpec(part, level);
                    Sprite sprite = LoadSprite($"{HomeHouseDir}/{part}-{level}.png");
                    string id = $"house_{part}_{(int)level}";
                    string payload = $"{part}:{(int)level}";

                    entries.Add(new SelectableItemEntry(
                        id: id,
                        displayName: spec.DisplayName,
                        subtitle: spec.TierLabel,
                        previewSprite: sprite,
                        unlockMethod: spec.UnlockMethod,
                        priceGold: spec.PriceGold,
                        adsRequired: spec.AdsRequired,
                        groupLabel: groupLabel,
                        payload: payload));
                }
            }

            return SaveOrUpdateCatalog(
                $"{CatalogsDir}/ItemCatalog_House.asset",
                defaultLabel: "NHÀ CỬA",
                equipActionLabel: "CHỌN",
                equippedStatusLabel: "ĐANG DÙNG",
                entries);
        }

        private static ItemCatalogSO BuildWeaponCatalog()
        {
            EnsureFolder(CatalogsDir);
            List<SelectableItemEntry> entries = new List<SelectableItemEntry>();

            var weaponSpecs = new (string assetPath, CustomizeUnlockMethod method, int adsRequired, string subtitle)[]
            {
                ("Assets/Project/Scenes/Data/Reward/Weapon/Katana.asset", CustomizeUnlockMethod.Gold, 1, "Sắc bén • Tấn công nhanh"),
                ("Assets/Project/Scenes/Data/Reward/Weapon/MaLoan.asset", CustomizeUnlockMethod.WatchAd, 1, "Uy lực • Sát thương cao")
            };

            foreach (var ws in weaponSpecs)
            {
                CreateItemReward rewardAsset = AssetDatabase.LoadAssetAtPath<CreateItemReward>(ws.assetPath);
                if (rewardAsset == null || rewardAsset.ItemReward == null) continue;

                ItemReward w = rewardAsset.ItemReward;
                entries.Add(new SelectableItemEntry(
                    id: w.ID,
                    displayName: w.Name,
                    subtitle: ws.subtitle,
                    previewSprite: w.Sprite,
                    unlockMethod: ws.method,
                    priceGold: w.Price,
                    adsRequired: ws.adsRequired,
                    groupLabel: string.Empty,
                    payload: w.ID));
            }

            return SaveOrUpdateCatalog(
                $"{CatalogsDir}/ItemCatalog_Weapon.asset",
                defaultLabel: "VŨ KHÍ",
                equipActionLabel: "TRANG BỊ",
                equippedStatusLabel: "ĐANG DÙNG",
                entries);
        }

        private static ItemCatalogSO SaveOrUpdateCatalog(
            string assetPath,
            string defaultLabel,
            string equipActionLabel,
            string equippedStatusLabel,
            List<SelectableItemEntry> entries)
        {
            ItemCatalogSO catalog = AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(assetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemCatalogSO>();
                AssetDatabase.CreateAsset(catalog, assetPath);
            }

            catalog.Configure(defaultLabel, equipActionLabel, equippedStatusLabel, entries);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void WireHomeSceneCatalogs(ItemCatalogSO outfitCatalog, ItemCatalogSO houseCatalog, ItemCatalogSO weaponCatalog)
        {
            Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool openedTemp = false;
            Scene homeScene = activeScene;

            if (activeScene.path != HomeScenePath)
            {
                homeScene = EditorSceneManager.OpenScene(HomeScenePath, OpenSceneMode.Single);
                openedTemp = true;
            }

            HomeController home = Object.FindAnyObjectByType<HomeController>();
            if (home != null)
            {
                SerializedObject so = new SerializedObject(home);
                SerializedProperty outfitProp = so.FindProperty("m_outfitCatalog");
                SerializedProperty houseProp = so.FindProperty("m_houseCatalog");
                SerializedProperty weaponProp = so.FindProperty("m_weaponCatalog");
                if (outfitProp != null) outfitProp.objectReferenceValue = outfitCatalog;
                if (houseProp != null) houseProp.objectReferenceValue = houseCatalog;
                if (weaponProp != null) weaponProp.objectReferenceValue = weaponCatalog;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(home);
                EditorSceneManager.SaveScene(homeScene);
            }

            if (openedTemp && !string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.OpenScene(activeScene.path, OpenSceneMode.Single);
            }
        }

        private static Button CreateNeonTabTemplate(
            string name,
            RectTransform parent,
            TMP_FontAsset font,
            Sprite spFill,
            Sprite spOuter,
            string labelText,
            float fontSize)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(ButtonAudio), typeof(NeonButtonStyle));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();

            Image fill = CreateSlicedImage("Fill", rt, spFill, new Color(0.06f, 0.16f, 0.20f, 0.95f));
            StretchFull(fill.rectTransform);
            fill.raycastTarget = true;

            Image outer = CreateSlicedImage("OuterBorder", rt, spOuter, new Color(0.16f, 0.92f, 0.88f, 1f));
            StretchFull(outer.rectTransform, -4f, -4f, -4f, -4f);
            outer.raycastTarget = false;

            TextMeshProUGUI label = CreateTMP("Label", rt, font, labelText, fontSize, FontStyles.Bold, new Color(1.00f, 0.96f, 0.86f, 1f), TextAlignmentOptions.Center);
            StretchFull(label.rectTransform, 8f, 8f, 2f, 2f);
            label.textWrappingMode = TextWrappingModes.NoWrap;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = fill;

            NeonButtonStyle style = btnObj.GetComponent<NeonButtonStyle>();
            style.ConfigureReferences(fill, outer, null, null, label);
            style.ApplyStyle();

            return btn;
        }

        private static Button CreateNeonIconButton(
            string name,
            RectTransform parent,
            Sprite spFill,
            Sprite spOuter,
            Sprite spIcon,
            Color fillColor,
            Color outerColor,
            Color iconColor)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(ButtonAudio), typeof(NeonButtonStyle));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();

            Image fill = CreateSlicedImage("Fill", rt, spFill, fillColor);
            StretchFull(fill.rectTransform);
            fill.raycastTarget = true;

            Image outer = CreateSlicedImage("OuterBorder", rt, spOuter, outerColor);
            StretchFull(outer.rectTransform, -4f, -4f, -4f, -4f);
            outer.raycastTarget = false;

            Image icon = CreateSimpleImage("Icon", rt, spIcon, iconColor);
            StretchFull(icon.rectTransform, 13f, 13f, 13f, 13f);

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = fill;

            NeonButtonStyle style = btnObj.GetComponent<NeonButtonStyle>();
            style.ConfigureReferences(fill, outer, null, icon);
            style.SetPalette(fillColor, outerColor, Color.clear, iconColor, Color.white);

            return btn;
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image CreateSlicedImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        private static Image CreateSimpleImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI CreateTMP(
            string name,
            RectTransform parent,
            TMP_FontAsset font,
            string text,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            rt.localScale = Vector3.one;
        }

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;
            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
