using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.Editor
{
    public static class GameUILayoutBuilder
    {
        private const string GameScenePath = "Assets/Project/Scenes/Game.unity";
        private const string FontPath = "Assets/Project/Font/NotoSans/NotoSans-Bold SDF.asset";

        private const string PanelFillPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_panel_fill.png";
        private const string BorderOuterPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_outer.png";
        private const string BorderInnerPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_border_inner.png";
        private const string LabelFillPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_label_fill.png";
        private const string LabelBorderPath = "Assets/Project/Sprites/UI/Neon90s/ui_neon_label_border.png";
        private const string IconWeaponPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_weapon.png";
        private const string IconOutfitPath = "Assets/Project/Sprites/UI/Neon90s/icon_neon_outfit.png";

        // Canvas reference resolution is 720 x 1600 (scale 0.01 => 7.2 x 16.0 world units).
        private const float AdBannerHeightPx = 120f;
        private const float FooterHeightPx = 140f;
        private const float HudPanelWidthPx = 688f;
        private const float HudPanelHeightPx = 294f;
        private const float TopPanelMarginPx = 32f;
        private const float PlayerPanelBottomPx = AdBannerHeightPx + FooterHeightPx + 6f; // 266px from bottom => Y = -534 to -240

        // 7x7 grid with ItemSize = (0.9, 0.9) => 6.3 x 6.3 world units => 630 x 630 px inside 664 x 664 BoardFrame.
        private const float BoardFrameSizePx = 664f;
        private const float BoardCenterYPx = 118f;
        private const float BoardWorldCenterY = 1.18f;

        private struct HudTheme
        {
            public Color PanelBg;
            public Color BorderOuter;
            public Color BorderInner;
            public Color LevelBg;
            public Color LevelBorder;
            public string HealthFillSprite;
            public string DefaultName;
            public string DefaultLevel;
            public string DefaultHealthText;
            public string DefaultManaText;
            public string DefaultAvatarSprite;
            public string[] TeammateSprites;
        }

        private struct HudRefs
        {
            public Image HealthFill;
            public Image ManaFill;
            public Image AttackTimerFill;
            public TextMeshProUGUI TxtName;
            public TextMeshProUGUI TxtLevel;
            public TextMeshProUGUI TxtHealth;
            public TextMeshProUGUI TxtMana;
            public RectTransform AvatarContainer;
        }

        [MenuItem("NTGH/UI/Apply Game Layout Battle HUD (7x7)")]
        public static void ApplyBattleHudLayout()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != GameScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            }

            var gc = Object.FindFirstObjectByType<GameController>();
            if (gc == null)
            {
                Debug.LogError("[GameUILayoutBuilder] GameController not found in Game.unity.");
                return;
            }

            var canvasRect = gc.transform.Find("Canvas") as RectTransform;
            if (canvasRect == null)
            {
                Debug.LogError("[GameUILayoutBuilder] GameController/Canvas not found in Game.unity.");
                return;
            }

            var header = canvasRect.Find("Header") as RectTransform;
            var body = canvasRect.Find("Body") as RectTransform;
            var footer = canvasRect.Find("Footer") as RectTransform;

            // Keep Header, Exp, ScoreText & DebugText intact for script references, but visually clean
            if (header != null) header.gameObject.SetActive(false);
            var expObj = canvasRect.Find("Exp");
            if (expObj != null) expObj.gameObject.SetActive(false);
            HideLegacyText(canvasRect.Find("ScoreText"));
            HideLegacyText(canvasRect.Find("DebugText"));

            ConfigureWeaponObjects(canvasRect);
            ConfigureBackground(body);
            ConfigureBoardFrame(body);

            // Hide legacy floating bar containers (keep inactive so no stray visuals remain)
            HideLegacyBarContainers(body);

            var enemyTheme = new HudTheme
            {
                PanelBg = new Color(0.10f, 0.03f, 0.05f, 0.90f),
                BorderOuter = new Color(0.84f, 0.16f, 0.22f, 1f),
                BorderInner = new Color(1.00f, 0.36f, 0.42f, 0.35f),
                LevelBg = new Color(0.32f, 0.06f, 0.09f, 0.95f),
                LevelBorder = new Color(0.82f, 0.20f, 0.25f, 0.90f),
                HealthFillSprite = "Assets/Project/Textures/bar_fill_red.png",
                DefaultName = "Hồng Côn",
                DefaultLevel = "Lv.100",
                DefaultHealthText = "100.000 / 100.000",
                DefaultManaText = "0 / 100",
                DefaultAvatarSprite = "Assets/Project/Textures/temp_enemy.png",
                TeammateSprites = new[]
                {
                    "Assets/Project/Textures/Characters/QuaDen.png",
                    "Assets/Project/Textures/Characters/KimMaoHo.png",
                    "Assets/Project/Textures/Characters/LapHoa.png",
                    "Assets/Project/Textures/Characters/LanhDienHo.png"
                }
            };

            var playerTheme = new HudTheme
            {
                PanelBg = new Color(0.03f, 0.09f, 0.11f, 0.90f),
                BorderOuter = new Color(0.12f, 0.78f, 0.82f, 1f),
                BorderInner = new Color(0.45f, 0.94f, 0.98f, 0.35f),
                LevelBg = new Color(0.05f, 0.25f, 0.28f, 0.95f),
                LevelBorder = new Color(0.18f, 0.76f, 0.80f, 0.90f),
                HealthFillSprite = "Assets/Project/Textures/bar_fill_green.png",
                DefaultName = "Lương Phi",
                DefaultLevel = "Lv.1",
                DefaultHealthText = "12.800 / 12.800",
                DefaultManaText = "0 / 100",
                DefaultAvatarSprite = "Assets/Project/Textures/temp_player.png",
                TeammateSprites = new[]
                {
                    "Assets/Project/Textures/Characters/TranHaoNam.png",
                    "Assets/Project/Textures/Characters/ThaiTu.png",
                    "Assets/Project/Textures/Characters/DieuDuong.png",
                    "Assets/Project/Textures/Characters/a_dung_l01_giao_bao.png"
                }
            };

            var enemyRefs = BuildHudPanel(body, "EnemyPanel", isTop: true, enemyTheme);
            var playerRefs = BuildHudPanel(body, "PlayerPanel", isTop: false, playerTheme);

            // Re-parent existing Com (Enemy avatar) and Player (Player avatar) into their AvatarContainers
            var enemyAvatarImg = AttachExistingAvatar(body, "Com", enemyRefs.AvatarContainer, enemyTheme.DefaultAvatarSprite);
            var playerAvatarImg = AttachExistingAvatar(body, "Player", playerRefs.AvatarContainer, playerTheme.DefaultAvatarSprite);

            // Wire GameController serialized references cleanly
            WireGameController(gc, enemyRefs, enemyAvatarImg, playerRefs, playerAvatarImg);

            ConfigureFooterBoosters(footer);
            ConfigureAdBannerZone(canvasRect);
            ConfigureShapesManager();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[GameUILayoutBuilder] Applied Battle HUD 7x7 Layout and saved Game.unity.");
        }

        [MenuItem("NTGH/UI/Apply Game Layout Concept 3 Final")]
        public static void ApplyConcept3FinalLayout()
        {
            ApplyBattleHudLayout();
        }

        private static void ConfigureBackground(RectTransform body)
        {
            if (body == null) return;

            var bodyImg = body.GetComponent<Image>();
            if (bodyImg != null)
            {
                bodyImg.color = new Color(0f, 0f, 0f, 0f);
            }

            var bgr = body.Find("Bgr") as RectTransform;
            if (bgr != null)
            {
                bgr.SetAsFirstSibling();
                AssignSprite(bgr, "Assets/Project/Scenes/Sprites/Bgr/Bgr_HongKong_Battle_Alley.jpg");
            }
        }

        private static void ConfigureWeaponObjects(RectTransform canvasRect)
        {
            ConfigureSingleWeapon(canvasRect.Find("Knife_Enemy") as RectTransform, new Vector2(-246f, 620f));
            ConfigureSingleWeapon(canvasRect.Find("Knife_Player") as RectTransform, new Vector2(-246f, -387f));
        }

        private static void ConfigureSingleWeapon(RectTransform weaponRoot, Vector2 anchoredPos)
        {
            if (weaponRoot == null) return;
            weaponRoot.anchoredPosition = anchoredPos;

            var knifeChild = weaponRoot.Find("Knife (1)");
            if (knifeChild != null)
            {
                AssignSprite(knifeChild, "Assets/Project/Scenes/Sprites/Katana.png");
                knifeChild.gameObject.SetActive(false);
            }
        }

        private static void HideLegacyBarContainers(RectTransform body)
        {
            if (body == null) return;
            for (int i = 0; i < body.childCount; i++)
            {
                var child = body.GetChild(i);
                if (child.name == "Enemy" || (child.name == "Player" && child.GetComponent<Image>() == null))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static void ConfigureBoardFrame(RectTransform body)
        {
            if (body == null) return;

            var boardFrame = body.Find("BoardFrame") as RectTransform;
            if (boardFrame == null)
            {
                boardFrame = CreateUIObject("BoardFrame", body);
                boardFrame.gameObject.AddComponent<Image>();
            }

            boardFrame.anchorMin = new Vector2(0.5f, 0.5f);
            boardFrame.anchorMax = new Vector2(0.5f, 0.5f);
            boardFrame.pivot = new Vector2(0.5f, 0.5f);
            boardFrame.anchoredPosition = new Vector2(0f, BoardCenterYPx);
            boardFrame.sizeDelta = new Vector2(BoardFrameSizePx, BoardFrameSizePx);

            var outline = boardFrame.GetComponent<Outline>();
            if (outline != null) Object.DestroyImmediate(outline);

            var bgImg = boardFrame.GetComponent<Image>();
            AssignSlicedSprite(bgImg, PanelFillPath, new Color(0.04f, 0.06f, 0.09f, 0.84f));

            // Metallic outer & inner frame overlays
            var borderOuter = GetOrCreateChild(boardFrame, "BorderOuter");
            StretchFull(borderOuter);
            AssignSlicedSprite(GetOrAddComponent<Image>(borderOuter), BorderOuterPath, new Color(0.36f, 0.42f, 0.50f, 0.95f));
            borderOuter.GetComponent<Image>().raycastTarget = false;

            var borderInner = GetOrCreateChild(boardFrame, "BorderInner");
            StretchFull(borderInner, 6f);
            AssignSlicedSprite(GetOrAddComponent<Image>(borderInner), BorderInnerPath, new Color(0.68f, 0.76f, 0.86f, 0.35f));
            borderInner.GetComponent<Image>().raycastTarget = false;

            // 7x7 Checkerboard Grid inside BoardFrame (630 x 630 px => 90x90 cells)
            var gridRect = GetOrCreateChild(boardFrame, "BoardGrid");
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = Vector2.zero;
            gridRect.sizeDelta = new Vector2(630f, 630f);

            var grid = GetOrAddComponent<GridLayoutGroup>(gridRect);
            grid.cellSize = new Vector2(90f, 90f);
            grid.spacing = Vector2.zero;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 7;
            grid.childAlignment = TextAnchor.MiddleCenter;

            // Clear and rebuild 49 cells cleanly
            for (int i = gridRect.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(gridRect.GetChild(i).gameObject);
            }

            Color cellA = new Color(0.09f, 0.12f, 0.17f, 0.72f);
            Color cellB = new Color(0.05f, 0.07f, 0.11f, 0.72f);
            Color cellLine = new Color(0.24f, 0.30f, 0.38f, 0.32f);

            for (int row = 0; row < 7; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    var cell = CreateUIObject($"Cell_{row}_{col}", gridRect);
                    var cellImg = cell.gameObject.AddComponent<Image>();
                    cellImg.color = (row + col) % 2 == 0 ? cellA : cellB;
                    cellImg.raycastTarget = false;

                    var cellOutline = cell.gameObject.AddComponent<Outline>();
                    cellOutline.effectColor = cellLine;
                    cellOutline.effectDistance = new Vector2(1f, -1f);
                }
            }

            // Ensure borders render on top of grid cells
            gridRect.SetAsFirstSibling();
            borderOuter.SetAsLastSibling();
            borderInner.SetAsLastSibling();
        }

        private static HudRefs BuildHudPanel(RectTransform body, string panelName, bool isTop, HudTheme theme)
        {
            var panel = GetOrCreateChild(body, panelName);
            if (isTop)
            {
                panel.anchorMin = new Vector2(0.5f, 1f);
                panel.anchorMax = new Vector2(0.5f, 1f);
                panel.pivot = new Vector2(0.5f, 1f);
                panel.anchoredPosition = new Vector2(0f, -TopPanelMarginPx);
            }
            else
            {
                panel.anchorMin = new Vector2(0.5f, 0f);
                panel.anchorMax = new Vector2(0.5f, 0f);
                panel.pivot = new Vector2(0.5f, 0f);
                panel.anchoredPosition = new Vector2(0f, PlayerPanelBottomPx);
            }
            panel.sizeDelta = new Vector2(HudPanelWidthPx, HudPanelHeightPx);

            // Panel background & metallic neon borders
            var bgImg = GetOrAddComponent<Image>(panel);
            AssignSlicedSprite(bgImg, PanelFillPath, theme.PanelBg);

            var borderOuter = GetOrCreateChild(panel, "BorderOuter");
            StretchFull(borderOuter);
            var outerImg = GetOrAddComponent<Image>(borderOuter);
            AssignSlicedSprite(outerImg, BorderOuterPath, theme.BorderOuter);
            outerImg.raycastTarget = false;

            var borderInner = GetOrCreateChild(panel, "BorderInner");
            StretchFull(borderInner, 5f);
            var innerImg = GetOrAddComponent<Image>(borderInner);
            AssignSlicedSprite(innerImg, BorderInnerPath, theme.BorderInner);
            innerImg.raycastTarget = false;

            Color goldBorder = new Color(0.85f, 0.67f, 0.28f, 1f);
            Color slotBg = new Color(0.07f, 0.08f, 0.11f, 0.95f);

            // --- LEFT COLUMN: Leader Avatar + (optional Enemy Attack Timer) + 2 Equipment Slots ---
            var avatarFrame = GetOrCreateChild(panel, "AvatarFrame");
            float avatarHeight = isTop ? 150f : 162f;
            SetTopLeft(avatarFrame, 18f, -16f, 162f, avatarHeight);
            AssignSlicedSprite(GetOrAddComponent<Image>(avatarFrame), PanelFillPath, slotBg);

            var avatarMask = GetOrCreateChild(avatarFrame, "AvatarMask");
            StretchFull(avatarMask, 6f);
            GetOrAddComponent<RectMask2D>(avatarMask);

            var avatarBorder = GetOrCreateChild(avatarFrame, "GoldBorder");
            StretchFull(avatarBorder);
            var avatarBorderImg = GetOrAddComponent<Image>(avatarBorder);
            AssignSlicedSprite(avatarBorderImg, BorderOuterPath, goldBorder, 1.8f);
            avatarBorderImg.raycastTarget = false;
            avatarBorder.SetAsLastSibling();

            // Dedicated Enemy Attack Countdown Timer bar (separate from NỘ/MP bar)
            Image attackTimerFillImg = null;
            if (isTop)
            {
                var timerBar = GetOrCreateChild(panel, "AttackTimerBar");
                SetTopLeft(timerBar, 18f, -172f, 162f, 14f);
                var timerBgImg = GetOrAddComponent<Image>(timerBar);
                AssignSprite(timerBar, "Assets/Project/Textures/bar_frame.png");
                timerBgImg.color = new Color(0.16f, 0.14f, 0.16f, 0.95f);

                var timerFillRect = GetOrCreateChild(timerBar, "Fill");
                StretchFull(timerFillRect, 2f);
                attackTimerFillImg = GetOrAddComponent<Image>(timerFillRect);
                AssignSprite(timerFillRect, "Assets/Project/Textures/bar_fill_orange.png");
                attackTimerFillImg.color = Color.white;
                attackTimerFillImg.type = Image.Type.Filled;
                attackTimerFillImg.fillMethod = Image.FillMethod.Horizontal;
                attackTimerFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
                attackTimerFillImg.fillAmount = 1f;
            }
            else
            {
                var oldTimer = panel.Find("AttackTimerBar");
                if (oldTimer != null) Object.DestroyImmediate(oldTimer.gameObject);
            }

            // 2 Equipment Slots below Avatar
            var equipRow = GetOrCreateChild(panel, "EquipSlots");
            float equipY = isTop ? -192f : -190f;
            float equipSize = isTop ? 76f : 77f;
            SetTopLeft(equipRow, 18f, equipY, 162f, equipSize + 4f);
            BuildEquipSlot(equipRow, "Slot_Weapon", 0f, equipSize, equipSize, IconWeaponPath, goldBorder, slotBg);
            BuildEquipSlot(equipRow, "Slot_Outfit", 162f - equipSize, equipSize, equipSize, IconOutfitPath, goldBorder, slotBg);

            // --- RIGHT COLUMN: Name/Lv/HP + MP + 4 Teammate Cards ---
            var rightCol = GetOrCreateChild(panel, "RightColumn");
            SetTopLeft(rightCol, 194f, -16f, 476f, 262f);

            // Row 1: Name + Level Pill + HP Bar
            var statsTop = GetOrCreateChild(rightCol, "StatsTop");
            SetTopLeft(statsTop, 0f, 0f, 476f, 34f);

            var txtNameRect = GetOrCreateChild(statsTop, "TxtName");
            SetTopLeft(txtNameRect, 0f, 0f, 134f, 34f);
            var txtName = ConfigureTMP(txtNameRect, theme.DefaultName, 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);
            txtName.enableAutoSizing = true;
            txtName.fontSizeMin = 15f;
            txtName.fontSizeMax = 22f;

            var levelBadge = GetOrCreateChild(statsTop, "LevelBadge");
            SetTopLeft(levelBadge, 136f, -4f, 76f, 26f);
            AssignSlicedSprite(GetOrAddComponent<Image>(levelBadge), LabelFillPath, theme.LevelBg);
            var levelBorder = GetOrCreateChild(levelBadge, "Border");
            StretchFull(levelBorder);
            var lvlBorderImg = GetOrAddComponent<Image>(levelBorder);
            AssignSlicedSprite(lvlBorderImg, LabelBorderPath, theme.LevelBorder);
            lvlBorderImg.raycastTarget = false;
            var txtLevelRect = GetOrCreateChild(levelBadge, "TxtLevel");
            StretchFull(txtLevelRect);
            var txtLevel = ConfigureTMP(txtLevelRect, theme.DefaultLevel, 15f, FontStyles.Bold | FontStyles.Italic, TextAlignmentOptions.Center, Color.white);

            // Health Bar
            var hpBar = GetOrCreateChild(statsTop, "HealthBar");
            SetTopLeft(hpBar, 220f, -4f, 256f, 26f);
            var hpFrameImg = GetOrAddComponent<Image>(hpBar);
            AssignSprite(hpBar, "Assets/Project/Textures/bar_frame.png");
            hpFrameImg.color = new Color(0.18f, 0.20f, 0.24f, 0.95f);

            var hpFillRect = GetOrCreateChild(hpBar, "Fill");
            StretchFull(hpFillRect, 2f);
            var hpFillImg = GetOrAddComponent<Image>(hpFillRect);
            AssignSprite(hpFillRect, theme.HealthFillSprite);
            hpFillImg.color = Color.white;
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpFillImg.fillAmount = 1f;

            var txtHpRect = GetOrCreateChild(hpBar, "TxtHealth");
            StretchFull(txtHpRect);
            txtHpRect.offsetMin = new Vector2(8f, 0f);
            txtHpRect.offsetMax = new Vector2(-10f, 0f);
            var txtHealth = ConfigureTMP(txtHpRect, theme.DefaultHealthText, 15f, FontStyles.Bold | FontStyles.Italic, TextAlignmentOptions.MidlineRight, Color.white);
            AddTextShadow(txtHpRect);

            // Row 2: NỘ (Skill Rage) Label + Cyan Bar
            var manaRow = GetOrCreateChild(rightCol, "ManaRow");
            SetTopLeft(manaRow, 0f, -38f, 476f, 26f);

            var mpLabelRect = GetOrCreateChild(manaRow, "TxtMPLabel");
            SetTopLeft(mpLabelRect, 0f, 0f, 44f, 26f);
            ConfigureTMP(mpLabelRect, "NỘ", 17f, FontStyles.Bold | FontStyles.Italic, TextAlignmentOptions.MidlineLeft, Color.white);

            var mpBar = GetOrCreateChild(manaRow, "ManaBar");
            SetTopLeft(mpBar, 48f, -2f, 428f, 22f);
            var mpFrameImg = GetOrAddComponent<Image>(mpBar);
            AssignSprite(mpBar, "Assets/Project/Textures/bar_frame.png");
            mpFrameImg.color = new Color(0.18f, 0.20f, 0.24f, 0.95f);

            var mpFillRect = GetOrCreateChild(mpBar, "Fill");
            StretchFull(mpFillRect, 2f);
            var mpFillImg = GetOrAddComponent<Image>(mpFillRect);
            AssignSprite(mpFillRect, "Assets/Project/Textures/bar_fill_blue.png");
            mpFillImg.color = Color.white;
            mpFillImg.type = Image.Type.Filled;
            mpFillImg.fillMethod = Image.FillMethod.Horizontal;
            mpFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            mpFillImg.fillAmount = 0f;

            var txtMpRect = GetOrCreateChild(mpBar, "TxtMana");
            StretchFull(txtMpRect);
            txtMpRect.offsetMin = new Vector2(8f, 0f);
            txtMpRect.offsetMax = new Vector2(-10f, 0f);
            var txtMana = ConfigureTMP(txtMpRect, theme.DefaultManaText, 14f, FontStyles.Bold | FontStyles.Italic, TextAlignmentOptions.MidlineRight, Color.white);
            AddTextShadow(txtMpRect);

            // Row 3: 4 Teammate Cards
            var teamRow = GetOrCreateChild(rightCol, "TeammatesRow");
            SetTopLeft(teamRow, 0f, -72f, 476f, 186f);
            var hlg = GetOrAddComponent<HorizontalLayoutGroup>(teamRow);
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            for (int i = 0; i < 4; i++)
            {
                string spritePath = (theme.TeammateSprites != null && i < theme.TeammateSprites.Length)
                    ? theme.TeammateSprites[i]
                    : null;
                BuildTeammateCard(teamRow, $"Teammate_{i + 1}", $"Đồng đội {i + 1}", spritePath, goldBorder, slotBg);
            }

            // Ensure outer borders render on top
            borderOuter.SetAsLastSibling();
            borderInner.SetAsLastSibling();

            return new HudRefs
            {
                HealthFill = hpFillImg,
                ManaFill = mpFillImg,
                AttackTimerFill = attackTimerFillImg,
                TxtName = txtName,
                TxtLevel = txtLevel,
                TxtHealth = txtHealth,
                TxtMana = txtMana,
                AvatarContainer = avatarMask
            };
        }

        private static void BuildEquipSlot(
            RectTransform parent,
            string slotName,
            float x,
            float width,
            float height,
            string iconPath,
            Color borderColor,
            Color bgColor)
        {
            var slot = GetOrCreateChild(parent, slotName);
            SetTopLeft(slot, x, 0f, width, height);
            AssignSlicedSprite(GetOrAddComponent<Image>(slot), PanelFillPath, bgColor);

            var icon = GetOrCreateChild(slot, "Icon");
            StretchFull(icon, 10f);
            var iconImg = GetOrAddComponent<Image>(icon);
            AssignSprite(icon, iconPath);
            iconImg.preserveAspect = true;
            iconImg.color = new Color(0.92f, 0.88f, 0.80f, 0.95f);

            var border = GetOrCreateChild(slot, "GoldBorder");
            StretchFull(border);
            var borderImg = GetOrAddComponent<Image>(border);
            AssignSlicedSprite(borderImg, BorderOuterPath, borderColor, 2.2f);
            borderImg.raycastTarget = false;
            border.SetAsLastSibling();
        }

        private static void BuildTeammateCard(
            RectTransform parent,
            string cardName,
            string label,
            string portraitPath,
            Color borderColor,
            Color bgColor)
        {
            var card = GetOrCreateChild(parent, cardName);
            card.sizeDelta = new Vector2(113f, 184f);
            AssignSlicedSprite(GetOrAddComponent<Image>(card), PanelFillPath, bgColor, 2.2f);

            // Portrait mask
            var portraitMask = GetOrCreateChild(card, "PortraitMask");
            portraitMask.anchorMin = new Vector2(0f, 1f);
            portraitMask.anchorMax = new Vector2(1f, 1f);
            portraitMask.pivot = new Vector2(0.5f, 1f);
            portraitMask.anchoredPosition = new Vector2(0f, -6f);
            portraitMask.sizeDelta = new Vector2(-12f, 122f);
            GetOrAddComponent<RectMask2D>(portraitMask);

            var portrait = GetOrCreateChild(portraitMask, "Portrait");
            StretchFull(portrait);
            var portraitImg = GetOrAddComponent<Image>(portrait);
            if (!string.IsNullOrEmpty(portraitPath))
            {
                AssignSprite(portrait, portraitPath);
            }
            portraitImg.color = Color.white;
            portraitImg.preserveAspect = false;
            var fitter = GetOrAddComponent<AspectRatioFitter>(portrait);
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (portraitImg.sprite != null)
            {
                fitter.aspectRatio = portraitImg.sprite.rect.width / portraitImg.sprite.rect.height;
            }

            // Label bar
            var nameRect = GetOrCreateChild(card, "TxtName");
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, 26f);
            nameRect.sizeDelta = new Vector2(-12f, 24f);
            var tmp = ConfigureTMP(nameRect, label, 13.5f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10f;
            tmp.fontSizeMax = 13.5f;

            // Mini cyan charge bar at bottom
            var barBg = GetOrCreateChild(card, "MiniBarBg");
            barBg.anchorMin = new Vector2(0f, 0f);
            barBg.anchorMax = new Vector2(1f, 0f);
            barBg.pivot = new Vector2(0.5f, 0f);
            barBg.anchoredPosition = new Vector2(0f, 12f);
            barBg.sizeDelta = new Vector2(-20f, 10f);
            var barBgImg = GetOrAddComponent<Image>(barBg);
            AssignSprite(barBg, "Assets/Project/Textures/bar_frame.png");
            barBgImg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

            var barFill = GetOrCreateChild(barBg, "Fill");
            StretchFull(barFill, 1f);
            var barFillImg = GetOrAddComponent<Image>(barFill);
            AssignSprite(barFill, "Assets/Project/Textures/bar_fill_blue.png");
            barFillImg.color = Color.white;
            barFillImg.type = Image.Type.Filled;
            barFillImg.fillMethod = Image.FillMethod.Horizontal;
            barFillImg.fillAmount = 1f;

            // Gold border on top
            var border = GetOrCreateChild(card, "GoldBorder");
            StretchFull(border);
            var borderImg = GetOrAddComponent<Image>(border);
            AssignSlicedSprite(borderImg, BorderOuterPath, borderColor, 2.2f);
            borderImg.raycastTarget = false;
            border.SetAsLastSibling();
        }

        private static Image AttachExistingAvatar(
            RectTransform body,
            string avatarName,
            RectTransform avatarMaskContainer,
            string defaultSpritePath)
        {
            // Find existing avatar either directly under Body or already inside AvatarMask
            Transform existing = avatarMaskContainer.Find(avatarName);
            if (existing == null && body != null)
            {
                for (int i = 0; i < body.childCount; i++)
                {
                    var c = body.GetChild(i);
                    if (c.name == avatarName && c.GetComponent<Image>() != null)
                    {
                        existing = c;
                        break;
                    }
                }
            }

            RectTransform rt;
            if (existing != null)
            {
                rt = existing as RectTransform;
                rt.SetParent(avatarMaskContainer, false);
            }
            else
            {
                rt = CreateUIObject(avatarName, avatarMaskContainer);
                rt.gameObject.AddComponent<Image>();
            }

            // Hide legacy floating rank text child on Player if present
            var legacyRank = rt.Find("Text (TMP)");
            if (legacyRank != null) legacyRank.gameObject.SetActive(false);

            StretchFull(rt);
            var img = rt.GetComponent<Image>();
            AssignSprite(rt, defaultSpritePath);
            img.color = Color.white;
            img.preserveAspect = false;

            var fitter = GetOrAddComponent<AspectRatioFitter>(rt);
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (img.sprite != null)
            {
                fitter.aspectRatio = img.sprite.rect.width / img.sprite.rect.height;
            }

            return img;
        }

        private static void WireGameController(
            GameController gc,
            HudRefs enemyRefs,
            Image enemyAvatar,
            HudRefs playerRefs,
            Image playerAvatar)
        {
            var so = new SerializedObject(gc);

            WireCharacter(so.FindProperty("Enemy"), enemyRefs, enemyAvatar, wireAttackTimer: true);
            WireCharacter(so.FindProperty("Player"), playerRefs, playerAvatar, wireAttackTimer: false);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gc);
        }

        private static void WireCharacter(
            SerializedProperty charProp,
            HudRefs refs,
            Image avatarImg,
            bool wireAttackTimer)
        {
            if (charProp == null) return;

            SetObjRef(charProp, "m_imgAvatar", avatarImg);
            SetObjRef(charProp, "m_Health", refs.HealthFill);
            SetObjRef(charProp, "m_Mana", refs.ManaFill);
            SetObjRef(charProp, "m_Skill", refs.ManaFill);
            SetObjRef(charProp, "m_timeToAttack", wireAttackTimer ? refs.AttackTimerFill : null);
            SetObjRef(charProp, "m_txtName", refs.TxtName);
            SetObjRef(charProp, "m_txtLevel", refs.TxtLevel);
            SetObjRef(charProp, "m_txtHealth", refs.TxtHealth);
            SetObjRef(charProp, "m_txtMana", refs.TxtMana);
        }

        private static void SetObjRef(SerializedProperty parent, string childName, Object value)
        {
            var p = parent.FindPropertyRelative(childName);
            if (p != null)
            {
                p.objectReferenceValue = value;
            }
        }

        private static void ConfigureFooterBoosters(RectTransform footer)
        {
            if (footer == null) return;

            footer.anchorMin = new Vector2(0f, 0f);
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(0.5f, 0f);
            footer.anchoredPosition = new Vector2(0f, AdBannerHeightPx);
            footer.sizeDelta = new Vector2(0f, FooterHeightPx);

            var footerImg = footer.GetComponent<Image>();
            if (footerImg != null)
            {
                footerImg.sprite = null;
                footerImg.color = new Color(0f, 0f, 0f, 0f);
            }

            // Hide legacy SupportCardsBar (weapon/outfit/teammates now live in PlayerPanel)
            var supportBar = footer.Find("SupportCardsBar");
            if (supportBar != null) supportBar.gameObject.SetActive(false);

            float[] boosterXs = { -228f, -76f, 76f, 228f };
            string[] slotNames = { "Slot_Booster1", "Slot_Booster2", "Slot_Booster3", "Slot_Booster4" };
            string[] btnNames = { "BtnSkill1_Molotov", "BtnSkill2_Knuckles", "BtnSkill3_Syringe", "BtnSkill4_Grenade" };
            string[] btnSprites =
            {
                "Assets/Project/Textures/skill_molotov.png",
                "Assets/Project/Textures/skill_knuckles.png",
                "Assets/Project/Textures/skill_syringe.png",
                "Assets/Project/Textures/skill_grenade.png"
            };
            string[] badgeCounts = { "x2", "x3", "x4", "x2" };

            for (int i = 0; i < 4; i++)
            {
                var slot = footer.Find(slotNames[i]) as RectTransform;
                if (slot != null)
                {
                    slot.anchorMin = new Vector2(0.5f, 0.5f);
                    slot.anchorMax = new Vector2(0.5f, 0.5f);
                    slot.pivot = new Vector2(0.5f, 0.5f);
                    slot.anchoredPosition = new Vector2(boosterXs[i], 0f);
                    slot.sizeDelta = new Vector2(106f, 106f);
                    AssignSprite(slot, "Assets/Project/Textures/booster_slot.png");
                }

                var btn = footer.Find(btnNames[i]) as RectTransform;
                if (btn != null)
                {
                    btn.anchorMin = new Vector2(0.5f, 0.5f);
                    btn.anchorMax = new Vector2(0.5f, 0.5f);
                    btn.pivot = new Vector2(0.5f, 0.5f);
                    btn.anchoredPosition = new Vector2(boosterXs[i], 0f);
                    btn.sizeDelta = new Vector2(96f, 96f);
                    AssignSprite(btn, btnSprites[i]);

                    var badge = btn.Find("Badge") as RectTransform;
                    if (badge != null)
                    {
                        badge.anchorMin = new Vector2(1f, 1f);
                        badge.anchorMax = new Vector2(1f, 1f);
                        badge.pivot = new Vector2(0.5f, 0.5f);
                        badge.anchoredPosition = new Vector2(-6f, -6f);
                        badge.sizeDelta = new Vector2(36f, 36f);
                        AssignSprite(badge, "Assets/Project/Textures/badge_count_bg.png");
                        var countTmp = badge.GetComponentInChildren<TextMeshProUGUI>();
                        if (countTmp != null)
                        {
                            countTmp.text = badgeCounts[i];
                            countTmp.fontSize = 18f;
                            countTmp.fontStyle = FontStyles.Bold;
                            countTmp.color = Color.white;
                        }
                    }
                }
            }
        }

        private static void ConfigureAdBannerZone(RectTransform canvasRect)
        {
            var bannerRect = GetOrCreateChild(canvasRect, "AdBannerZone");
            bannerRect.anchorMin = new Vector2(0f, 0f);
            bannerRect.anchorMax = new Vector2(1f, 0f);
            bannerRect.pivot = new Vector2(0.5f, 0f);
            bannerRect.anchoredPosition = Vector2.zero;
            bannerRect.sizeDelta = new Vector2(0f, AdBannerHeightPx);

            var bg = GetOrAddComponent<Image>(bannerRect);
            bg.sprite = null;
            bg.color = new Color(0.04f, 0.06f, 0.11f, 0.96f);

            var topLine = GetOrCreateChild(bannerRect, "TopDivider");
            topLine.anchorMin = new Vector2(0f, 1f);
            topLine.anchorMax = new Vector2(1f, 1f);
            topLine.pivot = new Vector2(0.5f, 1f);
            topLine.anchoredPosition = Vector2.zero;
            topLine.sizeDelta = new Vector2(0f, 2f);
            GetOrAddComponent<Image>(topLine).color = new Color(0.28f, 0.36f, 0.48f, 0.35f);

            var lblRect = GetOrCreateChild(bannerRect, "Label");
            StretchFull(lblRect);
            var tmp = ConfigureTMP(lblRect, "ADMOB BANNER (320×50 / ADAPTIVE 60dp)", 18f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.45f, 0.52f, 0.62f, 0.45f));
            tmp.raycastTarget = false;
        }

        private static void ConfigureShapesManager()
        {
            var shapesManager = Object.FindFirstObjectByType<ShapesManager>();
            if (shapesManager == null) return;

            var so = new SerializedObject(shapesManager);
            var prop = so.FindProperty("m_boardCenterY");
            if (prop != null)
            {
                prop.floatValue = BoardWorldCenterY;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(shapesManager);
            }
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.gameObject.layer;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static RectTransform GetOrCreateChild(RectTransform parent, string name)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null) return existing;
            return CreateUIObject(name, parent);
        }

        private static T GetOrAddComponent<T>(Component target) where T : Component
        {
            var comp = target.GetComponent<T>();
            if (comp == null) comp = target.gameObject.AddComponent<T>();
            return comp;
        }

        private static void SetTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void StretchFull(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static TextMeshProUGUI ConfigureTMP(
            RectTransform rt,
            string text,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Color color)
        {
            var tmp = GetOrAddComponent<TextMeshProUGUI>(rt);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void AddTextShadow(RectTransform rt)
        {
            var shadow = GetOrAddComponent<Shadow>(rt);
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private static void AssignSprite(Transform target, string assetPath)
        {
            if (target == null) return;
            var img = target.GetComponent<Image>();
            if (img == null) return;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                img.sprite = sprite;
            }
        }

        private static void HideLegacyText(Transform target)
        {
            if (target == null) return;
            var legacyText = target.GetComponent<Text>();
            if (legacyText != null)
            {
                legacyText.color = new Color(0f, 0f, 0f, 0f);
                legacyText.raycastTarget = false;
            }
        }

        private static void AssignSlicedSprite(Image img, string assetPath, Color tint, float pixelsPerUnitMultiplier = 1f)
        {
            if (img == null) return;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            }
            img.color = tint;
        }
    }
}
