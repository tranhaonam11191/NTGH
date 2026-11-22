using System.IO;
using NTGH.Demo.OutfitSystem;
using NTGH.UI;
using SS.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

namespace NTGH.EditorTools
{
    public static class HomeCustomizePopupBuilder
    {
        private const string NeonDir = "Assets/Project/Sprites/UI/Neon90s";
        private const string ScreensDir = "Assets/Project/Screens/HomeCustomizePopup";
        private const string ResourcesScreensDir = "Assets/Resources/Screens";
        private const string PreviewsDir = "Assets/Project/Textures/DauBuSkins/Previews";
        private const string FontPath = "Assets/Project/Font/LibreBaskerville-Regular SDF.asset";
        private const string CurrencyPrefabPath = "Assets/Project/Prefabs/UI/CurrencyDisplay.prefab";
        private const string CharacterPrefabPath = "Assets/Project/Demo/OutfitSystem/Prefabs/DemoCharacter_Bone.prefab";

        [MenuItem("NTGH/Build Home Customize Popup")]
        public static void BuildAll()
        {
            ConfigureNeonIconImporters();
            RenderOutfitPreviews();
            ConfigureOutfitDataAssets();

            CustomizeItemCardView cardPrefab = BuildCardPrefab();
            GameObject popupPrefab = BuildPopupPrefab(cardPrefab);
            CreateScreenReference(popupPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HomeCustomizePopupBuilder] Built HomeCustomizePopup, CustomizeItemCard, Outfit previews, and ScreenReference successfully!");
        }

        private static void ConfigureNeonIconImporters()
        {
            string[] icons =
            {
                "icon_neon_house.png",
                "icon_neon_lock.png",
                "icon_neon_ad.png",
                "icon_neon_check.png",
                "icon_neon_close.png"
            };

            foreach (string icon in icons)
            {
                string path = $"{NeonDir}/{icon}";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }
                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void RenderOutfitPreviews()
        {
            GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
            if (charPrefab == null)
            {
                Debug.LogWarning($"[HomeCustomizePopupBuilder] Character prefab not found at {CharacterPrefabPath}");
                return;
            }

            if (!Directory.Exists(PreviewsDir))
                Directory.CreateDirectory(PreviewsDir);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab);
            instance.transform.position = new Vector3(500f, 500f, 0f);
            instance.transform.localScale = Vector3.one;

            CharacterOutfitController controller = instance.GetComponent<CharacterOutfitController>();
            if (controller == null || controller.Outfits == null || controller.Outfits.Count == 0)
            {
                Object.DestroyImmediate(instance);
                Debug.LogWarning("[HomeCustomizePopupBuilder] CharacterOutfitController has no outfits.");
                return;
            }

            // Put character on temporary high layer or render cleanly with dedicated camera
            const int previewLayer = 31;
            SetLayerRecursively(instance, previewLayer);

            GameObject camObj = new GameObject("__OutfitPreviewCam");
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.cullingMask = 1 << previewLayer;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;

            string[] fileNames =
            {
                "OutfitPreview_Lv1_TuCuu.png",
                "OutfitPreview_Lv2_HongCon.png",
                "OutfitPreview_Lv3_SongHoaHongCon.png",
                "OutfitPreview_Lv4_NhiLoNguyenSoai.png",
                "OutfitPreview_Lv5_LongDau.png"
            };

            const int rtSize = 768;
            RenderTexture rt = new RenderTexture(rtSize, rtSize, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            try
            {
                int count = Mathf.Min(controller.Outfits.Count, fileNames.Length);
                for (int i = 0; i < count; i++)
                {
                    controller.EquipOutfitByIndex(i);
                    SpriteResolver[] resolvers = instance.GetComponentsInChildren<SpriteResolver>(true);
                    foreach (SpriteResolver r in resolvers)
                    {
                        r.ResolveSpriteToSpriteRenderer();
                    }

                    // Calculate world bounds of all active SpriteRenderers
                    Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);
                    bool hasBounds = false;
                    foreach (SpriteRenderer sr in instance.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (!sr.enabled || sr.sprite == null) continue;
                        if (!hasBounds)
                        {
                            bounds = sr.bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(sr.bounds);
                        }
                    }

                    if (!hasBounds)
                        bounds = new Bounds(instance.transform.position + Vector3.up * 2f, new Vector3(3f, 5f, 1f));

                    cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
                    cam.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x) * 1.12f;

                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    cam.Render();

                    Texture2D raw = new Texture2D(rtSize, rtSize, TextureFormat.RGBA32, false);
                    raw.ReadPixels(new Rect(0, 0, rtSize, rtSize), 0, 0);
                    raw.Apply();
                    RenderTexture.active = prev;

                    Texture2D cropped = CropTransparent(raw, 12);
                    Object.DestroyImmediate(raw);

                    string outPath = $"{PreviewsDir}/{fileNames[i]}";
                    File.WriteAllBytes(outPath, cropped.EncodeToPNG());
                    Object.DestroyImmediate(cropped);
                }
            }
            finally
            {
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camObj);
                Object.DestroyImmediate(instance);
            }

            AssetDatabase.Refresh();

            foreach (string file in fileNames)
            {
                string path = $"{PreviewsDir}/{file}";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static Texture2D CropTransparent(Texture2D src, int pad)
        {
            int w = src.width;
            int h = src.height;
            Color32[] pixels = src.GetPixels32();

            int minX = w, minY = h, maxX = 0, maxY = 0;
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (pixels[row + x].a > 10)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (maxX <= minX || maxY <= minY)
            {
                minX = 0; minY = 0; maxX = w - 1; maxY = h - 1;
            }
            else
            {
                minX = Mathf.Max(0, minX - pad);
                minY = Mathf.Max(0, minY - pad);
                maxX = Mathf.Min(w - 1, maxX + pad);
                maxY = Mathf.Min(h - 1, maxY + pad);
            }

            int cw = maxX - minX + 1;
            int ch = maxY - minY + 1;
            Texture2D dst = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
            dst.SetPixels(src.GetPixels(minX, minY, cw, ch));
            dst.Apply();
            return dst;
        }

        private static void ConfigureOutfitDataAssets()
        {
            var specs = new (string assetPath, string previewFile, CustomizeUnlockMethod method, int priceGold, int adsRequired)[]
            {
                ("Assets/Project/Demo/OutfitSystem/Data/OutfitData_Outfit_01_TuCuu.asset", "OutfitPreview_Lv1_TuCuu.png", CustomizeUnlockMethod.Free, 0, 1),
                ("Assets/Project/Demo/OutfitSystem/Data/OutfitData_Outfit_02_HongCon.asset", "OutfitPreview_Lv2_HongCon.png", CustomizeUnlockMethod.Gold, 500, 1),
                ("Assets/Project/Demo/OutfitSystem/Data/OutfitData_Outfit_03_SongHoaHongCon.asset", "OutfitPreview_Lv3_SongHoaHongCon.png", CustomizeUnlockMethod.WatchAd, 1500, 1),
                ("Assets/Project/Demo/OutfitSystem/Data/OutfitData_Outfit_04_NhiLoNguyenSoai.asset", "OutfitPreview_Lv4_NhiLoNguyenSoai.png", CustomizeUnlockMethod.Gold, 3500, 1),
                ("Assets/Project/Demo/OutfitSystem/Data/OutfitData_Outfit_05_LongDau.asset", "OutfitPreview_Lv5_LongDau.png", CustomizeUnlockMethod.WatchAd, 8000, 2)
            };

            foreach (var s in specs)
            {
                OutfitData data = AssetDatabase.LoadAssetAtPath<OutfitData>(s.assetPath);
                if (data == null) continue;
                Sprite preview = AssetDatabase.LoadAssetAtPath<Sprite>($"{PreviewsDir}/{s.previewFile}");
                data.ConfigureUnlock(s.method, s.priceGold, s.adsRequired, preview);
                EditorUtility.SetDirty(data);
            }
        }

        private static CustomizeItemCardView BuildCardPrefab()
        {
            EnsureFolder(ScreensDir);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite spPanelFill = LoadSprite($"{NeonDir}/ui_neon_panel_fill.png");
            Sprite spOuterBorder = LoadSprite($"{NeonDir}/ui_neon_border_outer.png");
            Sprite spInnerBorder = LoadSprite($"{NeonDir}/ui_neon_border_inner.png");
            Sprite spLabelFill = LoadSprite($"{NeonDir}/ui_neon_label_fill.png");
            Sprite spDollar = LoadSprite($"{NeonDir}/icon_neon_dollar.png");
            Sprite spAd = LoadSprite($"{NeonDir}/icon_neon_ad.png");
            Sprite spCheck = LoadSprite($"{NeonDir}/icon_neon_check.png");
            Sprite spLock = LoadSprite($"{NeonDir}/icon_neon_lock.png");

            GameObject root = new GameObject("CustomizeItemCard", typeof(RectTransform));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(282f, 366f);

            // 3-layer Neon 90s Card Frame
            Image cardFill = CreateSlicedImage("CardFill", rootRt, spPanelFill, new Color(0.06f, 0.08f, 0.12f, 0.94f));
            StretchFull(cardFill.rectTransform);
            cardFill.raycastTarget = true;

            Image cardOuter = CreateSlicedImage("OuterBorder", rootRt, spOuterBorder, new Color(0.16f, 0.92f, 0.88f, 1f));
            StretchFull(cardOuter.rectTransform, -5f, -5f, -5f, -5f);
            cardOuter.raycastTarget = false;

            Image cardInner = CreateSlicedImage("InnerBorder", rootRt, spInnerBorder, new Color(1.00f, 0.78f, 0.28f, 0.75f));
            StretchFull(cardInner.rectTransform, 6f, 6f, 6f, 6f);
            cardInner.raycastTarget = false;

            // Top Row: Status Tag + Corner Status Badge (cleanly inside cardInner)
            RectTransform topRow = CreateRect("TopRow", rootRt);
            topRow.anchorMin = new Vector2(0f, 1f);
            topRow.anchorMax = new Vector2(1f, 1f);
            topRow.pivot = new Vector2(0.5f, 1f);
            topRow.offsetMin = new Vector2(18f, -48f);
            topRow.offsetMax = new Vector2(-18f, -18f);

            Image tagBg = CreateSlicedImage("StatusTagBg", topRow, spLabelFill, new Color(0.07f, 0.32f, 0.35f, 0.95f));
            tagBg.rectTransform.anchorMin = new Vector2(0f, 0f);
            tagBg.rectTransform.anchorMax = new Vector2(1f, 1f);
            tagBg.rectTransform.offsetMin = new Vector2(0f, 1f);
            tagBg.rectTransform.offsetMax = new Vector2(-38f, -1f);
            tagBg.raycastTarget = false;

            TextMeshProUGUI tagText = CreateTMP("StatusTagText", tagBg.rectTransform, font, "ĐÃ MỞ", 13.5f, FontStyles.Bold, new Color(0.16f, 0.94f, 0.58f, 1f), TextAlignmentOptions.Center);
            StretchFull(tagText.rectTransform, 6f, 6f, 0f, 0f);

            Image badgeBg = CreateSlicedImage("StatusBadgeBg", topRow, spLabelFill, new Color(0.16f, 0.92f, 0.88f, 1f));
            badgeBg.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            badgeBg.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            badgeBg.rectTransform.pivot = new Vector2(1f, 0.5f);
            badgeBg.rectTransform.sizeDelta = new Vector2(30f, 30f);
            badgeBg.rectTransform.anchoredPosition = Vector2.zero;
            badgeBg.raycastTarget = false;

            Image badgeIcon = CreateSimpleImage("StatusBadgeIcon", badgeBg.rectTransform, spCheck, new Color(0.04f, 0.08f, 0.10f, 1f));
            StretchFull(badgeIcon.rectTransform, 5f, 5f, 5f, 5f);

            // Preview Box
            Image previewBg = CreateSlicedImage("PreviewBox", rootRt, spLabelFill, new Color(0.05f, 0.12f, 0.16f, 0.85f));
            previewBg.rectTransform.anchorMin = new Vector2(0f, 1f);
            previewBg.rectTransform.anchorMax = new Vector2(1f, 1f);
            previewBg.rectTransform.pivot = new Vector2(0.5f, 1f);
            previewBg.rectTransform.offsetMin = new Vector2(18f, -220f);
            previewBg.rectTransform.offsetMax = new Vector2(-18f, -54f);
            previewBg.raycastTarget = false;

            Image previewImage = CreateSimpleImage("PreviewImage", previewBg.rectTransform, null, Color.white);
            StretchFull(previewImage.rectTransform, 8f, 8f, 8f, 8f);
            previewImage.preserveAspect = true;

            // Locked Overlay inside Preview Box
            GameObject lockedOverlayObj = new GameObject("LockedOverlay", typeof(RectTransform), typeof(Image));
            lockedOverlayObj.transform.SetParent(previewBg.rectTransform, false);
            Image lockedOverlayImg = lockedOverlayObj.GetComponent<Image>();
            lockedOverlayImg.sprite = spLabelFill;
            lockedOverlayImg.type = Image.Type.Sliced;
            lockedOverlayImg.color = new Color(0.03f, 0.04f, 0.07f, 0.55f);
            lockedOverlayImg.raycastTarget = false;
            StretchFull(lockedOverlayImg.rectTransform);

            Image lockCenterIcon = CreateSimpleImage("LockCenterIcon", lockedOverlayImg.rectTransform, spLock, new Color(1.00f, 0.78f, 0.28f, 1f));
            lockCenterIcon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            lockCenterIcon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            lockCenterIcon.rectTransform.sizeDelta = new Vector2(64f, 64f);
            lockCenterIcon.rectTransform.anchoredPosition = Vector2.zero;

            // Title & Subtitle
            TextMeshProUGUI titleText = CreateTMP("TitleText", rootRt, font, "Đồ Bán Báo", 19.5f, FontStyles.Bold, new Color(1.00f, 0.96f, 0.86f, 1f), TextAlignmentOptions.Center);
            titleText.rectTransform.anchorMin = new Vector2(0f, 0f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 0f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 0f);
            titleText.rectTransform.offsetMin = new Vector2(16f, 108f);
            titleText.rectTransform.offsetMax = new Vector2(-16f, 138f);
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;

            TextMeshProUGUI subtitleText = CreateTMP("SubtitleText", rootRt, font, "Tứ Cửu • +50 HP", 14f, FontStyles.Normal, new Color(1.00f, 0.78f, 0.28f, 1f), TextAlignmentOptions.Center);
            subtitleText.rectTransform.anchorMin = new Vector2(0f, 0f);
            subtitleText.rectTransform.anchorMax = new Vector2(1f, 0f);
            subtitleText.rectTransform.pivot = new Vector2(0.5f, 0f);
            subtitleText.rectTransform.offsetMin = new Vector2(14f, 80f);
            subtitleText.rectTransform.offsetMax = new Vector2(-14f, 106f);
            subtitleText.overflowMode = TextOverflowModes.Ellipsis;
            subtitleText.textWrappingMode = TextWrappingModes.NoWrap;

            // Bottom Action CTA Button (cleanly inside cardInner, no cluttering inner border)
            GameObject btnObj = new GameObject("ActionButton", typeof(RectTransform), typeof(Button), typeof(ButtonAudio));
            btnObj.transform.SetParent(rootRt, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 0f);
            btnRt.anchorMax = new Vector2(1f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.offsetMin = new Vector2(20f, 18f);
            btnRt.offsetMax = new Vector2(-20f, 70f);

            Image actionFill = CreateSlicedImage("Fill", btnRt, spLabelFill, new Color(0.07f, 0.26f, 0.19f, 0.96f));
            StretchFull(actionFill.rectTransform);
            actionFill.raycastTarget = true;

            Image actionOuter = CreateSlicedImage("OuterBorder", btnRt, spOuterBorder, new Color(0.16f, 0.94f, 0.58f, 1f));
            StretchFull(actionOuter.rectTransform, -4f, -4f, -4f, -4f);
            actionOuter.raycastTarget = false;

            Image actionInner = null;

            Button actionBtn = btnObj.GetComponent<Button>();
            actionBtn.targetGraphic = actionFill;

            // Horizontal content inside Action Button (Icon + Label)
            RectTransform btnContent = CreateRect("Content", btnRt);
            StretchFull(btnContent, 10f, 10f, 4f, 4f);
            HorizontalLayoutGroup hlg = btnContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 8f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Image actionIcon = CreateSimpleImage("Icon", btnContent, spDollar, Color.white);
            actionIcon.rectTransform.sizeDelta = new Vector2(28f, 28f);
            LayoutElement iconLe = actionIcon.gameObject.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 28f;
            iconLe.preferredHeight = 28f;

            TextMeshProUGUI actionLabel = CreateTMP("Label", btnContent, font, "500", 20f, FontStyles.Bold, new Color(1.00f, 0.96f, 0.86f, 1f), TextAlignmentOptions.Center);
            actionLabel.enableAutoSizing = true;
            actionLabel.fontSizeMin = 14f;
            actionLabel.fontSizeMax = 21f;
            actionLabel.textWrappingMode = TextWrappingModes.NoWrap;
            ContentSizeFitter labelCsf = actionLabel.gameObject.AddComponent<ContentSizeFitter>();
            labelCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            labelCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CustomizeItemCardView cardView = root.AddComponent<CustomizeItemCardView>();
            cardView.ConfigureReferences(
                cardFill, cardOuter, cardInner,
                previewBg, previewImage, lockedOverlayObj, lockCenterIcon,
                tagBg, tagText, badgeBg, badgeIcon,
                titleText, subtitleText,
                actionBtn, actionFill, actionOuter, actionInner, actionIcon, actionLabel,
                spDollar, spAd, spCheck, spLock);

            string prefabPath = $"{ScreensDir}/CustomizeItemCard.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            return savedPrefab.GetComponent<CustomizeItemCardView>();
        }

        private static GameObject BuildPopupPrefab(CustomizeItemCardView cardPrefab)
        {
            EnsureFolder(ScreensDir);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite spPanelFill = LoadSprite($"{NeonDir}/ui_neon_panel_fill.png");
            Sprite spOuterBorder = LoadSprite($"{NeonDir}/ui_neon_border_outer.png");
            Sprite spInnerBorder = LoadSprite($"{NeonDir}/ui_neon_border_inner.png");
            Sprite spLabelFill = LoadSprite($"{NeonDir}/ui_neon_label_fill.png");
            Sprite spOutfit = LoadSprite($"{NeonDir}/icon_neon_outfit.png");
            Sprite spHouse = LoadSprite($"{NeonDir}/icon_neon_house.png");
            Sprite spClose = LoadSprite($"{NeonDir}/icon_neon_close.png");

            // Root Screen Object (required by SS.UI)
            GameObject root = new GameObject("HomeCustomizePopup", typeof(RectTransform), typeof(ScreenController));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            // Centered Neon 90s Modal Panel (660 x 1140 on 720x1600 reference)
            RectTransform panelRt = CreateRect("Panel", rootRt);
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(660f, 1140f);
            panelRt.anchoredPosition = Vector2.zero;

            // Panel 3-layer Neon 90s Frame
            Image panelFill = CreateSlicedImage("PanelFill", panelRt, spPanelFill, new Color(0.05f, 0.07f, 0.11f, 0.96f));
            StretchFull(panelFill.rectTransform);
            panelFill.raycastTarget = true;

            Image panelOuter = CreateSlicedImage("PanelOuterBorder", panelRt, spOuterBorder, new Color(0.16f, 0.92f, 0.88f, 1f));
            StretchFull(panelOuter.rectTransform, -8f, -8f, -8f, -8f);
            panelOuter.raycastTarget = false;

            Image panelInner = CreateSlicedImage("PanelInnerBorder", panelRt, spInnerBorder, new Color(1.00f, 0.78f, 0.28f, 0.95f));
            StretchFull(panelInner.rectTransform, 8f, 8f, 8f, 8f);
            panelInner.raycastTarget = false;

            // Main Vertical Layout Container inside Panel (padded cleanly inside PanelInnerBorder)
            RectTransform bodyRt = CreateRect("Body", panelRt);
            StretchFull(bodyRt, 32f, 32f, 30f, 28f);
            VerticalLayoutGroup bodyVlg = bodyRt.gameObject.AddComponent<VerticalLayoutGroup>();
            bodyVlg.spacing = 12f;
            bodyVlg.childAlignment = TextAnchor.UpperCenter;
            bodyVlg.childControlWidth = true;
            bodyVlg.childControlHeight = true;
            bodyVlg.childForceExpandWidth = true;
            bodyVlg.childForceExpandHeight = false;

            // 1. HEADER ROW (Title + Close X Button)
            RectTransform headerRt = CreateRect("HeaderRow", bodyRt);
            LayoutElement headerLe = headerRt.gameObject.AddComponent<LayoutElement>();
            headerLe.preferredHeight = 62f;
            headerLe.minHeight = 62f;
            headerLe.flexibleHeight = 0f;

            TextMeshProUGUI titleText = CreateTMP("TitleText", headerRt, font, "CHỌN TRANG PHỤC", 28f, FontStyles.Bold, new Color(1.00f, 0.96f, 0.86f, 1f), TextAlignmentOptions.MidlineLeft);
            titleText.rectTransform.anchorMin = new Vector2(0f, 0f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.offsetMin = new Vector2(2f, 0f);
            titleText.rectTransform.offsetMax = new Vector2(-68f, 0f);

            Button closeBtn = CreateNeonIconButton("BtnClose", headerRt, spLabelFill, spOuterBorder, null, spClose,
                new Color(0.24f, 0.06f, 0.12f, 0.95f),
                new Color(1.00f, 0.24f, 0.45f, 1f),
                Color.clear,
                new Color(1.00f, 0.94f, 0.94f, 1f));
            RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.sizeDelta = new Vector2(54f, 54f);
            closeRt.anchoredPosition = Vector2.zero;

            // 2. CURRENCY & LEGEND ROW
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

            TextMeshProUGUI hintText = CreateTMP("LegendHint", currencyRow, font, "Mở khóa bằng Tiền hoặc Xem QC", 15f, FontStyles.Italic, new Color(0.16f, 0.92f, 0.88f, 0.92f), TextAlignmentOptions.MidlineRight);
            hintText.rectTransform.anchorMin = new Vector2(0.42f, 0f);
            hintText.rectTransform.anchorMax = new Vector2(1f, 1f);
            hintText.rectTransform.offsetMin = Vector2.zero;
            hintText.rectTransform.offsetMax = new Vector2(-2f, 0f);

            // 3. MAIN TAB BAR (TRANG PHỤC | NHÀ CỬA)
            RectTransform tabBar = CreateRect("MainTabBar", bodyRt);
            LayoutElement tabBarLe = tabBar.gameObject.AddComponent<LayoutElement>();
            tabBarLe.preferredHeight = 68f;
            tabBarLe.minHeight = 68f;
            tabBarLe.flexibleHeight = 0f;

            HorizontalLayoutGroup tabHlg = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabHlg.spacing = 14f;
            tabHlg.childControlWidth = true;
            tabHlg.childControlHeight = true;
            tabHlg.childForceExpandWidth = true;
            tabHlg.childForceExpandHeight = true;

            (Button tabOutfitBtn, NeonButtonStyle tabOutfitStyle) = CreateNeonTabButton("TabOutfit", tabBar, font, spPanelFill, spOuterBorder, spInnerBorder, spOutfit, "TRANG PHỤC", 21f);
            (Button tabHouseBtn, NeonButtonStyle tabHouseStyle) = CreateNeonTabButton("TabHouse", tabBar, font, spPanelFill, spOuterBorder, spInnerBorder, spHouse, "NHÀ CỬA", 21f);

            // 4. HOUSE SUB-TAB BAR (MÁI HIÊN | CỬA CUỐN | TƯỜNG NHÀ)
            RectTransform subTabBar = CreateRect("HouseSubTabBar", bodyRt);
            LayoutElement subTabBarLe = subTabBar.gameObject.AddComponent<LayoutElement>();
            subTabBarLe.preferredHeight = 48f;
            subTabBarLe.minHeight = 48f;
            subTabBarLe.flexibleHeight = 0f;

            HorizontalLayoutGroup subTabHlg = subTabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            subTabHlg.spacing = 10f;
            subTabHlg.childControlWidth = true;
            subTabHlg.childControlHeight = true;
            subTabHlg.childForceExpandWidth = true;
            subTabHlg.childForceExpandHeight = true;

            (Button subAwningBtn, NeonButtonStyle subAwningStyle) = CreateNeonTabButton("SubTabAwning", subTabBar, font, spLabelFill, spOuterBorder, null, null, "MÁI HIÊN", 17f);
            (Button subDoorBtn, NeonButtonStyle subDoorStyle) = CreateNeonTabButton("SubTabDoor", subTabBar, font, spLabelFill, spOuterBorder, null, null, "CỬA CUỐN", 17f);
            (Button subWallBtn, NeonButtonStyle subWallStyle) = CreateNeonTabButton("SubTabWall", subTabBar, font, spLabelFill, spOuterBorder, null, null, "TƯỜNG NHÀ", 17f);
            subTabBar.gameObject.SetActive(false);

            // 5. SCROLLABLE GRID AREA
            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(bodyRt, false);
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

            // 6. TOAST BANNER (Overlay at bottom of Panel)
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

            // Attach & Wire Controller
            HomeCustomizePopupController controller = root.AddComponent<HomeCustomizePopupController>();
            controller.ConfigureReferences(
                titleText,
                currencyView,
                closeBtn,
                tabOutfitBtn,
                tabOutfitStyle,
                tabHouseBtn,
                tabHouseStyle,
                subTabBar.gameObject,
                subAwningBtn,
                subAwningStyle,
                subDoorBtn,
                subDoorStyle,
                subWallBtn,
                subWallStyle,
                scrollRect,
                contentRt,
                cardPrefab,
                toastCg,
                toastText);

            string prefabPath = $"{ScreensDir}/HomeCustomizePopup.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            return savedPrefab;
        }

        private static void CreateScreenReference(GameObject popupPrefab)
        {
            EnsureFolder(ResourcesScreensDir);
            string refPath = $"{ResourcesScreensDir}/HomeCustomizePopup.asset";
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
                prefabProp.objectReferenceValue = popupPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(screenRef);
        }

        private static (Button, NeonButtonStyle) CreateNeonTabButton(
            string name,
            RectTransform parent,
            TMP_FontAsset font,
            Sprite spFill,
            Sprite spOuter,
            Sprite spInner,
            Sprite iconSprite,
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

            Image inner = null;
            if (spInner != null)
            {
                inner = CreateSlicedImage("InnerBorder", rt, spInner, new Color(1.00f, 0.78f, 0.30f, 1f));
                StretchFull(inner.rectTransform, 4f, 4f, 4f, 4f);
                inner.raycastTarget = false;
            }

            RectTransform content = CreateRect("Content", rt);
            StretchFull(content, 10f, 10f, 4f, 4f);
            HorizontalLayoutGroup hlg = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Image icon = null;
            if (iconSprite != null)
            {
                icon = CreateSimpleImage("Icon", content, iconSprite, new Color(1.00f, 0.88f, 0.45f, 1f));
                icon.rectTransform.sizeDelta = new Vector2(34f, 34f);
                LayoutElement le = icon.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 34f;
                le.preferredHeight = 34f;
            }

            TextMeshProUGUI label = CreateTMP("Label", content, font, labelText, fontSize, FontStyles.Bold, new Color(1.00f, 0.96f, 0.86f, 1f), TextAlignmentOptions.Center);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            ContentSizeFitter csf = label.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = fill;

            NeonButtonStyle style = btnObj.GetComponent<NeonButtonStyle>();
            style.ConfigureReferences(fill, outer, inner, icon, label);
            style.ApplyStyle();

            return (btn, style);
        }

        private static Button CreateNeonIconButton(
            string name,
            RectTransform parent,
            Sprite spFill,
            Sprite spOuter,
            Sprite spInner,
            Sprite spIcon,
            Color fillColor,
            Color outerColor,
            Color innerColor,
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

            Image inner = null;
            if (spInner != null)
            {
                inner = CreateSlicedImage("InnerBorder", rt, spInner, innerColor);
                StretchFull(inner.rectTransform, 4f, 4f, 4f, 4f);
                inner.raycastTarget = false;
            }

            Image icon = CreateSimpleImage("Icon", rt, spIcon, iconColor);
            StretchFull(icon.rectTransform, 13f, 13f, 13f, 13f);

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = fill;

            NeonButtonStyle style = btnObj.GetComponent<NeonButtonStyle>();
            style.ConfigureReferences(fill, outer, inner, icon);
            style.SetPalette(fillColor, outerColor, innerColor, iconColor, Color.white);

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

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
