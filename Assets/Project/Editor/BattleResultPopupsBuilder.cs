using System.IO;
using NTGH.UI;
using SS.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.Editor
{
    /// <summary>
    /// Builds pure-UI SS.UI prefabs and Resources/Screens ScreenReference assets
    /// for LoseWarningPopup, GameLosePopup, and GameWinPopup.
    /// </summary>
    public static class BattleResultPopupsBuilder
    {
        private const string FontPath = "Assets/Project/Font/NotoSans/NotoSans-Bold SDF.asset";
        private const string PanelFillPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_panel_fill.png";
        private const string BorderOuterPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_outer.png";
        private const string BorderInnerPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_inner.png";
        private const string CircleFillPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_circle_fill.png";
        private const string CircleBorderPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_circle_border.png";
        private const string CloseIconPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_close.png";
        private const string AdIconPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_ad.png";
        private const string DollarIconPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_dollar.png";
        private const string GoldCoinPath = "Assets/Project/Textures/icon_gold_coin.png";
        private const string CurrencyPrefabPath = "Assets/Project/Prefabs/UI/CurrencyDisplay.prefab";

        private const string LoseWarningFolder = "Assets/Project/Screens/LoseWarning";
        private const string LoseWarningPrefabPath = LoseWarningFolder + "/LoseWarningPopup.prefab";
        private const string LoseWarningRefPath = "Assets/Resources/Screens/LoseWarningPopup.asset";

        private const string GameLoseFolder = "Assets/Project/Screens/GameLose";
        private const string GameLosePrefabPath = GameLoseFolder + "/GameLosePopup.prefab";
        private const string GameLoseRefPath = "Assets/Resources/Screens/GameLosePopup.asset";

        private const string GameWinFolder = "Assets/Project/Screens/GameWin";
        private const string GameWinPrefabPath = GameWinFolder + "/GameWinPopup.prefab";
        private const string GameWinRefPath = "Assets/Resources/Screens/GameWinPopup.asset";

        // Neon 90s palette
        private static readonly Color CrimsonPanelFill = new Color(0.11f, 0.06f, 0.09f, 0.97f);
        private static readonly Color CrimsonOuter = new Color(1.00f, 0.23f, 0.36f, 1f);
        private static readonly Color GoldPanelFill = new Color(0.11f, 0.09f, 0.06f, 0.97f);
        private static readonly Color GoldOuter = new Color(1.00f, 0.78f, 0.30f, 1f);
        private static readonly Color CyanAccent = new Color(0.16f, 0.92f, 0.88f, 1f);
        private static readonly Color InnerBoxFill = new Color(0.04f, 0.05f, 0.08f, 0.82f);

        [MenuItem("NTGH/UI/Build Battle Result Popups (LoseWarning, GameLose, GameWin)")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/Resources/Screens");
            BuildLoseWarningPopup();
            BuildGameLosePopup();
            BuildGameWinPopup();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BattleResultPopupsBuilder] Built LoseWarningPopup, GameLosePopup, and GameWinPopup prefabs + ScreenReferences.");
        }

        private static void BuildLoseWarningPopup()
        {
            EnsureFolder(LoseWarningFolder);

            var root = CreateScreenRoot("LoseWarningPopup", typeof(LoseWarningPopupController));
            var rootRt = root.GetComponent<RectTransform>();

            var currencyView = AddTopCurrencyDisplay(rootRt);

            // Main Neon Crimson Panel (620 x 650)
            var panel = CreateNeonPanel(rootRt, "PanelRoot", new Vector2(620f, 650f), CrimsonPanelFill, CrimsonOuter, new Color(1f, 0.78f, 0.30f, 0.35f), out var borderOuter, out var borderInner);

            // Close Button (top-right corner)
            var btnClose = CreateCloseButton(panel, CrimsonOuter, new Color(0.14f, 0.05f, 0.08f, 1f));

            // Title: WARNING
            var titleRt = CreateUI("TxtTitle", panel);
            SetTop(titleRt, 30f, 54f, 24f);
            var txtTitle = MakeTMP(titleRt, "WARNING", 40f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.32f, 0.44f, 1f));

            // Center Warning Card
            var contentBox = CreateUI("WarningContent", panel);
            contentBox.anchorMin = new Vector2(0f, 0.5f);
            contentBox.anchorMax = new Vector2(1f, 0.5f);
            contentBox.pivot = new Vector2(0.5f, 0.5f);
            contentBox.anchoredPosition = new Vector2(0f, 18f);
            contentBox.sizeDelta = new Vector2(-56f, 320f);
            AssignSliced(contentBox.gameObject.AddComponent<Image>(), PanelFillPath, InnerBoxFill);

            var contentBorder = CreateUI("Border", contentBox);
            StretchFull(contentBorder);
            var cbImg = contentBorder.gameObject.AddComponent<Image>();
            AssignSliced(cbImg, BorderInnerPath, new Color(1f, 0.23f, 0.36f, 0.45f));
            cbImg.raycastTarget = false;

            // Gold Icon Box + Penalty Badge
            var iconBox = CreateUI("GoldIconBox", contentBox);
            iconBox.anchorMin = new Vector2(0.5f, 1f);
            iconBox.anchorMax = new Vector2(0.5f, 1f);
            iconBox.pivot = new Vector2(0.5f, 1f);
            iconBox.anchoredPosition = new Vector2(0f, -30f);
            iconBox.sizeDelta = new Vector2(128f, 128f);
            AssignSliced(iconBox.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.24f, 0.14f, 0.08f, 0.92f));

            var iconBoxBorder = CreateUI("Border", iconBox);
            StretchFull(iconBoxBorder);
            var ibbImg = iconBoxBorder.gameObject.AddComponent<Image>();
            AssignSliced(ibbImg, BorderOuterPath, GoldOuter);
            ibbImg.raycastTarget = false;

            var goldImgRt = CreateUI("IconGold", iconBox);
            goldImgRt.anchorMin = new Vector2(0.5f, 0.5f);
            goldImgRt.anchorMax = new Vector2(0.5f, 0.5f);
            goldImgRt.pivot = new Vector2(0.5f, 0.5f);
            goldImgRt.sizeDelta = new Vector2(78f, 78f);
            var goldImg = goldImgRt.gameObject.AddComponent<Image>();
            goldImg.sprite = LoadSprite(GoldCoinPath) ?? LoadSprite(DollarIconPath);
            goldImg.preserveAspect = true;
            goldImg.raycastTarget = false;

            // Penalty Badge (-200)
            var badgeRt = CreateUI("PenaltyBadge", iconBox);
            badgeRt.anchorMin = new Vector2(1f, 0f);
            badgeRt.anchorMax = new Vector2(1f, 0f);
            badgeRt.pivot = new Vector2(0.5f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(6f, 6f);
            badgeRt.sizeDelta = new Vector2(96f, 38f);
            AssignSliced(badgeRt.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.86f, 0.14f, 0.26f, 1f));
            var badgeLblRt = CreateUI("Text", badgeRt);
            StretchFull(badgeLblRt);
            var txtPenaltyBadge = MakeTMP(badgeLblRt, "-200", 22f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Warning Message
            var warnMsgRt = CreateUI("TxtWarningMessage", contentBox);
            warnMsgRt.anchorMin = new Vector2(0f, 0f);
            warnMsgRt.anchorMax = new Vector2(1f, 0f);
            warnMsgRt.pivot = new Vector2(0.5f, 0f);
            warnMsgRt.anchoredPosition = new Vector2(0f, 76f);
            warnMsgRt.sizeDelta = new Vector2(-36f, 44f);
            var txtWarningMsg = MakeTMP(warnMsgRt, "Thua trận sẽ bị mất 200 tiền!", 24f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.90f, 0.62f, 1f));

            var subMsgRt = CreateUI("TxtSubMessage", contentBox);
            subMsgRt.anchorMin = new Vector2(0f, 0f);
            subMsgRt.anchorMax = new Vector2(1f, 0f);
            subMsgRt.pivot = new Vector2(0.5f, 0f);
            subMsgRt.anchoredPosition = new Vector2(0f, 24f);
            subMsgRt.sizeDelta = new Vector2(-36f, 44f);
            var txtSubMsg = MakeTMP(subMsgRt, "Hồi phục máu để tiếp tục chiến đấu ngay.", 20f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.82f, 0.88f, 0.95f, 0.92f));

            // Horizontal Revive Buttons (CoffeeMania ReviveButtons layout)
            var btnRow = CreateHorizontalButtonRow(panel, "ReviveButtons", 36f, 108f, 22f);

            var btnGold = CreateNeonActionButton(
                btnRow,
                "BtnReviveGold",
                "HỒI SINH",
                "200",
                LoadSprite(DollarIconPath),
                new Color(0.24f, 0.16f, 0.04f, 0.96f),
                GoldOuter,
                new Color(1f, 0.92f, 0.68f, 0.45f),
                GoldOuter,
                out var txtGoldCost);

            var btnAd = CreateNeonActionButton(
                btnRow,
                "BtnReviveRV",
                "XEM QC",
                "Miễn phí",
                LoadSprite(AdIconPath),
                new Color(0.06f, 0.20f, 0.24f, 0.96f),
                CyanAccent,
                new Color(1f, 0.78f, 0.30f, 0.40f),
                CyanAccent,
                out _);

            borderOuter.SetAsLastSibling();
            borderInner.SetAsLastSibling();
            btnClose.transform.SetAsLastSibling();

            var controller = root.GetComponent<LoseWarningPopupController>();
            controller.SetEditorReferences(
                currencyView,
                txtTitle,
                btnClose,
                txtPenaltyBadge,
                txtWarningMsg,
                txtSubMsg,
                btnGold,
                txtGoldCost,
                btnAd);

            SavePrefabAndScreenRef(root, LoseWarningPrefabPath, LoseWarningRefPath);
        }

        private static void BuildGameLosePopup()
        {
            EnsureFolder(GameLoseFolder);

            var root = CreateScreenRoot("GameLosePopup", typeof(GameLosePopupController));
            var rootRt = root.GetComponent<RectTransform>();

            var currencyView = AddTopCurrencyDisplay(rootRt);

            // Main Neon Crimson Panel (620 x 620)
            var panel = CreateNeonPanel(rootRt, "PanelRoot", new Vector2(620f, 620f), CrimsonPanelFill, CrimsonOuter, new Color(0.16f, 0.92f, 0.88f, 0.30f), out var borderOuter, out var borderInner);

            // Close Button -> Home
            var btnClose = CreateCloseButton(panel, CyanAccent, new Color(0.08f, 0.12f, 0.18f, 1f));

            // Title: THẤT BẠI
            var titleRt = CreateUI("TxtTitle", panel);
            SetTop(titleRt, 30f, 54f, 24f);
            var txtTitle = MakeTMP(titleRt, "THẤT BẠI", 40f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.32f, 0.44f, 1f));

            // Center Lose Card
            var contentBox = CreateUI("LoseContent", panel);
            contentBox.anchorMin = new Vector2(0f, 0.5f);
            contentBox.anchorMax = new Vector2(1f, 0.5f);
            contentBox.pivot = new Vector2(0.5f, 0.5f);
            contentBox.anchoredPosition = new Vector2(0f, 16f);
            contentBox.sizeDelta = new Vector2(-56f, 290f);
            AssignSliced(contentBox.gameObject.AddComponent<Image>(), PanelFillPath, InnerBoxFill);

            var contentBorder = CreateUI("Border", contentBox);
            StretchFull(contentBorder);
            var cbImg = contentBorder.gameObject.AddComponent<Image>();
            AssignSliced(cbImg, BorderInnerPath, new Color(1f, 0.23f, 0.36f, 0.38f));
            cbImg.raycastTarget = false;

            // HP Empty Badge Icon
            var hpBox = CreateUI("HealthEmptyBadge", contentBox);
            hpBox.anchorMin = new Vector2(0.5f, 1f);
            hpBox.anchorMax = new Vector2(0.5f, 1f);
            hpBox.pivot = new Vector2(0.5f, 1f);
            hpBox.anchoredPosition = new Vector2(0f, -32f);
            hpBox.sizeDelta = new Vector2(120f, 120f);
            AssignSliced(hpBox.gameObject.AddComponent<Image>(), CircleFillPath, new Color(0.24f, 0.06f, 0.10f, 0.95f));
            var hpRing = CreateUI("Ring", hpBox);
            StretchFull(hpRing);
            var hpRingImg = hpRing.gameObject.AddComponent<Image>();
            AssignSliced(hpRingImg, CircleBorderPath, CrimsonOuter);
            hpRingImg.raycastTarget = false;
            var hpTextRt = CreateUI("TxtHpZero", hpBox);
            StretchFull(hpTextRt);
            MakeTMP(hpTextRt, "0 HP", 30f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.45f, 0.54f, 1f));

            // Status Text
            var statusRt = CreateUI("TxtStatus", contentBox);
            statusRt.anchorMin = new Vector2(0f, 0f);
            statusRt.anchorMax = new Vector2(1f, 0f);
            statusRt.pivot = new Vector2(0.5f, 0f);
            statusRt.anchoredPosition = new Vector2(0f, 82f);
            statusRt.sizeDelta = new Vector2(-36f, 40f);
            var txtStatus = MakeTMP(statusRt, "Bạn đã hết máu!", 26f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.86f, 1f));

            // Gold Lost Pill
            var goldPill = CreateUI("GoldLostPill", contentBox);
            goldPill.anchorMin = new Vector2(0.5f, 0f);
            goldPill.anchorMax = new Vector2(0.5f, 0f);
            goldPill.pivot = new Vector2(0.5f, 0f);
            goldPill.anchoredPosition = new Vector2(0f, 24f);
            goldPill.sizeDelta = new Vector2(240f, 46f);
            AssignSliced(goldPill.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.32f, 0.06f, 0.12f, 0.92f));
            var goldPillBorder = CreateUI("Border", goldPill);
            StretchFull(goldPillBorder);
            var gpbImg = goldPillBorder.gameObject.AddComponent<Image>();
            AssignSliced(gpbImg, BorderInnerPath, new Color(1f, 0.32f, 0.44f, 0.65f));
            gpbImg.raycastTarget = false;
            var goldLostRt = CreateUI("TxtGoldLost", goldPill);
            StretchFull(goldLostRt);
            var txtGoldLost = MakeTMP(goldLostRt, "-200 Tiền", 22f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.65f, 0.70f, 1f));

            // Single Centered Retry CTA (CoffeeMania ButtonRetry layout)
            var btnRow = CreateHorizontalButtonRow(panel, "RetryRow", 36f, 96f, 0f);
            var btnRetry = CreateNeonActionButton(
                btnRow,
                "BtnRetry",
                "ĐÁNH LẠI",
                string.Empty,
                null,
                new Color(0.06f, 0.20f, 0.24f, 0.96f),
                CyanAccent,
                new Color(1f, 0.78f, 0.30f, 0.40f),
                CyanAccent,
                out _);

            borderOuter.SetAsLastSibling();
            borderInner.SetAsLastSibling();
            btnClose.transform.SetAsLastSibling();

            var controller = root.GetComponent<GameLosePopupController>();
            controller.SetEditorReferences(
                currencyView,
                txtTitle,
                btnClose,
                txtStatus,
                goldPill.gameObject,
                txtGoldLost,
                btnRetry);

            SavePrefabAndScreenRef(root, GameLosePrefabPath, GameLoseRefPath);
        }

        private static void BuildGameWinPopup()
        {
            EnsureFolder(GameWinFolder);

            var root = CreateScreenRoot("GameWinPopup", typeof(GameWinPopupController));
            var rootRt = root.GetComponent<RectTransform>();

            // Celebration Title above panel
            var titleRt = CreateUI("TxtTitle", rootRt);
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0f, 355f);
            titleRt.sizeDelta = new Vector2(640f, 80f);
            var txtTitle = MakeTMP(titleRt, "CHIẾN THẮNG", 52f, FontStyles.Bold, TextAlignmentOptions.Center, GoldOuter);

            // Main Neon Gold Panel (620 x 580)
            var panel = CreateNeonPanel(rootRt, "PanelRoot", new Vector2(620f, 580f), GoldPanelFill, GoldOuter, new Color(0.16f, 0.92f, 0.88f, 0.40f), out var borderOuter, out var borderInner);

            // Center Reward Card
            var rewardBox = CreateUI("RewardContent", panel);
            rewardBox.anchorMin = new Vector2(0f, 0.5f);
            rewardBox.anchorMax = new Vector2(1f, 0.5f);
            rewardBox.pivot = new Vector2(0.5f, 0.5f);
            rewardBox.anchoredPosition = new Vector2(0f, 56f);
            rewardBox.sizeDelta = new Vector2(-56f, 340f);
            AssignSliced(rewardBox.gameObject.AddComponent<Image>(), PanelFillPath, InnerBoxFill);

            var rewardBorder = CreateUI("Border", rewardBox);
            StretchFull(rewardBorder);
            var rbImg = rewardBorder.gameObject.AddComponent<Image>();
            AssignSliced(rbImg, BorderInnerPath, new Color(1f, 0.78f, 0.30f, 0.45f));
            rbImg.raycastTarget = false;

            // Header "PHẦN THƯỞNG"
            var rewardHeaderRt = CreateUI("TxtRewardHeader", rewardBox);
            SetTop(rewardHeaderRt, 22f, 36f, 20f);
            MakeTMP(rewardHeaderRt, "PHẦN THƯỞNG", 22f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.58f, 0.95f));

            // x2 Badge (hidden until ad watched)
            var multBadge = CreateUI("BadgeX2", rewardBox);
            multBadge.anchorMin = new Vector2(1f, 1f);
            multBadge.anchorMax = new Vector2(1f, 1f);
            multBadge.pivot = new Vector2(1f, 1f);
            multBadge.anchoredPosition = new Vector2(-18f, -18f);
            multBadge.sizeDelta = new Vector2(68f, 36f);
            AssignSliced(multBadge.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.08f, 0.45f, 0.48f, 1f));
            var multLbl = CreateUI("Text", multBadge);
            StretchFull(multLbl);
            MakeTMP(multLbl, "x2", 22f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.75f, 1f, 0.98f, 1f));
            multBadge.gameObject.SetActive(false);

            // Gold Reward Icon Box
            var iconBox = CreateUI("RewardIconBox", rewardBox);
            iconBox.anchorMin = new Vector2(0.5f, 0.5f);
            iconBox.anchorMax = new Vector2(0.5f, 0.5f);
            iconBox.pivot = new Vector2(0.5f, 0.5f);
            iconBox.anchoredPosition = new Vector2(0f, 12f);
            iconBox.sizeDelta = new Vector2(148f, 148f);
            AssignSliced(iconBox.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.24f, 0.17f, 0.06f, 0.92f));
            var iconBorder = CreateUI("Border", iconBox);
            StretchFull(iconBorder);
            var ibImg = iconBorder.gameObject.AddComponent<Image>();
            AssignSliced(ibImg, BorderOuterPath, GoldOuter);
            ibImg.raycastTarget = false;

            var goldImgRt = CreateUI("IconGold", iconBox);
            goldImgRt.anchorMin = new Vector2(0.5f, 0.5f);
            goldImgRt.anchorMax = new Vector2(0.5f, 0.5f);
            goldImgRt.pivot = new Vector2(0.5f, 0.5f);
            goldImgRt.sizeDelta = new Vector2(92f, 92f);
            var goldImg = goldImgRt.gameObject.AddComponent<Image>();
            goldImg.sprite = LoadSprite(GoldCoinPath) ?? LoadSprite(DollarIconPath);
            goldImg.preserveAspect = true;
            goldImg.raycastTarget = false;

            // Gold Amount Text (+350)
            var amountRt = CreateUI("TxtGoldAmount", rewardBox);
            amountRt.anchorMin = new Vector2(0f, 0f);
            amountRt.anchorMax = new Vector2(1f, 0f);
            amountRt.pivot = new Vector2(0.5f, 0f);
            amountRt.anchoredPosition = new Vector2(0f, 24f);
            amountRt.sizeDelta = new Vector2(-36f, 56f);
            var txtGoldAmount = MakeTMP(amountRt, "+350", 44f, FontStyles.Bold, TextAlignmentOptions.Center, GoldOuter);

            // Bottom Horizontal Buttons (CoffeeMania GameWinV2 BottomButtons layout)
            var btnRow = CreateHorizontalButtonRow(panel, "BottomButtons", 36f, 108f, 22f);

            var btnAdX2 = CreateNeonActionButton(
                btnRow,
                "BtnWatchAdsX2",
                "NHẬN x2",
                "Xem QC",
                LoadSprite(AdIconPath),
                new Color(0.24f, 0.16f, 0.04f, 0.96f),
                GoldOuter,
                new Color(1f, 0.92f, 0.68f, 0.45f),
                GoldOuter,
                out _);

            var btnNext = CreateNeonActionButton(
                btnRow,
                "BtnNext",
                "TIẾP TỤC",
                string.Empty,
                null,
                new Color(0.06f, 0.20f, 0.24f, 0.96f),
                CyanAccent,
                new Color(1f, 0.78f, 0.30f, 0.40f),
                CyanAccent,
                out _);

            borderOuter.SetAsLastSibling();
            borderInner.SetAsLastSibling();

            var controller = root.GetComponent<GameWinPopupController>();
            controller.SetEditorReferences(
                txtTitle,
                iconBox,
                txtGoldAmount,
                multBadge.gameObject,
                btnAdX2,
                btnNext);

            SavePrefabAndScreenRef(root, GameWinPrefabPath, GameWinRefPath);
        }

        // =========================================================================
        // UI Helper Builders
        // =========================================================================

        private static GameObject CreateScreenRoot(string name, System.Type controllerType)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), controllerType);
            root.layer = LayerMask.NameToLayer("UI");
            StretchFull(root.GetComponent<RectTransform>());
            return root;
        }

        private static CurrencyDisplayView AddTopCurrencyDisplay(RectTransform rootRt)
        {
            var currencyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CurrencyPrefabPath);
            if (currencyPrefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(currencyPrefab, rootRt);
                inst.name = "CurrencyDisplay";
                var rt = inst.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(36f, -56f);
                var view = inst.GetComponent<CurrencyDisplayView>();
                if (view != null && view.AddButton != null)
                    view.AddButton.gameObject.SetActive(false);
                return view;
            }

            // Fallback pill if prefab is missing
            var pill = CreateUI("CurrencyDisplay", rootRt);
            pill.anchorMin = new Vector2(0f, 1f);
            pill.anchorMax = new Vector2(0f, 1f);
            pill.pivot = new Vector2(0f, 1f);
            pill.anchoredPosition = new Vector2(36f, -56f);
            pill.sizeDelta = new Vector2(210f, 56f);
            AssignSliced(pill.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.06f, 0.08f, 0.13f, 0.94f));
            var txtRt = CreateUI("TxtAmount", pill);
            StretchFull(txtRt);
            var txt = MakeTMP(txtRt, "0", 24f, FontStyles.Bold, TextAlignmentOptions.Center, GoldOuter);
            var fallbackView = pill.gameObject.AddComponent<CurrencyDisplayView>();
            fallbackView.ConfigureReferences(txt, null, null);
            return fallbackView;
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
            var btnRt = CreateUI("BtnClose", panel);
            btnRt.anchorMin = new Vector2(1f, 1f);
            btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-8f, -8f);
            btnRt.sizeDelta = new Vector2(68f, 68f);

            var fillImg = btnRt.gameObject.AddComponent<Image>();
            AssignSliced(fillImg, CircleFillPath, fillColor);
            var button = btnRt.gameObject.AddComponent<Button>();

            var borderRt = CreateUI("Border", btnRt);
            StretchFull(borderRt);
            var borderImg = borderRt.gameObject.AddComponent<Image>();
            AssignSliced(borderImg, CircleBorderPath, borderColor);
            borderImg.raycastTarget = false;

            var iconRt = CreateUI("IconClose", btnRt);
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(30f, 30f);
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = LoadSprite(CloseIconPath);
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var style = btnRt.gameObject.AddComponent<NeonButtonStyle>();
            style.ConfigureReferences(fillImg, borderImg, null, iconImg);
            style.SetPalette(fillColor, borderColor, borderColor, Color.white, Color.white);

            return button;
        }

        private static RectTransform CreateHorizontalButtonRow(RectTransform panel, string name, float bottomOffset, float height, float spacing)
        {
            var row = CreateUI(name, panel);
            row.anchorMin = new Vector2(0f, 0f);
            row.anchorMax = new Vector2(1f, 0f);
            row.pivot = new Vector2(0.5f, 0f);
            row.anchoredPosition = new Vector2(0f, bottomOffset);
            row.sizeDelta = new Vector2(-56f, height);

            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            return row;
        }

        private static Button CreateNeonActionButton(
            RectTransform parent,
            string name,
            string primaryLabel,
            string secondaryLabel,
            Sprite iconSprite,
            Color fillColor,
            Color outerBorderColor,
            Color innerBorderColor,
            Color accentColor,
            out TextMeshProUGUI secondaryTmp)
        {
            var btnRt = CreateUI(name, parent);
            var fillImg = btnRt.gameObject.AddComponent<Image>();
            AssignSliced(fillImg, PanelFillPath, fillColor);
            var button = btnRt.gameObject.AddComponent<Button>();

            var outerRt = CreateUI("OuterBorder", btnRt);
            StretchFull(outerRt);
            var outerImg = outerRt.gameObject.AddComponent<Image>();
            AssignSliced(outerImg, BorderOuterPath, outerBorderColor);
            outerImg.raycastTarget = false;

            var innerRt = CreateUI("InnerBorder", btnRt);
            StretchFull(innerRt, 5f);
            var innerImg = innerRt.gameObject.AddComponent<Image>();
            AssignSliced(innerImg, BorderInnerPath, innerBorderColor);
            innerImg.raycastTarget = false;

            bool hasSecondary = !string.IsNullOrEmpty(secondaryLabel);
            Image iconImg = null;
            TextMeshProUGUI primaryTmp;
            secondaryTmp = null;

            if (!hasSecondary)
            {
                var lblRt = CreateUI("Label", btnRt);
                StretchFull(lblRt, 10f);
                primaryTmp = MakeTMP(lblRt, primaryLabel, 28f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.97f, 0.90f, 1f));
            }
            else
            {
                var topRt = CreateUI("TxtPrimary", btnRt);
                topRt.anchorMin = new Vector2(0f, 0.5f);
                topRt.anchorMax = new Vector2(1f, 1f);
                topRt.offsetMin = new Vector2(10f, 2f);
                topRt.offsetMax = new Vector2(-10f, -8f);
                primaryTmp = MakeTMP(topRt, primaryLabel, 22f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.97f, 0.90f, 1f));

                var subRow = CreateUI("SubRow", btnRt);
                subRow.anchorMin = new Vector2(0f, 0f);
                subRow.anchorMax = new Vector2(1f, 0.5f);
                subRow.offsetMin = new Vector2(10f, 8f);
                subRow.offsetMax = new Vector2(-10f, -2f);
                var subHlg = subRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                subHlg.spacing = 8f;
                subHlg.childAlignment = TextAnchor.MiddleCenter;
                subHlg.childControlWidth = false;
                subHlg.childControlHeight = false;
                subHlg.childForceExpandWidth = false;
                subHlg.childForceExpandHeight = false;

                if (iconSprite != null)
                {
                    var iconRt = CreateUI("Icon", subRow);
                    iconRt.sizeDelta = new Vector2(26f, 26f);
                    iconImg = iconRt.gameObject.AddComponent<Image>();
                    iconImg.sprite = iconSprite;
                    iconImg.color = accentColor;
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;
                }

                var secRt = CreateUI("TxtSecondary", subRow);
                secRt.sizeDelta = new Vector2(120f, 32f);
                secondaryTmp = MakeTMP(secRt, secondaryLabel, 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, accentColor);
                var csf = secRt.gameObject.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            var style = btnRt.gameObject.AddComponent<NeonButtonStyle>();
            style.ConfigureReferences(fillImg, outerImg, innerImg, iconImg, primaryTmp);
            style.SetPalette(fillColor, outerBorderColor, innerBorderColor, accentColor, new Color(1f, 0.97f, 0.90f, 1f));

            return button;
        }

        private static void SavePrefabAndScreenRef(GameObject root, string prefabPath, string screenRefPath)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            var screenRef = GetOrCreateAsset<ScreenReference>(screenRefPath);
            screenRef.ScreenPrefab = prefab;
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

        private static void SetTop(RectTransform rt, float top, float height, float horizontalInset = 0f)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(-horizontalInset * 2f, height);
        }

        private static void AssignSliced(Image img, string spritePath, Color color)
        {
            img.sprite = LoadSprite(spritePath);
            img.type = img.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static TextMeshProUGUI MakeTMP(RectTransform rt, string text, float fontSize, FontStyles style, TextAlignmentOptions align, Color color)
        {
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;
            string parent = Path.GetDirectoryName(assetFolderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(assetFolderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
