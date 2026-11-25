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

        // Canvas reference resolution is 720 x 1600 (scale 0.01 => 7.2 x 16.0 world units).
        private const float AdBannerHeightPx = 120f;
        private const float FooterHeightPx = 140f;

        // 7x7 grid with ItemSize = (0.9, 0.9) => 6.3 x 6.3 world units => 630 x 630 px inside 664 x 664 BoardFrame.
        private const float BoardFrameSizePx = 664f;
        private const float BoardCenterYPx = 54f;
        private const float BoardWorldCenterY = BoardCenterYPx / 100f;

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

            // Both character panels inherit their layout from CharacterPanel.prefab.
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
                var fitter = bgr.GetComponent<AspectRatioFitter>();
                if (fitter != null) Object.DestroyImmediate(fitter);
                StretchFull(bgr);
                var background = GetOrAddComponent<Image>(bgr);
                background.sprite = null;
                background.color = new Color32(18, 23, 35, 255);
            }
        }

        private static void ConfigureWeaponObjects(RectTransform canvasRect)
        {
            ConfigureSingleWeapon(canvasRect.Find("Knife_Enemy") as RectTransform, new Vector2(-246f, 556f));
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
