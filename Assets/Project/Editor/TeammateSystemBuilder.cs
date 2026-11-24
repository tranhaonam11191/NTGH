using System.Collections.Generic;
using System.IO;
using NTGH.Teammates;
using NTGH.UI;
using SS.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.Editor
{
    public static class TeammateSystemBuilder
    {
        private const string FontPath = "Assets/Project/Font/NotoSans/NotoSans-Bold SDF.asset";
        private const string PanelFillPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_panel_fill.png";
        private const string BorderOuterPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_outer.png";
        private const string BorderInnerPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_inner.png";
        private const string CloseIconPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_close.png";

        private const string AttackFramePath = "Assets/Project/Sprites/UI/TeammateCards/card_frame_attack.png";
        private const string SupportFramePath = "Assets/Project/Sprites/UI/TeammateCards/card_frame_support.png";

        private const string DataRoot = "Assets/Project/Data/Teammates";
        private const string ThemesFolder = DataRoot + "/CardThemes";
        private const string CardsFolder = DataRoot + "/Cards";
        private const string DatabasePath = DataRoot + "/TeammateCardDatabase.asset";

        private const string WeaponCatalogPath = "Assets/Project/Data/ItemCatalogs/ItemCatalog_Weapon.asset";
        private const string OutfitCatalogPath = "Assets/Project/Data/ItemCatalogs/ItemCatalog_Outfit.asset";

        private const string PopupFolder = "Assets/Project/Screens/TeammateSquadPopup";
        private const string PopupPrefabPath = PopupFolder + "/TeammateSquadPopup.prefab";
        private const string ScreenRefPath = "Assets/Resources/Screens/TeammateSquadPopup.asset";

        private const string RewardScreenFolder = "Assets/Project/Screens/TeammateCardRewardScreen";
        private const string RewardPrefabPath = RewardScreenFolder + "/TeammateCardRewardScreen.prefab";
        private const string RewardScreenRefPath = "Assets/Resources/Screens/TeammateCardRewardScreen.asset";

        [MenuItem("NTGH/Teammates/Build Teammate System (Data + Popup + Wiring)")]
        public static void BuildAll()
        {
            EnsureSpriteImport(AttackFramePath);
            EnsureSpriteImport(SupportFramePath);
            AssetDatabase.Refresh();

            var database = BuildScriptableObjects();
            BuildTeammateSquadPopupPrefab();
            BuildTeammateCardRewardScreenPrefab();
            PopulateSampleEnemyData(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WireScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TeammateSystemBuilder] Built Teammate Card System (SOs, SS Popup Prefabs, Scene Wiring) successfully.");
        }

        [MenuItem("NTGH/Teammates/Build Story Card Reward & Squad UI")]
        public static void BuildStoryRewardAndSquadUI()
        {
            BuildTeammateSquadPopupPrefab();
            BuildTeammateCardRewardScreenPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TeammateSystemBuilder] Built TeammateSquadPopup and TeammateCardRewardScreen prefabs + ScreenReferences.");
        }

        [MenuItem("NTGH/Teammates/Setup Story Card Reward Test Data")]
        public static void SetupStoryCardRewardTestData()
        {
            var level01 = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Project/Scenes/Data/DauBu/Level01.asset");
            var cardThaiTu = AssetDatabase.LoadAssetAtPath<TeammateCardSO>(CardsFolder + "/Card_ThaiTu.asset");
            var cardDieuDuong = AssetDatabase.LoadAssetAtPath<TeammateCardSO>(CardsFolder + "/Card_DieuDuong.asset");
            var cardQuaDen = AssetDatabase.LoadAssetAtPath<TeammateCardSO>(CardsFolder + "/Card_QuaDen.asset");

            if (level01 == null || cardThaiTu == null || cardDieuDuong == null || cardQuaDen == null)
            {
                Debug.LogError("[TeammateSystemBuilder] Missing Level01 or TeammateCardSO assets for test data setup.");
                return;
            }

            // 1. Mark Card_ThaiTu as storyOnly so it is hidden in Home Shop while locked
            var soThaiTu = new SerializedObject(cardThaiTu);
            var storyOnlyProp = soThaiTu.FindProperty("m_storyOnly");
            if (storyOnlyProp != null)
            {
                storyOnlyProp.boolValue = true;
                soThaiTu.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(cardThaiTu);
            }

            // 2. Attach 2 cards (Thai Tu [StoryOnly] + Dieu Duong) to Level01 StoryIntro (Trigger = Start)
            var startGroup = level01.conversations.Find(c => c != null && c.trigger == ConversationTrigger.Start && c.lines != null && c.lines.Count > 0);
            if (startGroup != null)
            {
                var targetLine = startGroup.lines[startGroup.lines.Count - 1];
                targetLine.rewardTeammateCards = new List<TeammateCardSO> { cardThaiTu, cardDieuDuong };
            }

            // 3. Attach 1 card (Qua Den) to Level01 Victory conversation
            var victoryGroup = level01.conversations.Find(c => c != null && c.trigger == ConversationTrigger.Victory && c.lines != null && c.lines.Count > 1);
            if (victoryGroup != null)
            {
                var targetLine = victoryGroup.lines[1]; // "A Dũng: Tao nhờ anh Quạ Đen xử bọn nó rồi..."
                targetLine.rewardTeammateCards = new List<TeammateCardSO> { cardQuaDen };
            }

            EditorUtility.SetDirty(level01);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. Reset teammate save & level index to 0 for immediate testing
            ResetTeammateSaveDataForTesting();

            Debug.Log("[TeammateSystemBuilder] Setup Story Card Reward Test Data on Level01 (Intro: Thái Tử [StoryOnly] + Diệu Dương; Victory: Quạ Đen) & reset save data.");
        }

        [MenuItem("NTGH/Teammates/Reset Teammate Save Data (Test)")]
        public static void ResetTeammateSaveDataForTesting()
        {
            var database = AssetDatabase.LoadAssetAtPath<TeammateCardDatabaseSO>(DatabasePath);
            User.EnsureLoaded();
            if (User.data == null) return;

            User.data.unlocked_teammate_ids.Clear();
            User.data.equipped_teammate_ids.Clear();
            User.data.unseen_teammate_ids.Clear();
            User.data.level = 0;
            User.data.EnsureTeammateDefaults(database);
            User.Save();

            Debug.Log("[TeammateSystemBuilder] Reset teammate unlock/equip/new save data and set level = 0 (Level01).");
        }

        private static void EnsureSpriteImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
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
            if (dirty) importer.SaveAndReimport();
        }

        private static TeammateCardDatabaseSO BuildScriptableObjects()
        {
            EnsureFolder(DataRoot);
            EnsureFolder(ThemesFolder);
            EnsureFolder(CardsFolder);

            var attackFrame = AssetDatabase.LoadAssetAtPath<Sprite>(AttackFramePath);
            var supportFrame = AssetDatabase.LoadAssetAtPath<Sprite>(SupportFramePath);

            var attackTheme = GetOrCreateAsset<TeammateCardThemeSO>(ThemesFolder + "/CardTheme_Attack.asset");
            attackTheme.Configure(
                TeammateCardType.Attack,
                "CÔNG",
                attackFrame,
                null,
                Color.white,
                new Color(0f, 0f, 0f, 0f), // Frame already has beveled gold & crimson border
                new Color(0.84f, 0.16f, 0.20f, 1f),
                new Color(0.96f, 0.28f, 0.26f, 1f));
            EditorUtility.SetDirty(attackTheme);

            var supportTheme = GetOrCreateAsset<TeammateCardThemeSO>(ThemesFolder + "/CardTheme_Support.asset");
            supportTheme.Configure(
                TeammateCardType.Support,
                "THỦ",
                supportFrame,
                null,
                Color.white,
                new Color(0f, 0f, 0f, 0f),
                new Color(0.14f, 0.66f, 0.90f, 1f),
                new Color(0.20f, 0.80f, 0.96f, 1f));
            EditorUtility.SetDirty(supportTheme);

            var cards = new List<TeammateCardSO>
            {
                CreateOrUpdateCard(
                    "Card_TranHaoNam", "tm_tran_hao_nam", "Trần Hạo Nam",
                    TeammateCardType.Attack,
                    "Assets/Project/Textures/Characters/TranHaoNam.png",
                    effectValue: 18, priceGold: 0, unlockedByDefault: true,
                    "Đại ca Đồng La Loan. Tăng mạnh sát thương mỗi đòn tấn công."),
                CreateOrUpdateCard(
                    "Card_ADung", "tm_a_dung", "A Dũng",
                    TeammateCardType.Support,
                    "Assets/Project/Textures/Characters/a_dung_l01_giao_bao.png",
                    effectValue: 8, priceGold: 0, unlockedByDefault: true,
                    "Huynh đệ trung thành, che chắn giảm sát thương địch gây ra."),
                CreateOrUpdateCard(
                    "Card_DieuDuong", "tm_dieu_duong", "Diệu Dương",
                    TeammateCardType.Support,
                    "Assets/Project/Textures/Characters/DieuDuong.png",
                    effectValue: 12, priceGold: 350, unlockedByDefault: false,
                    "Mưu sĩ lạnh lùng, giảm mạnh sát thương từ đòn đánh đối thủ."),
                CreateOrUpdateCard(
                    "Card_ThaiTu", "tm_thai_tu", "Thái Tử",
                    TeammateCardType.Attack,
                    "Assets/Project/Textures/Characters/ThaiTu.png",
                    effectValue: 25, priceGold: 400, unlockedByDefault: false,
                    "Chiến thần Hồng Hưng. Cộng thêm sát thương bùng nổ khi ra đòn."),
                CreateOrUpdateCard(
                    "Card_QuaDen", "tm_qua_den", "Quạ Đen",
                    TeammateCardType.Attack,
                    "Assets/Project/Textures/Characters/QuaDen.png",
                    effectValue: 35, priceGold: 800, unlockedByDefault: false,
                    "Mãnh tướng Đông Anh. Sát thương tấn công cực đại.")
            };

            var database = GetOrCreateAsset<TeammateCardDatabaseSO>(DatabasePath);
            database.Configure(attackTheme, supportTheme, cards);
            EditorUtility.SetDirty(database);
            return database;
        }

        private static TeammateCardSO CreateOrUpdateCard(
            string assetName,
            string id,
            string displayName,
            TeammateCardType cardType,
            string avatarPath,
            int effectValue,
            int priceGold,
            bool unlockedByDefault,
            string description)
        {
            var card = GetOrCreateAsset<TeammateCardSO>($"{CardsFolder}/{assetName}.asset");
            var avatar = AssetDatabase.LoadAssetAtPath<Sprite>(avatarPath);
            card.Configure(id, displayName, cardType, avatar, effectValue, priceGold, unlockedByDefault, description);
            EditorUtility.SetDirty(card);
            return card;
        }

        private static void PopulateSampleEnemyData(TeammateCardDatabaseSO database)
        {
            if (database == null) return;
            var quaDen = database.GetCardById("tm_qua_den");
            var dieuDuong = database.GetCardById("tm_dieu_duong");
            var thaiTu = database.GetCardById("tm_thai_tu");

            var enemyTargets = new HashSet<CharacterData>();

            // Collect every enemy referenced by any LevelData asset
            var levelGuids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Project/Scenes/Data" });
            foreach (string guid in levelGuids)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (level != null && level.enemy != null)
                {
                    enemyTargets.Add(level.enemy);
                }
            }

            // Also include any non-player CharacterData
            var charGuids = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/Project/Scenes/Data" });
            foreach (string guid in charGuids)
            {
                var cd = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));
                if (cd != null && cd.Character != null && !cd.Character.isPlayer)
                {
                    enemyTargets.Add(cd);
                }
            }

            foreach (var cd in enemyTargets)
            {
                if (cd.equippedTeammates == null || cd.equippedTeammates.Count == 0)
                {
                    cd.equippedWeaponId = "Katana";
                    cd.equippedOutfitId = "outfit_lv2_hongcon";
                    cd.equippedTeammates = new List<TeammateCardSO>();
                    if (quaDen != null) cd.equippedTeammates.Add(quaDen);
                    if (dieuDuong != null) cd.equippedTeammates.Add(dieuDuong);
                    if (thaiTu != null) cd.equippedTeammates.Add(thaiTu);
                    EditorUtility.SetDirty(cd);
                }
            }
        }

        private static void BuildTeammateSquadPopupPrefab()
        {
            EnsureFolder(PopupFolder);
            EnsureFolder("Assets/Resources/Screens");

            var root = new GameObject("TeammateSquadPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(TeammateSquadPopupController));
            root.layer = LayerMask.NameToLayer("UI");
            var rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            // Main Neon90s Panel (660 x 1160)
            var panel = CreateUI("Panel", rootRt);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(660f, 1180f);
            AssignSliced(panel.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.05f, 0.08f, 0.12f, 0.96f));

            var borderOuter = CreateUI("BorderOuter", panel);
            StretchFull(borderOuter);
            var boImg = borderOuter.gameObject.AddComponent<Image>();
            AssignSliced(boImg, BorderOuterPath, new Color(0.14f, 0.84f, 0.88f, 1f));
            boImg.raycastTarget = false;

            var borderInner = CreateUI("BorderInner", panel);
            StretchFull(borderInner, 6f);
            var biImg = borderInner.gameObject.AddComponent<Image>();
            AssignSliced(biImg, BorderInnerPath, new Color(0.95f, 0.76f, 0.30f, 0.45f));
            biImg.raycastTarget = false;

            // Header
            var txtTitleRect = CreateUI("TxtTitle", panel);
            SetTop(txtTitleRect, 20f, 44f, 24f);
            var txtTitle = MakeTMP(txtTitleRect, "ĐỘI HÌNH ĐỒNG ĐỘI", 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.94f, 0.76f, 1f));

            var txtGoldRect = CreateUI("TxtGold", panel);
            txtGoldRect.anchorMin = new Vector2(1f, 1f);
            txtGoldRect.anchorMax = new Vector2(1f, 1f);
            txtGoldRect.pivot = new Vector2(1f, 1f);
            txtGoldRect.anchoredPosition = new Vector2(-84f, -24f);
            txtGoldRect.sizeDelta = new Vector2(190f, 38f);
            var txtGold = MakeTMP(txtGoldRect, "0 $", 24f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(1f, 0.84f, 0.24f, 1f));

            var closeRect = CreateUI("BtnClose", panel);
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-18f, -18f);
            closeRect.sizeDelta = new Vector2(52f, 52f);
            var closeImg = closeRect.gameObject.AddComponent<Image>();
            AssignSliced(closeImg, PanelFillPath, new Color(0.65f, 0.14f, 0.18f, 0.95f));
            var btnClose = closeRect.gameObject.AddComponent<Button>();
            var closeIcon = CreateUI("Icon", closeRect);
            StretchFull(closeIcon, 12f);
            var ci = closeIcon.gameObject.AddComponent<Image>();
            ci.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CloseIconPath);
            ci.preserveAspect = true;

            // Squad Header + 4 Equipped Slots
            var squadHeaderRect = CreateUI("TxtSquadHeader", panel);
            SetTop(squadHeaderRect, 80f, 28f, 24f);
            var txtSquadHeader = MakeTMP(squadHeaderRect, "ĐỘI HÌNH XUẤT TRẬN (0/4)", 18f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.70f, 0.92f, 0.98f, 1f));

            var squadRow = CreateUI("SquadSlotsRow", panel);
            SetTop(squadRow, 114f, 210f, 20f);
            var squadHlg = squadRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            squadHlg.spacing = 12f;
            squadHlg.childAlignment = TextAnchor.MiddleCenter;
            squadHlg.childControlWidth = false;
            squadHlg.childControlHeight = false;

            var slotViews = new TeammateCardView[4];
            var slotButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                var slotRt = BuildReusableCardUI(squadRow, $"SquadSlot_{i + 1}", 142f, 206f, out var view);
                var btn = slotRt.gameObject.AddComponent<Button>();
                slotViews[i] = view;
                slotButtons[i] = btn;
            }

            // Status Notice
            var noticeRect = CreateUI("TxtNotice", panel);
            SetTop(noticeRect, 330f, 26f, 24f);
            var txtNotice = MakeTMP(noticeRect, "", 16f, FontStyles.Italic, TextAlignmentOptions.Center, new Color(1f, 0.78f, 0.34f, 1f));

            // Filter Bar
            var filterRow = CreateUI("FilterRow", panel);
            SetTop(filterRow, 362f, 48f, 24f);
            var filterHlg = filterRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            filterHlg.spacing = 14f;
            filterHlg.childAlignment = TextAnchor.MiddleCenter;
            filterHlg.childControlWidth = true;
            filterHlg.childControlHeight = true;
            filterHlg.childForceExpandWidth = true;
            filterHlg.childForceExpandHeight = true;

            var btnAll = CreateFilterTab(filterRow, "Tab_All", "TẤT CẢ");
            var btnAttack = CreateFilterTab(filterRow, "Tab_Attack", "CÔNG (ĐỎ)");
            var btnSupport = CreateFilterTab(filterRow, "Tab_Support", "THỦ (XANH)");

            // ScrollRect for Collection / Shop Cards
            var scrollRectRt = CreateUI("ScrollView", panel);
            scrollRectRt.anchorMin = Vector2.zero;
            scrollRectRt.anchorMax = Vector2.one;
            scrollRectRt.offsetMin = new Vector2(20f, 20f);
            scrollRectRt.offsetMax = new Vector2(-20f, -424f);
            var scrollRect = scrollRectRt.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var viewport = CreateUI("Viewport", scrollRectRt);
            StretchFull(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            scrollRect.viewport = viewport;

            var content = CreateUI("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 0f);
            var glg = content.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(190f, 340f);
            glg.spacing = new Vector2(16f, 18f);
            glg.padding = new RectOffset(8, 8, 8, 16);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.UpperCenter;
            var csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = content;

            // Shop Item Template (CardView + Effect Text + Action Button)
            var shopItem = CreateUI("ShopCardTemplate", content);
            shopItem.sizeDelta = new Vector2(190f, 340f);
            AssignSliced(shopItem.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.08f, 0.11f, 0.16f, 0.90f));

            var cardContainer = BuildReusableCardUI(shopItem, "CardView", 164f, 236f, out _);
            cardContainer.anchorMin = new Vector2(0.5f, 1f);
            cardContainer.anchorMax = new Vector2(0.5f, 1f);
            cardContainer.pivot = new Vector2(0.5f, 1f);
            cardContainer.anchoredPosition = new Vector2(0f, -8f);

            var effectRect = CreateUI("TxtEffect", shopItem);
            effectRect.anchorMin = new Vector2(0f, 0f);
            effectRect.anchorMax = new Vector2(1f, 0f);
            effectRect.pivot = new Vector2(0.5f, 0f);
            effectRect.anchoredPosition = new Vector2(0f, 54f);
            effectRect.sizeDelta = new Vector2(-12f, 36f);
            var effTmp = MakeTMP(effectRect, "Tấn công +18 ST", 13f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.84f, 0.90f, 0.96f, 0.92f));
            effTmp.textWrappingMode = TextWrappingModes.Normal;

            var btnActionRect = CreateUI("BtnAction", shopItem);
            btnActionRect.anchorMin = new Vector2(0f, 0f);
            btnActionRect.anchorMax = new Vector2(1f, 0f);
            btnActionRect.pivot = new Vector2(0.5f, 0f);
            btnActionRect.anchoredPosition = new Vector2(0f, 8f);
            btnActionRect.sizeDelta = new Vector2(-16f, 40f);
            AssignSliced(btnActionRect.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.12f, 0.62f, 0.58f, 1f));
            btnActionRect.gameObject.AddComponent<Button>();
            var btnActionLbl = CreateUI("Label", btnActionRect);
            StretchFull(btnActionLbl);
            MakeTMP(btnActionLbl, "XUẤT TRẬN", 16f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // NEW Badge (top-right corner of shop item)
            var badgeNew = CreateUI("BadgeNew", shopItem);
            badgeNew.anchorMin = new Vector2(1f, 1f);
            badgeNew.anchorMax = new Vector2(1f, 1f);
            badgeNew.pivot = new Vector2(1f, 1f);
            badgeNew.anchoredPosition = new Vector2(-4f, -4f);
            badgeNew.sizeDelta = new Vector2(64f, 26f);
            var badgeNewImg = badgeNew.gameObject.AddComponent<Image>();
            AssignSliced(badgeNewImg, PanelFillPath, new Color(0.96f, 0.22f, 0.28f, 1f));
            badgeNewImg.raycastTarget = false;
            var badgeNewLbl = CreateUI("Label", badgeNew);
            StretchFull(badgeNewLbl);
            var badgeNewTmp = MakeTMP(badgeNewLbl, "NEW", 14f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.96f, 0.72f, 1f));
            badgeNewTmp.raycastTarget = false;
            badgeNew.gameObject.SetActive(false);

            // Pre-Battle Start Battle Button (bottom of panel, hidden in Home mode)
            var btnStartRect = CreateUI("BtnStartBattle", panel);
            btnStartRect.anchorMin = new Vector2(0.5f, 0f);
            btnStartRect.anchorMax = new Vector2(0.5f, 0f);
            btnStartRect.pivot = new Vector2(0.5f, 0f);
            btnStartRect.anchoredPosition = new Vector2(0f, 18f);
            btnStartRect.sizeDelta = new Vector2(360f, 58f);
            AssignSliced(btnStartRect.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.92f, 0.42f, 0.12f, 1f));
            var btnStartBattle = btnStartRect.gameObject.AddComponent<Button>();
            var btnStartLblRect = CreateUI("Label", btnStartRect);
            StretchFull(btnStartLblRect);
            var txtStartBattleLabel = MakeTMP(btnStartLblRect, "VÀO TRẬN", 24f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            btnStartRect.gameObject.SetActive(false);

            borderOuter.SetAsLastSibling();
            borderInner.SetAsLastSibling();

            var controller = root.GetComponent<TeammateSquadPopupController>();
            controller.SetEditorReferences(
                txtTitle, txtGold, txtSquadHeader, txtNotice, btnClose,
                slotViews, slotButtons, btnAll, btnAttack, btnSupport,
                scrollRect, content, shopItem.gameObject,
                btnStartBattle, txtStartBattleLabel);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PopupPrefabPath);
            Object.DestroyImmediate(root);

            // Register SS ScreenReference in Resources/Screens/
            var screenRef = GetOrCreateAsset<ScreenReference>(ScreenRefPath);
            screenRef.ScreenPrefab = prefab;
            EditorUtility.SetDirty(screenRef);
        }

        private static void BuildTeammateCardRewardScreenPrefab()
        {
            EnsureFolder(RewardScreenFolder);
            EnsureFolder("Assets/Resources/Screens");

            var root = new GameObject(
                "TeammateCardRewardScreen",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TeammateCardRewardController));
            root.layer = LayerMask.NameToLayer("UI");
            var rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            // Main Neon90s Panel (660 x 760)
            var panel = CreateUI("Panel", rootRt);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(660f, 760f);
            AssignSliced(panel.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.05f, 0.08f, 0.13f, 0.97f));

            var borderOuter = CreateUI("BorderOuter", panel);
            StretchFull(borderOuter);
            var boImg = borderOuter.gameObject.AddComponent<Image>();
            AssignSliced(boImg, BorderOuterPath, new Color(0.96f, 0.76f, 0.24f, 1f));
            boImg.raycastTarget = false;

            var borderInner = CreateUI("BorderInner", panel);
            StretchFull(borderInner, 6f);
            var biImg = borderInner.gameObject.AddComponent<Image>();
            AssignSliced(biImg, BorderInnerPath, new Color(0.14f, 0.84f, 0.88f, 0.55f));
            biImg.raycastTarget = false;

            // Title & Subtitle
            var titleRect = CreateUI("TxtTitle", panel);
            SetTop(titleRect, 28f, 48f, 24f);
            var txtTitle = MakeTMP(titleRect, "CHIÊU MỘ ĐỒNG ĐỘI MỚI!", 32f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.52f, 1f));

            var subtitleRect = CreateUI("TxtSubtitle", panel);
            SetTop(subtitleRect, 80f, 30f, 24f);
            var txtSubtitle = MakeTMP(subtitleRect, "Nhận miễn phí theo cốt truyện", 18f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.72f, 0.90f, 0.98f, 0.95f));

            // Cards Container (supports 1..3 cards)
            var cardsRow = CreateUI("CardsContainer", panel);
            cardsRow.anchorMin = new Vector2(0f, 0.5f);
            cardsRow.anchorMax = new Vector2(1f, 0.5f);
            cardsRow.pivot = new Vector2(0.5f, 0.5f);
            cardsRow.anchoredPosition = new Vector2(0f, 12f);
            cardsRow.sizeDelta = new Vector2(-36f, 360f);
            var cardsHlg = cardsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            cardsHlg.spacing = 14f;
            cardsHlg.childAlignment = TextAnchor.MiddleCenter;
            cardsHlg.childControlWidth = false;
            cardsHlg.childControlHeight = false;
            cardsHlg.childForceExpandWidth = false;
            cardsHlg.childForceExpandHeight = false;

            int maxSlots = TeammateCardRewardController.MaxCardsPerReveal;
            var cardSlots = new RectTransform[maxSlots];
            var cardViews = new TeammateCardView[maxSlots];
            var cardEffectTexts = new TextMeshProUGUI[maxSlots];

            for (int i = 0; i < maxSlots; i++)
            {
                var slot = CreateUI($"RewardCardSlot_{i + 1}", cardsRow);
                slot.sizeDelta = new Vector2(190f, 326f);
                slot.gameObject.AddComponent<CanvasGroup>();

                var cardRt = BuildReusableCardUI(slot, "CardView", 184f, 268f, out var view);
                cardRt.anchorMin = new Vector2(0.5f, 1f);
                cardRt.anchorMax = new Vector2(0.5f, 1f);
                cardRt.pivot = new Vector2(0.5f, 1f);
                cardRt.anchoredPosition = new Vector2(0f, 0f);

                var effRect = CreateUI("TxtEffect", slot);
                effRect.anchorMin = new Vector2(0f, 0f);
                effRect.anchorMax = new Vector2(1f, 0f);
                effRect.pivot = new Vector2(0.5f, 0f);
                effRect.anchoredPosition = new Vector2(0f, 4f);
                effRect.sizeDelta = new Vector2(0f, 48f);
                var effTmp = MakeTMP(effRect, "Tấn công +18 sát thương", 15f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.92f, 0.96f, 1f, 0.96f));
                effTmp.textWrappingMode = TextWrappingModes.Normal;

                cardSlots[i] = slot;
                cardViews[i] = view;
                cardEffectTexts[i] = effTmp;
            }

            // Single Card Description (shown only when count == 1)
            var descRect = CreateUI("TxtSingleDescription", panel);
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 0f);
            descRect.pivot = new Vector2(0.5f, 0f);
            descRect.anchoredPosition = new Vector2(0f, 114f);
            descRect.sizeDelta = new Vector2(-64f, 54f);
            var txtSingleDesc = MakeTMP(descRect, string.Empty, 16f, FontStyles.Italic, TextAlignmentOptions.Center, new Color(0.82f, 0.88f, 0.94f, 0.90f));
            txtSingleDesc.textWrappingMode = TextWrappingModes.Normal;
            descRect.gameObject.SetActive(false);

            // Claim Button
            var btnClaimRect = CreateUI("BtnClaim", panel);
            btnClaimRect.anchorMin = new Vector2(0.5f, 0f);
            btnClaimRect.anchorMax = new Vector2(0.5f, 0f);
            btnClaimRect.pivot = new Vector2(0.5f, 0f);
            btnClaimRect.anchoredPosition = new Vector2(0f, 34f);
            btnClaimRect.sizeDelta = new Vector2(320f, 64f);
            var claimCg = btnClaimRect.gameObject.AddComponent<CanvasGroup>();
            AssignSliced(btnClaimRect.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.14f, 0.68f, 0.62f, 1f));
            var btnClaim = btnClaimRect.gameObject.AddComponent<Button>();
            var btnClaimLblRect = CreateUI("Label", btnClaimRect);
            StretchFull(btnClaimLblRect);
            var txtClaimLabel = MakeTMP(btnClaimLblRect, "NHẬN THẺ", 24f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            borderOuter.SetAsLastSibling();
            borderInner.SetAsLastSibling();

            var controller = root.GetComponent<TeammateCardRewardController>();
            controller.SetEditorReferences(
                panel,
                txtTitle,
                txtSubtitle,
                cardsHlg,
                cardSlots,
                cardViews,
                cardEffectTexts,
                txtSingleDesc,
                btnClaim,
                txtClaimLabel,
                claimCg);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, RewardPrefabPath);
            Object.DestroyImmediate(root);

            var screenRef = GetOrCreateAsset<ScreenReference>(RewardScreenRefPath);
            screenRef.ScreenPrefab = prefab;
            EditorUtility.SetDirty(screenRef);
        }

        private static RectTransform BuildReusableCardUI(RectTransform parent, string name, float width, float height, out TeammateCardView view)
        {
            var card = CreateUI(name, parent);
            card.sizeDelta = new Vector2(width, height);
            var bgImg = card.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.14f, 0.90f);

            // Upper portrait window (matches card_frame_attack / card_frame_support window)
            var mask = CreateUI("PortraitMask", card);
            mask.anchorMin = new Vector2(0.11f, 0.38f);
            mask.anchorMax = new Vector2(0.89f, 0.91f);
            mask.offsetMin = Vector2.zero;
            mask.offsetMax = Vector2.zero;
            mask.gameObject.AddComponent<RectMask2D>();

            var portrait = CreateUI("Portrait", mask);
            StretchFull(portrait);
            var pImg = portrait.gameObject.AddComponent<Image>();
            pImg.preserveAspect = false;
            var fitter = portrait.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            // Type badge (top-right corner of portrait)
            var badge = CreateUI("TypeBadge", card);
            badge.anchorMin = new Vector2(1f, 1f);
            badge.anchorMax = new Vector2(1f, 1f);
            badge.pivot = new Vector2(1f, 1f);
            badge.anchoredPosition = new Vector2(-10f, -10f);
            badge.sizeDelta = new Vector2(46f, 20f);
            var badgeBg = badge.gameObject.AddComponent<Image>();
            AssignSliced(badgeBg, PanelFillPath, new Color(0.84f, 0.16f, 0.20f, 1f), 2.5f);
            var badgeTxtRt = CreateUI("Label", badge);
            StretchFull(badgeTxtRt);
            var badgeTmp = MakeTMP(badgeTxtRt, "CÔNG", 11f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Lower nameplate box
            var nameRt = CreateUI("TxtName", card);
            nameRt.anchorMin = new Vector2(0.08f, 0.18f);
            nameRt.anchorMax = new Vector2(0.92f, 0.34f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            var nameTmp = MakeTMP(nameRt, "Đồng đội", 14f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            nameTmp.enableAutoSizing = true;
            nameTmp.fontSizeMin = 10f;
            nameTmp.fontSizeMax = 14f;

            var statRt = CreateUI("TxtStat", card);
            statRt.anchorMin = new Vector2(0.08f, 0.06f);
            statRt.anchorMax = new Vector2(0.92f, 0.18f);
            statRt.offsetMin = Vector2.zero;
            statRt.offsetMax = Vector2.zero;
            var statTmp = MakeTMP(statRt, "+18 ST", 13f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.42f, 1f));

            // Optional outer border (for empty slots)
            var border = CreateUI("Border", card);
            StretchFull(border);
            var borderImg = border.gameObject.AddComponent<Image>();
            AssignSliced(borderImg, BorderOuterPath, new Color(0.4f, 0.45f, 0.52f, 0.6f), 2.2f);
            borderImg.raycastTarget = false;

            view = card.gameObject.AddComponent<TeammateCardView>();
            view.SetReferences(bgImg, borderImg, pImg, fitter, nameTmp, statTmp, badgeBg, badgeTmp, null);
            return card;
        }

        private static Button CreateFilterTab(RectTransform parent, string name, string label)
        {
            var rt = CreateUI(name, parent);
            AssignSliced(rt.gameObject.AddComponent<Image>(), PanelFillPath, new Color(0.08f, 0.11f, 0.16f, 0.88f));
            var btn = rt.gameObject.AddComponent<Button>();
            var lblRt = CreateUI("Label", rt);
            StretchFull(lblRt);
            MakeTMP(lblRt, label, 16f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            return btn;
        }

        private static void WireScenes()
        {
            var database = AssetDatabase.LoadAssetAtPath<TeammateCardDatabaseSO>(DatabasePath);
            var weaponCatalog = AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(WeaponCatalogPath);
            var outfitCatalog = AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(OutfitCatalogPath);

            // Wire SOManager prefab first so all prefab instances inherit it
            var smPrefabPath = "Assets/Project/Prefabs/Prefabs/SOManager.prefab";
            var smPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(smPrefabPath);
            if (smPrefab != null)
            {
                var smComp = smPrefab.GetComponent<SOManager>();
                if (smComp != null)
                {
                    var so = new SerializedObject(smComp);
                    SetProp(so, "m_weaponCatalog", weaponCatalog);
                    SetProp(so, "m_outfitCatalog", outfitCatalog);
                    SetProp(so, "m_teammateDatabase", database);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(smComp);
                    PrefabUtility.SavePrefabAsset(smPrefab);
                }
            }

            string originalScenePath = EditorSceneManager.GetActiveScene().path;
            var scenesToWire = new HashSet<string>
            {
                "Assets/Project/Scenes/Main/Main.unity",
                "Assets/Project/Scenes/Intro.unity",
                "Assets/Project/Scenes/Story/StoryIntro.unity",
                "Assets/Project/Scenes/Home/Home.unity",
                "Assets/Project/Scenes/Game.unity"
            };
            foreach (var bs in EditorBuildSettings.scenes)
            {
                if (!string.IsNullOrEmpty(bs.path)) scenesToWire.Add(bs.path);
            }

            foreach (string scenePath in scenesToWire)
            {
                if (!File.Exists(scenePath)) continue;
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool dirty = false;

                // Reload asset references after OpenScene(Single)
                database = AssetDatabase.LoadAssetAtPath<TeammateCardDatabaseSO>(DatabasePath);
                weaponCatalog = AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(WeaponCatalogPath);
                outfitCatalog = AssetDatabase.LoadAssetAtPath<ItemCatalogSO>(OutfitCatalogPath);

                var soManager = Object.FindFirstObjectByType<SOManager>(FindObjectsInactive.Include);
                if (soManager != null)
                {
                    if (PrefabUtility.GetPrefabInstanceStatus(soManager) == PrefabInstanceStatus.Connected)
                    {
                        PrefabUtility.RevertObjectOverride(soManager, InteractionMode.AutomatedAction);
                    }
                    var so = new SerializedObject(soManager);
                    SetProp(so, "m_weaponCatalog", weaponCatalog);
                    SetProp(so, "m_outfitCatalog", outfitCatalog);
                    SetProp(so, "m_teammateDatabase", database);
                    so.ApplyModifiedProperties();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(soManager);
                    EditorUtility.SetDirty(soManager);
                    dirty = true;
                }

                var homeCtrl = Object.FindFirstObjectByType<HomeController>(FindObjectsInactive.Include);
                if (homeCtrl != null)
                {
                    var so = new SerializedObject(homeCtrl);
                    SetProp(so, "m_teammateDatabase", database);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(homeCtrl);
                    EnsureHomeTeammatesButton(homeCtrl);
                    dirty = true;
                }

                if (scenePath == "Assets/Project/Scenes/Game.unity")
                {
                    GameUILayoutBuilder.ApplyBattleHudLayout();
                    dirty = true;
                }

                if (dirty)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }

        private static void EnsureHomeTeammatesButton(HomeController homeCtrl)
        {
            if (homeCtrl == null) return;
            var btnOutfit = GameObject.Find("BtnOutfit");
            if (btnOutfit == null || btnOutfit.transform.parent == null) return;

            Transform parent = btnOutfit.transform.parent;
            Transform existing = parent.Find("BtnTeammates");
            GameObject btnGo = existing != null ? existing.gameObject : Object.Instantiate(btnOutfit, parent);
            btnGo.name = "BtnTeammates";
            btnGo.SetActive(true);

            var rt = btnGo.GetComponent<RectTransform>();
            var outfitRt = btnOutfit.GetComponent<RectTransform>();
            if (rt != null && outfitRt != null)
            {
                rt.anchoredPosition = outfitRt.anchoredPosition + new Vector2(0f, -140f);
                rt.sizeDelta = outfitRt.sizeDelta;
            }

            // Give BtnTeammates a distinct gold/amber neon style and crisp "ĐỘI" icon label
            Color goldRing = new Color(0.96f, 0.72f, 0.18f, 1f);
            foreach (var img in btnGo.GetComponentsInChildren<Image>(true))
            {
                if (img.name.ToLowerInvariant().Contains("icon"))
                {
                    img.enabled = false; // Hide cloned outfit jacket icon
                }
                else if (img.name.ToLowerInvariant().Contains("border") || img.name.ToLowerInvariant().Contains("ring"))
                {
                    img.color = goldRing;
                }
            }

            var labelRt = btnGo.transform.Find("TeammateLabel") as RectTransform;
            if (labelRt == null)
            {
                labelRt = CreateUI("TeammateLabel", btnGo.transform);
                StretchFull(labelRt, 10f);
            }
            var existingTmp = labelRt.GetComponent<TextMeshProUGUI>();
            if (existingTmp == null)
            {
                MakeTMP(labelRt, "ĐỘI\nHÌNH", 22f, FontStyles.Bold, TextAlignmentOptions.Center, goldRing);
            }
            else
            {
                existingTmp.text = "ĐỘI\nHÌNH";
                existingTmp.color = goldRing;
            }

            var btn = btnGo.GetComponent<Button>();
            if (btn != null)
            {
                while (btn.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                }
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, homeCtrl.OnClickTeammates);
                EditorUtility.SetDirty(btn);
            }
        }

        private static void SetProp(SerializedObject so, string name, Object value)
        {
            var prop = so.FindProperty(name);
            if (prop != null) prop.objectReferenceValue = value;
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static void EnsureFolder(string assetPath)
        {
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

        private static RectTransform CreateUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.gameObject.layer;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static void StretchFull(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void SetTop(RectTransform rt, float topOffset, float height, float sideMargin)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -topOffset);
            rt.sizeDelta = new Vector2(-sideMargin * 2f, height);
        }

        private static void AssignSliced(Image img, string spritePath, Color color, float ppu = 1f)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = ppu;
            }
            img.color = color;
        }

        private static TextMeshProUGUI MakeTMP(RectTransform rt, string text, float size, FontStyles style, TextAlignmentOptions align, Color color)
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
    }
}
