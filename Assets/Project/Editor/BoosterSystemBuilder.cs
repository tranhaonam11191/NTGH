using System.Collections.Generic;
using System.IO;
using NTGH.Boosters;
using NTGH.UI;
using SS.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.Editor
{
    /// <summary>
    /// Creates and wires:
    /// 1) Assets/Resources/Data/BoosterDatabase.asset (4 Match-3 boosters with unlock levels & Pack 1/3 prices)
    /// 2) Assets/Project/Screens/BuyBoosterPopup/BuyBoosterPopup.prefab + Assets/Resources/Screens/BuyBoosterPopup.asset
    /// 3) Footer BoosterSlotView (3 states: Locked, Available green circle, Empty red circle) & GameBoosterHUD in Game.unity
    /// </summary>
    public static class BoosterSystemBuilder
    {
        private const string FontPath = "Assets/Project/Font/NotoSans/NotoSans-Bold SDF.asset";
        private const string PanelFillPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_panel_fill.png";
        private const string BorderOuterPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_outer.png";
        private const string BorderInnerPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_inner.png";
        private const string CircleFillPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_circle_fill.png";
        private const string CircleBorderPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_circle_border.png";
        private const string CloseIconPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_close.png";
        private const string LockIconPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_lock.png";
        private const string DollarIconPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_dollar.png";
        private const string GoldCoinPath = "Assets/Project/Textures/icon_gold_coin.png";

        private const string IconHammerPath = "Assets/Project/Textures/skill_knuckles.png";
        private const string IconFreeSwapPath = "Assets/Project/Textures/skill_syringe.png";
        private const string IconLineBlastPath = "Assets/Project/Textures/skill_grenade.png";
        private const string IconShufflePath = "Assets/Project/Textures/skill_molotov.png";

        private const string BoosterDbAssetPath = "Assets/Resources/Data/BoosterDatabase.asset";
        private const string BuyBoosterFolder = "Assets/Project/Screens/BuyBoosterPopup";
        private const string BuyBoosterPrefabPath = BuyBoosterFolder + "/BuyBoosterPopup.prefab";
        private const string BuyBoosterRefPath = "Assets/Resources/Screens/BuyBoosterPopup.asset";
        private const string GameScenePath = "Assets/Project/Scenes/Game.unity";
        private const string IntroScenePath = "Assets/Project/Scenes/Intro.unity";

        // Neon 90s palette
        private static readonly Color PanelFill = new Color(0.07f, 0.09f, 0.14f, 0.97f);
        private static readonly Color CyanOuter = new Color(0.16f, 0.92f, 0.88f, 1f);
        private static readonly Color GoldOuter = new Color(1.00f, 0.78f, 0.30f, 1f);
        private static readonly Color InnerBoxFill = new Color(0.04f, 0.06f, 0.10f, 0.88f);
        private static readonly Color GreenBadgeColor = new Color32(34, 197, 94, 255);

        [MenuItem("NTGH/Boosters/Build Booster System (SO + Popup + Game HUD)")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/Resources/Data");
            EnsureFolder("Assets/Resources/Screens");
            EnsureFolder(BuyBoosterFolder);

            BoosterDatabaseSO db = BuildOrUpdateDatabase();
            BuildBuyBoosterPopup();
            WireGameScene(db);
            WireSOManagerInScene(IntroScenePath, db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BoosterSystemBuilder] Successfully built BoosterDatabaseSO, BuyBoosterPopup, and wired Game.unity.");
        }

        private static BoosterDatabaseSO BuildOrUpdateDatabase()
        {
            BoosterDatabaseSO db = AssetDatabase.LoadAssetAtPath<BoosterDatabaseSO>(BoosterDbAssetPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<BoosterDatabaseSO>();
                AssetDatabase.CreateAsset(db, BoosterDbAssetPath);
            }

            var configs = new List<BoosterConfig>
            {
                new BoosterConfig
                {
                    type = BoosterType.Hammer,
                    id = "booster_hammer",
                    displayName = "Búa Phá Ô",
                    description = "Phá hủy ngay lập tức 1 ô bất kỳ trên bàn cờ và gây sát thương.",
                    targetingHint = "Chọn 1 ô bất kỳ để phá hủy",
                    icon = LoadSprite(IconHammerPath),
                    unlockLevel = 1,
                    initialFreeCount = 2,
                    pricePack1 = 150,
                    pricePack3 = 400
                },
                new BoosterConfig
                {
                    type = BoosterType.FreeSwap,
                    id = "booster_free_swap",
                    displayName = "Đổi Chỗ Tự Do",
                    description = "Hoán đổi 2 ô kề nhau bất kỳ mà không cần tạo match-3.",
                    targetingHint = "Chọn hoặc kéo 2 ô kề nhau để đổi chỗ",
                    icon = LoadSprite(IconFreeSwapPath),
                    unlockLevel = 2,
                    initialFreeCount = 2,
                    pricePack1 = 200,
                    pricePack3 = 520
                },
                new BoosterConfig
                {
                    type = BoosterType.LineBlast,
                    id = "booster_line_blast",
                    displayName = "Phá Hàng & Cột",
                    description = "Quét sạch toàn bộ hàng ngang và cột dọc đi qua ô được chọn.",
                    targetingHint = "Chọn 1 ô để phá sạch cả hàng và cột",
                    icon = LoadSprite(IconLineBlastPath),
                    unlockLevel = 3,
                    initialFreeCount = 2,
                    pricePack1 = 300,
                    pricePack3 = 780
                },
                new BoosterConfig
                {
                    type = BoosterType.Shuffle,
                    id = "booster_shuffle",
                    displayName = "Xáo Trộn Bàn",
                    description = "Xáo trộn toàn bộ các ô trên bàn cờ để mở ra nước đi mới.",
                    targetingHint = string.Empty,
                    icon = LoadSprite(IconShufflePath),
                    unlockLevel = 4,
                    initialFreeCount = 2,
                    pricePack1 = 150,
                    pricePack3 = 400
                }
            };

            db.SetBoosters(configs);
            EditorUtility.SetDirty(db);
            return db;
        }

        private static void BuildBuyBoosterPopup()
        {
            var root = new GameObject("BuyBoosterPopup", typeof(RectTransform), typeof(ScreenController), typeof(BuyBoosterPopupController));
            var rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            var panel = CreateNeonPanel(rootRt, "PanelRoot", new Vector2(620f, 680f), PanelFill, CyanOuter, new Color(1f, 0.78f, 0.30f, 0.35f), out var borderOuter, out var borderInner);

            var btnClose = CreateCloseButton(panel, CyanOuter, new Color(0.08f, 0.12f, 0.18f, 1f));

            var titleRt = CreateUI("TxtTitle", panel);
            SetTop(titleRt, 28f, 52f, 24f);
            var txtTitle = MakeTMP(titleRt, "MUA BOOSTER", 38f, FontStyles.Bold, TextAlignmentOptions.Center, CyanOuter);

            // Center Info Card
            var infoBox = CreateUI("BoosterInfoBox", panel);
            infoBox.anchorMin = new Vector2(0f, 0.5f);
            infoBox.anchorMax = new Vector2(1f, 0.5f);
            infoBox.pivot = new Vector2(0.5f, 0.5f);
            infoBox.anchoredPosition = new Vector2(0f, 34f);
            infoBox.sizeDelta = new Vector2(-56f, 330f);
            AssignSliced(infoBox.gameObject.AddComponent<Image>(), PanelFillPath, InnerBoxFill);

            var infoBorder = CreateUI("Border", infoBox);
            StretchFull(infoBorder);
            var ibImg = infoBorder.gameObject.AddComponent<Image>();
            AssignSliced(ibImg, BorderInnerPath, new Color(0.16f, 0.92f, 0.88f, 0.42f));
            ibImg.raycastTarget = false;

            // Icon Frame
            var iconBox = CreateUI("BoosterIconBox", infoBox);
            iconBox.anchorMin = new Vector2(0.5f, 1f);
            iconBox.anchorMax = new Vector2(0.5f, 1f);
            iconBox.pivot = new Vector2(0.5f, 1f);
            iconBox.anchoredPosition = new Vector2(0f, -24f);
            iconBox.sizeDelta = new Vector2(124f, 124f);
            AssignSliced(iconBox.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.10f, 0.16f, 0.24f, 0.95f));

            var iconBoxBorder = CreateUI("Border", iconBox);
            StretchFull(iconBoxBorder);
            var ibbImg = iconBoxBorder.gameObject.AddComponent<Image>();
            AssignSliced(ibbImg, BorderOuterPath, GoldOuter);
            ibbImg.raycastTarget = false;

            var iconRt = CreateUI("ImgBoosterIcon", iconBox);
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(92f, 92f);
            var imgBoosterIcon = iconRt.gameObject.AddComponent<Image>();
            imgBoosterIcon.sprite = LoadSprite(IconHammerPath);
            imgBoosterIcon.preserveAspect = true;
            imgBoosterIcon.raycastTarget = false;

            // Name
            var nameRt = CreateUI("TxtBoosterName", infoBox);
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -158f);
            nameRt.sizeDelta = new Vector2(-32f, 44f);
            var txtName = MakeTMP(nameRt, "Búa Phá Ô", 30f, FontStyles.Bold, TextAlignmentOptions.Center, GoldOuter);

            // Description
            var descRt = CreateUI("TxtBoosterDesc", infoBox);
            descRt.anchorMin = new Vector2(0f, 1f);
            descRt.anchorMax = new Vector2(1f, 1f);
            descRt.pivot = new Vector2(0.5f, 1f);
            descRt.anchoredPosition = new Vector2(0f, -206f);
            descRt.sizeDelta = new Vector2(-40f, 68f);
            var txtDesc = MakeTMP(descRt, "Phá hủy ngay lập tức 1 ô bất kỳ trên bàn cờ.", 21f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.86f, 0.92f, 0.98f, 0.95f));
            txtDesc.textWrappingMode = TextWrappingModes.Normal;

            // Owned count
            var ownedRt = CreateUI("TxtOwnedCount", infoBox);
            ownedRt.anchorMin = new Vector2(0f, 0f);
            ownedRt.anchorMax = new Vector2(1f, 0f);
            ownedRt.pivot = new Vector2(0.5f, 0f);
            ownedRt.anchoredPosition = new Vector2(0f, 16f);
            ownedRt.sizeDelta = new Vector2(-32f, 36f);
            var txtOwned = MakeTMP(ownedRt, "Đang có: <color=#29EBE0>0</color>", 22f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Horizontal Purchase Buttons Row (Pack 1 & Pack 3)
            var btnRow = CreateUI("PackButtonsRow", panel);
            btnRow.anchorMin = new Vector2(0f, 0f);
            btnRow.anchorMax = new Vector2(1f, 0f);
            btnRow.pivot = new Vector2(0.5f, 0f);
            btnRow.anchoredPosition = new Vector2(0f, 40f);
            btnRow.sizeDelta = new Vector2(-56f, 124f);
            var hlg = btnRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 22f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            Sprite goldIcon = LoadSprite(GoldCoinPath) ?? LoadSprite(DollarIconPath);

            var btnPack1 = CreatePackButton(
                btnRow,
                "BtnBuyPack1",
                "GÓI x1",
                "150",
                goldIcon,
                new Color(0.08f, 0.19f, 0.24f, 0.96f),
                CyanOuter,
                out var txtPack1Price,
                withSaveTag: false,
                out _);

            var btnPack3 = CreatePackButton(
                btnRow,
                "BtnBuyPack3",
                "GÓI x3",
                "400",
                goldIcon,
                new Color(0.24f, 0.16f, 0.04f, 0.96f),
                GoldOuter,
                out var txtPack3Price,
                withSaveTag: true,
                out var txtPack3SaveTag);

            // Toast overlay
            var toastRt = CreateUI("ToastBanner", panel);
            toastRt.anchorMin = new Vector2(0.5f, 0f);
            toastRt.anchorMax = new Vector2(0.5f, 0f);
            toastRt.pivot = new Vector2(0.5f, 0f);
            toastRt.anchoredPosition = new Vector2(0f, 174f);
            toastRt.sizeDelta = new Vector2(420f, 48f);
            AssignSliced(toastRt.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.04f, 0.14f, 0.10f, 0.96f));
            var toastCg = toastRt.gameObject.AddComponent<CanvasGroup>();
            toastCg.alpha = 0f;
            toastCg.blocksRaycasts = false;
            var toastLblRt = CreateUI("TxtToast", toastRt);
            StretchFull(toastLblRt);
            var txtToast = MakeTMP(toastLblRt, "+1 Booster!", 22f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.45f, 1f, 0.65f, 1f));

            borderOuter.SetAsLastSibling();
            borderInner.SetAsLastSibling();
            btnClose.transform.SetAsLastSibling();
            toastRt.SetAsLastSibling();

            var controller = root.GetComponent<BuyBoosterPopupController>();
            controller.SetEditorReferences(
                txtTitle,
                btnClose,
                imgBoosterIcon,
                txtName,
                txtDesc,
                txtOwned,
                btnPack1,
                txtPack1Price,
                btnPack3,
                txtPack3Price,
                txtPack3SaveTag,
                toastCg,
                txtToast);

            SavePrefabAndScreenRef(root, BuyBoosterPrefabPath, BuyBoosterRefPath);
        }

        private static void WireGameScene(BoosterDatabaseSO db)
        {
            var activeScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var gc = Object.FindFirstObjectByType<GameController>();
            var shapesManager = Object.FindFirstObjectByType<ShapesManager>();
            if (gc == null)
            {
                Debug.LogError("[BoosterSystemBuilder] GameController not found in Game.unity.");
                return;
            }

            var canvasRect = gc.transform.Find("Canvas") as RectTransform;
            var footer = canvasRect != null ? canvasRect.Find("Footer") as RectTransform : null;
            if (footer == null)
            {
                Debug.LogError("[BoosterSystemBuilder] Canvas/Footer not found in Game.unity.");
                return;
            }

            string[] btnNames = { "BtnSkill1_Molotov", "BtnSkill2_Knuckles", "BtnSkill3_Syringe", "BtnSkill4_Grenade" };
            BoosterType[] types = { BoosterType.Hammer, BoosterType.FreeSwap, BoosterType.LineBlast, BoosterType.Shuffle };
            string[] iconPaths = { IconHammerPath, IconFreeSwapPath, IconLineBlastPath, IconShufflePath };

            var slotViews = new BoosterSlotView[4];
            Sprite circleFill = LoadSprite(CircleFillPath);
            Sprite circleBorder = LoadSprite(CircleBorderPath);
            Sprite lockIcon = LoadSprite(LockIconPath);
            Sprite borderOuter = LoadSprite(BorderOuterPath);

            for (int i = 0; i < 4; i++)
            {
                var btnRt = footer.Find(btnNames[i]) as RectTransform;
                if (btnRt == null) continue;

                var btn = btnRt.GetComponent<Button>() ?? btnRt.gameObject.AddComponent<Button>();
                var iconImg = btnRt.GetComponent<Image>();
                if (iconImg != null)
                {
                    var sprite = LoadSprite(iconPaths[i]);
                    if (sprite != null) iconImg.sprite = sprite;
                }

                // Active targeting highlight border
                var highlightRt = GetOrCreateUI(btnRt, "ActiveHighlight");
                StretchFull(highlightRt, -6f);
                var hlImg = highlightRt.GetComponent<Image>() ?? highlightRt.gameObject.AddComponent<Image>();
                AssignSliced(hlImg, BorderOuterPath, CyanOuter);
                hlImg.raycastTarget = false;
                highlightRt.gameObject.SetActive(false);

                // Small circular count/status badge (Green when >0, Red when 0)
                var badgeRt = GetOrCreateUI(btnRt, "Badge");
                badgeRt.anchorMin = new Vector2(1f, 1f);
                badgeRt.anchorMax = new Vector2(1f, 1f);
                badgeRt.pivot = new Vector2(0.5f, 0.5f);
                badgeRt.anchoredPosition = new Vector2(-4f, -4f);
                badgeRt.sizeDelta = new Vector2(36f, 36f);

                var badgeImg = badgeRt.GetComponent<Image>() ?? badgeRt.gameObject.AddComponent<Image>();
                if (circleFill != null) badgeImg.sprite = circleFill;
                badgeImg.type = Image.Type.Simple;
                badgeImg.color = GreenBadgeColor;
                badgeImg.raycastTarget = false;

                var badgeRingRt = GetOrCreateUI(badgeRt, "Ring");
                StretchFull(badgeRingRt);
                var badgeRingImg = badgeRingRt.GetComponent<Image>() ?? badgeRingRt.gameObject.AddComponent<Image>();
                if (circleBorder != null) badgeRingImg.sprite = circleBorder;
                badgeRingImg.color = Color.white;
                badgeRingImg.raycastTarget = false;

                var countTmp = badgeRt.GetComponentInChildren<TextMeshProUGUI>(true);
                if (countTmp == null)
                {
                    var countRt = CreateUI("Text", badgeRt);
                    StretchFull(countRt);
                    countTmp = MakeTMP(countRt, "2", 20f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
                }
                else
                {
                    var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                    if (font != null) countTmp.font = font;
                    countTmp.text = "2";
                    countTmp.fontSize = 20f;
                    countTmp.fontStyle = FontStyles.Bold;
                    countTmp.alignment = TextAlignmentOptions.Center;
                    countTmp.color = Color.white;
                    countTmp.raycastTarget = false;
                }

                // Lock Overlay (for Locked state)
                var lockOverlayRt = GetOrCreateUI(btnRt, "LockOverlay");
                StretchFull(lockOverlayRt);
                var lockBgImg = lockOverlayRt.GetComponent<Image>() ?? lockOverlayRt.gameObject.AddComponent<Image>();
                AssignSliced(lockBgImg, PanelFillPath, new Color(0.02f, 0.03f, 0.06f, 0.68f));
                lockBgImg.raycastTarget = false;

                var lockIconRt = GetOrCreateUI(lockOverlayRt, "LockIcon");
                lockIconRt.anchorMin = new Vector2(0.5f, 0.62f);
                lockIconRt.anchorMax = new Vector2(0.5f, 0.62f);
                lockIconRt.pivot = new Vector2(0.5f, 0.5f);
                lockIconRt.anchoredPosition = Vector2.zero;
                lockIconRt.sizeDelta = new Vector2(34f, 34f);
                var lockIconImg = lockIconRt.GetComponent<Image>() ?? lockIconRt.gameObject.AddComponent<Image>();
                if (lockIcon != null) lockIconImg.sprite = lockIcon;
                lockIconImg.preserveAspect = true;
                lockIconImg.color = new Color(0.88f, 0.92f, 0.98f, 0.95f);
                lockIconImg.raycastTarget = false;

                var lockLblRt = GetOrCreateUI(lockOverlayRt, "TxtLockLevel");
                lockLblRt.anchorMin = new Vector2(0f, 0f);
                lockLblRt.anchorMax = new Vector2(1f, 0f);
                lockLblRt.pivot = new Vector2(0.5f, 0f);
                lockLblRt.anchoredPosition = new Vector2(0f, 6f);
                lockLblRt.sizeDelta = new Vector2(0f, 24f);
                var lockTmp = MakeTMP(lockLblRt, $"Lv.{i + 1}", 17f, FontStyles.Bold, TextAlignmentOptions.Center, GoldOuter);
                lockOverlayRt.gameObject.SetActive(false);

                highlightRt.SetAsFirstSibling();
                lockOverlayRt.SetAsLastSibling();
                badgeRt.SetAsLastSibling();

                var slotView = btnRt.GetComponent<BoosterSlotView>() ?? btnRt.gameObject.AddComponent<BoosterSlotView>();
                slotView.SetEditorReferences(
                    types[i],
                    btn,
                    iconImg,
                    lockOverlayRt.gameObject,
                    lockTmp,
                    badgeRt.gameObject,
                    badgeImg,
                    countTmp,
                    highlightRt.gameObject);

                slotViews[i] = slotView;
            }

            // Targeting Hint Banner (just above Footer)
            var bannerRt = GetOrCreateUI(footer, "BoosterTargetingBanner");
            bannerRt.anchorMin = new Vector2(0.5f, 1f);
            bannerRt.anchorMax = new Vector2(0.5f, 1f);
            bannerRt.pivot = new Vector2(0.5f, 0f);
            bannerRt.anchoredPosition = new Vector2(0f, 10f);
            bannerRt.sizeDelta = new Vector2(620f, 56f);
            var bannerBg = bannerRt.GetComponent<Image>() ?? bannerRt.gameObject.AddComponent<Image>();
            AssignSliced(bannerBg, PanelFillPath, new Color(0.05f, 0.14f, 0.20f, 0.95f));

            var bannerBorderRt = GetOrCreateUI(bannerRt, "Border");
            StretchFull(bannerBorderRt);
            var bannerBorderImg = bannerBorderRt.GetComponent<Image>() ?? bannerBorderRt.gameObject.AddComponent<Image>();
            AssignSliced(bannerBorderImg, BorderOuterPath, CyanOuter);
            bannerBorderImg.raycastTarget = false;

            var hintRt = GetOrCreateUI(bannerRt, "TxtHint");
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 1f);
            hintRt.offsetMin = new Vector2(18f, 0f);
            hintRt.offsetMax = new Vector2(-112f, 0f);
            var txtHint = MakeTMP(hintRt, "Chọn mục tiêu trên bàn cờ", 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

            var cancelRt = GetOrCreateUI(bannerRt, "BtnCancel");
            cancelRt.anchorMin = new Vector2(1f, 0.5f);
            cancelRt.anchorMax = new Vector2(1f, 0.5f);
            cancelRt.pivot = new Vector2(1f, 0.5f);
            cancelRt.anchoredPosition = new Vector2(-10f, 0f);
            cancelRt.sizeDelta = new Vector2(92f, 40f);
            var cancelImg = cancelRt.GetComponent<Image>() ?? cancelRt.gameObject.AddComponent<Image>();
            AssignSliced(cancelImg, PanelFillPath, new Color(0.82f, 0.18f, 0.28f, 0.96f));
            var btnCancel = cancelRt.GetComponent<Button>() ?? cancelRt.gameObject.AddComponent<Button>();
            btnCancel.targetGraphic = cancelImg;
            var cancelLblRt = GetOrCreateUI(cancelRt, "Text");
            StretchFull(cancelLblRt);
            MakeTMP(cancelLblRt, "HỦY", 20f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            bannerRt.gameObject.SetActive(false);

            // Toast Banner
            var toastRt = GetOrCreateUI(footer, "BoosterToast");
            toastRt.anchorMin = new Vector2(0.5f, 1f);
            toastRt.anchorMax = new Vector2(0.5f, 1f);
            toastRt.pivot = new Vector2(0.5f, 0f);
            toastRt.anchoredPosition = new Vector2(0f, 14f);
            toastRt.sizeDelta = new Vector2(460f, 48f);
            var toastImg = toastRt.GetComponent<Image>() ?? toastRt.gameObject.AddComponent<Image>();
            AssignSliced(toastImg, PanelFillPath, new Color(0.06f, 0.08f, 0.14f, 0.95f));
            toastImg.raycastTarget = false;
            var toastCg = toastRt.GetComponent<CanvasGroup>() ?? toastRt.gameObject.AddComponent<CanvasGroup>();
            toastCg.alpha = 0f;
            toastCg.blocksRaycasts = false;
            var toastLblRt = GetOrCreateUI(toastRt, "TxtToast");
            StretchFull(toastLblRt);
            var txtToast = MakeTMP(toastLblRt, "Mở khóa ở Màn 2!", 22f, FontStyles.Bold, TextAlignmentOptions.Center, GoldOuter);

            var hud = footer.GetComponent<GameBoosterHUD>() ?? footer.gameObject.AddComponent<GameBoosterHUD>();
            hud.SetEditorReferences(
                db,
                shapesManager,
                slotViews,
                bannerRt.gameObject,
                txtHint,
                btnCancel,
                toastCg,
                txtToast);

            // Also wire SOManager if present in Game.unity
            var soMgr = Object.FindFirstObjectByType<SOManager>();
            if (soMgr != null)
            {
                AssignBoosterDbOnSOManager(soMgr, db);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }

        private static void WireSOManagerInScene(string scenePath, BoosterDatabaseSO db)
        {
            if (!File.Exists(scenePath)) return;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var soMgr = Object.FindFirstObjectByType<SOManager>();
            if (soMgr != null)
            {
                AssignBoosterDbOnSOManager(soMgr, db);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void AssignBoosterDbOnSOManager(SOManager soMgr, BoosterDatabaseSO db)
        {
            var so = new SerializedObject(soMgr);
            var prop = so.FindProperty("m_boosterDatabase");
            if (prop != null)
            {
                prop.objectReferenceValue = db;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(soMgr);
            }
        }

        private static Button CreatePackButton(
            RectTransform parent,
            string name,
            string title,
            string price,
            Sprite coinSprite,
            Color fillColor,
            Color borderColor,
            out TextMeshProUGUI priceText,
            bool withSaveTag,
            out TextMeshProUGUI saveTagText)
        {
            var rt = CreateUI(name, parent);
            var bg = rt.gameObject.AddComponent<Image>();
            AssignSliced(bg, PanelFillPath, fillColor);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;

            var borderRt = CreateUI("Border", rt);
            StretchFull(borderRt);
            var borderImg = borderRt.gameObject.AddComponent<Image>();
            AssignSliced(borderImg, BorderOuterPath, borderColor);
            borderImg.raycastTarget = false;

            var titleRt = CreateUI("TxtPackTitle", rt);
            titleRt.anchorMin = new Vector2(0f, 0.56f);
            titleRt.anchorMax = new Vector2(1f, 0.94f);
            titleRt.offsetMin = new Vector2(10f, 0f);
            titleRt.offsetMax = new Vector2(-10f, 0f);
            MakeTMP(titleRt, title, 28f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Price row
            var priceRow = CreateUI("PriceRow", rt);
            priceRow.anchorMin = new Vector2(0.5f, 0.24f);
            priceRow.anchorMax = new Vector2(0.5f, 0.24f);
            priceRow.pivot = new Vector2(0.5f, 0.5f);
            priceRow.anchoredPosition = Vector2.zero;
            priceRow.sizeDelta = new Vector2(180f, 40f);
            var rowLayout = priceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;

            var coinRt = CreateUI("IconCoin", priceRow);
            coinRt.sizeDelta = new Vector2(30f, 30f);
            var coinImg = coinRt.gameObject.AddComponent<Image>();
            coinImg.sprite = coinSprite;
            coinImg.preserveAspect = true;
            coinImg.raycastTarget = false;

            var priceRt = CreateUI("TxtPrice", priceRow);
            priceRt.sizeDelta = new Vector2(96f, 36f);
            priceText = MakeTMP(priceRt, price, 26f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, GoldOuter);

            saveTagText = null;
            if (withSaveTag)
            {
                var tagRt = CreateUI("SaveBadge", rt);
                tagRt.anchorMin = new Vector2(1f, 1f);
                tagRt.anchorMax = new Vector2(1f, 1f);
                tagRt.pivot = new Vector2(0.5f, 0.5f);
                tagRt.anchoredPosition = new Vector2(-8f, -4f);
                tagRt.sizeDelta = new Vector2(74f, 30f);
                var tagImg = tagRt.gameObject.AddComponent<Image>();
                AssignSliced(tagImg, PanelFillPath, new Color32(239, 68, 68, 255));
                tagImg.raycastTarget = false;

                var tagLblRt = CreateUI("Text", tagRt);
                StretchFull(tagLblRt);
                saveTagText = MakeTMP(tagLblRt, "-15%", 18f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            }

            return btn;
        }

        private static RectTransform CreateNeonPanel(
            RectTransform parent,
            string name,
            Vector2 size,
            Color fill,
            Color outerColor,
            Color innerColor,
            out RectTransform borderOuter,
            out RectTransform borderInner)
        {
            var panel = CreateUI(name, parent);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = size;
            AssignSliced(panel.gameObject.AddComponent<Image>(), PanelFillPath, fill);

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
            var rt = CreateUI("BtnClose", panel);
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-12f, -12f);
            rt.sizeDelta = new Vector2(56f, 56f);

            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = LoadSprite(CircleFillPath);
            bg.color = fillColor;

            var ringRt = CreateUI("Ring", rt);
            StretchFull(ringRt);
            var ringImg = ringRt.gameObject.AddComponent<Image>();
            ringImg.sprite = LoadSprite(CircleBorderPath);
            ringImg.color = borderColor;
            ringImg.raycastTarget = false;

            var iconRt = CreateUI("Icon", rt);
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(26f, 26f);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = LoadSprite(CloseIconPath);
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            return btn;
        }

        private static void SavePrefabAndScreenRef(GameObject root, string prefabPath, string refPath)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            var screenRef = AssetDatabase.LoadAssetAtPath<ScreenReference>(refPath);
            if (screenRef == null)
            {
                screenRef = ScriptableObject.CreateInstance<ScreenReference>();
                AssetDatabase.CreateAsset(screenRef, refPath);
            }

            screenRef.ScreenPrefab = prefab;
            EditorUtility.SetDirty(screenRef);
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;
            string parent = Path.GetDirectoryName(assetFolderPath)?.Replace("\\", "/");
            string name = Path.GetFileName(assetFolderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, name);
        }

        private static RectTransform CreateUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.gameObject.layer;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static RectTransform GetOrCreateUI(RectTransform parent, string name)
        {
            var existing = parent.Find(name) as RectTransform;
            return existing != null ? existing : CreateUI(name, parent);
        }

        private static void StretchFull(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void SetTop(RectTransform rt, float topInset, float height, float sideInset)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -topInset);
            rt.sizeDelta = new Vector2(-sideInset * 2f, height);
        }

        private static TextMeshProUGUI MakeTMP(
            RectTransform rt,
            string text,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Color color)
        {
            var tmp = rt.GetComponent<TextMeshProUGUI>() ?? rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void AssignSliced(Image img, string path, Color tint)
        {
            var sprite = LoadSprite(path);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            img.color = tint;
        }
    }
}
