using System.Collections.Generic;
using NTGH.Demo.OutfitSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

namespace NTGH.EditorTools
{
    public static class OutfitDemoBuilder
    {
        private const string RootDir = "Assets/Project/Demo/OutfitSystem";
        private const string LibrariesDir = RootDir + "/Libraries";
        private const string DataDir = RootDir + "/Data";
        private const string PrefabsDir = RootDir + "/Prefabs";
        private const string DemoScenePath = RootDir + "/OutfitDemo.unity";
        private const string PartsBaseDir = "Assets/Project/Textures/DauBuSkins/Parts";

        private static readonly string[] Categories =
        {
            "Head", "Torso", "LeftArm", "RightArm", "LeftLeg", "RightLeg"
        };

        private static readonly string[] BoneNames =
        {
            "Root_Bone", "Spine_Bone", "Head_Bone", "LeftArm_Bone", "RightArm_Bone", "LeftLeg_Bone", "RightLeg_Bone"
        };

        // Pivots on the six registered concept crops, in category order.
        private const float PixelsPerUnit = 185f;
        private static readonly Vector2[] PartPivots =
        {
            new Vector2(120f / 168f, 42f / 194f),
            new Vector2(185f / 439f, 328f / 656f),
            new Vector2(97f / 176f, 287f / 365f),
            new Vector2(77f / 217f, 287f / 373f),
            new Vector2(131f / 251f, 514f / 595f),
            new Vector2(95f / 308f, 514f / 595f)
        };

        private struct RankTierSpec
        {
            public string FolderName;
            public string AssetKey;
            public string MixLabel;
            public string OutfitId;
            public string DisplayName;
            public string RankTitle;
            public int PriceGold;
            public int BonusHealth;
            public int BonusDefence;
            public Color ButtonColor;
        }

        private static readonly RankTierSpec[] RankTiers =
        {
            new RankTierSpec
            {
                FolderName = "Lv1_TuCuu",
                AssetKey = "Outfit_01_TuCuu",
                MixLabel = "Lv1",
                OutfitId = "outfit_lv1_tucuu",
                DisplayName = "Đồ Bán Báo",
                RankTitle = "Tứ Cửu",
                PriceGold = 0,
                BonusHealth = 50,
                BonusDefence = 10,
                ButtonColor = new Color(0.35f, 0.38f, 0.42f)
            },
            new RankTierSpec
            {
                FolderName = "Lv2_HongCon",
                AssetKey = "Outfit_02_HongCon",
                MixLabel = "Lv2",
                OutfitId = "outfit_lv2_hongcon",
                DisplayName = "Áo Khoác Đường Phố",
                RankTitle = "Hồng Côn",
                PriceGold = 500,
                BonusHealth = 150,
                BonusDefence = 30,
                ButtonColor = new Color(0.22f, 0.25f, 0.30f)
            },
            new RankTierSpec
            {
                FolderName = "Lv3_SongHoaHongCon",
                AssetKey = "Outfit_03_SongHoaHongCon",
                MixLabel = "Lv3",
                OutfitId = "outfit_lv3_songhoa",
                DisplayName = "Áo Khoác Da",
                RankTitle = "Song Hoa Hồng Côn",
                PriceGold = 1500,
                BonusHealth = 320,
                BonusDefence = 65,
                ButtonColor = new Color(0.48f, 0.16f, 0.14f)
            },
            new RankTierSpec
            {
                FolderName = "Lv4_NhiLoNguyenSoai",
                AssetKey = "Outfit_04_NhiLoNguyenSoai",
                MixLabel = "Lv4",
                OutfitId = "outfit_lv4_nhilo",
                DisplayName = "Vest Xanh Navy",
                RankTitle = "Nhị Lộ Nguyên Soái",
                PriceGold = 3500,
                BonusHealth = 600,
                BonusDefence = 120,
                ButtonColor = new Color(0.14f, 0.24f, 0.44f)
            },
            new RankTierSpec
            {
                FolderName = "Lv5_LongDau",
                AssetKey = "Outfit_05_LongDau",
                MixLabel = "Lv5",
                OutfitId = "outfit_lv5_longdau",
                DisplayName = "Vest Long Đầu",
                RankTitle = "Long Đầu",
                PriceGold = 8000,
                BonusHealth = 1200,
                BonusDefence = 240,
                ButtonColor = new Color(0.68f, 0.50f, 0.12f)
            }
        };

        [MenuItem("Tools/NTGH/Build Outfit SpriteLibrary Demo")]
        public static void BuildDemo()
        {
            EnsureDirectories();
            AssetDatabase.Refresh();

            var allSets = new List<Dictionary<string, Sprite>>(RankTiers.Length);
            var allOutfits = new List<OutfitData>(RankTiers.Length);
            SpriteLibraryAsset firstLibrary = null;

            // 1. Configure & rig all 5 tiers of Dau Bu modular parts (30 PNGs)
            for (int i = 0; i < RankTiers.Length; i++)
            {
                RankTierSpec tier = RankTiers[i];
                Dictionary<string, Sprite> partSprites = LoadAndRigTierParts(tier);
                allSets.Add(partSprites);

                SpriteLibraryAsset lib = CreateSetLibrary(tier.AssetKey, partSprites);
                if (firstLibrary == null)
                {
                    firstLibrary = lib;
                }

                OutfitData data = CreateOutfitData(
                    $"OutfitData_{tier.AssetKey}",
                    tier.OutfitId,
                    tier.DisplayName,
                    tier.RankTitle,
                    tier.PriceGold,
                    tier.BonusHealth,
                    tier.BonusDefence,
                    lib,
                    null);
                allOutfits.Add(data);
            }

            // 2. Create combined Mix-and-Match SpriteLibraryAsset across all 5 tiers
            SpriteLibraryAsset mixLib = CreateMixAndMatchLibrary(allSets);

            // 3. Create Character Prefab with 7-Bone Skeleton + SpriteLibrary + SpriteResolver + SpriteSkin
            GameObject characterPrefab = CreateCharacterPrefab(firstLibrary, mixLib, allOutfits);

            // 4. Create Demo Scene with Camera + Character + 5-Tier UI
            CreateDemoScene(characterPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[NTGH] 5-Tier Đầu Bự Outfit SpriteLibrary Demo built successfully! Open " + DemoScenePath + " and press Play.");
        }

        private static void EnsureDirectories()
        {
            EnsureFolder("Assets/Project", "Demo");
            EnsureFolder("Assets/Project/Demo", "OutfitSystem");
            EnsureFolder(RootDir, "Libraries");
            EnsureFolder(RootDir, "Data");
            EnsureFolder(RootDir, "Prefabs");
            AssetDatabase.Refresh();
        }

        private static void EnsureFolder(string parentFolder, string newFolderName)
        {
            string fullPath = $"{parentFolder}/{newFolderName}";
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentFolder, newFolderName);
            }
        }

        private static Dictionary<string, Sprite> LoadAndRigTierParts(RankTierSpec tier)
        {
            string dir = $"{PartsBaseDir}/{tier.FolderName}";
            var result = new Dictionary<string, Sprite>();

            int[] boneIndices = { 2, 1, 3, 4, 5, 6 };
            for (int i = 0; i < Categories.Length; i++)
            {
                string category = Categories[i];
                result[category] = ConfigureRiggedSprite($"{dir}/{category}.png", boneIndices[i], PartPivots[i]);
            }

            return result;
        }

        private static Sprite ConfigureRiggedSprite(string assetPath, int boneIndex, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[NTGH] Missing part sprite at: " + assetPath);
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            importer.spritePixelsPerUnit = PixelsPerUnit;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            // Attach shared SpriteBone definitions + full-quad mesh bound to boneIndex
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            SpriteRect[] spriteRects = dataProvider.GetSpriteRects();
            if (spriteRects != null && spriteRects.Length > 0)
            {
                SpriteRect rect = spriteRects[0];
                GUID spriteId = rect.spriteID;

                ISpriteBoneDataProvider boneProvider = dataProvider.GetDataProvider<ISpriteBoneDataProvider>();
                if (boneProvider != null)
                {
                    boneProvider.SetBones(spriteId, CreateSharedSpriteBones(rect.rect.size, pivot));
                }

                ISpriteMeshDataProvider meshProvider = dataProvider.GetDataProvider<ISpriteMeshDataProvider>();
                if (meshProvider != null)
                {
                    float w = rect.rect.width;
                    float h = rect.rect.height;
                    BoneWeight weight = new BoneWeight { boneIndex0 = boneIndex, weight0 = 1f };

                    Vertex2DMetaData[] vertices =
                    {
                        new Vertex2DMetaData { position = new Vector2(0f, 0f), boneWeight = weight },
                        new Vertex2DMetaData { position = new Vector2(w, 0f), boneWeight = weight },
                        new Vertex2DMetaData { position = new Vector2(w, h), boneWeight = weight },
                        new Vertex2DMetaData { position = new Vector2(0f, h), boneWeight = weight }
                    };

                    int[] indices = { 0, 1, 2, 0, 2, 3 };
                    Vector2Int[] edges =
                    {
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 2),
                        new Vector2Int(2, 3),
                        new Vector2Int(3, 0)
                    };

                    meshProvider.SetVertices(spriteId, vertices);
                    meshProvider.SetIndices(spriteId, indices);
                    meshProvider.SetEdges(spriteId, edges);
                }

                dataProvider.Apply();
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static List<SpriteBone> CreateSharedSpriteBones(Vector2 spriteSize, Vector2 pivot)
        {
            Vector3 pivotPixel = new Vector3(spriteSize.x * pivot.x, spriteSize.y * pivot.y, 0f);
            var bones = new List<SpriteBone>(BoneNames.Length);

            bones.Add(new SpriteBone
            {
                name = BoneNames[0],
                guid = "00000000000000000000000000000001",
                position = pivotPixel,
                rotation = Quaternion.identity,
                length = 50f,
                parentId = -1,
                color = Color.white
            });

            bones.Add(new SpriteBone
            {
                name = BoneNames[1],
                guid = "00000000000000000000000000000002",
                position = Vector3.zero,
                rotation = Quaternion.identity,
                length = 120f,
                parentId = 0,
                color = Color.green
            });

            bones.Add(new SpriteBone
            {
                name = BoneNames[2],
                guid = "00000000000000000000000000000003",
                position = Vector3.zero,
                rotation = Quaternion.identity,
                length = 80f,
                parentId = 1,
                color = Color.yellow
            });

            bones.Add(new SpriteBone
            {
                name = BoneNames[3],
                guid = "00000000000000000000000000000004",
                position = Vector3.zero,
                rotation = Quaternion.identity,
                length = 100f,
                parentId = 1,
                color = Color.cyan
            });

            bones.Add(new SpriteBone
            {
                name = BoneNames[4],
                guid = "00000000000000000000000000000005",
                position = Vector3.zero,
                rotation = Quaternion.identity,
                length = 100f,
                parentId = 1,
                color = Color.magenta
            });

            bones.Add(new SpriteBone
            {
                name = BoneNames[5],
                guid = "00000000000000000000000000000006",
                position = Vector3.zero,
                rotation = Quaternion.identity,
                length = 110f,
                parentId = 0,
                color = Color.blue
            });

            bones.Add(new SpriteBone
            {
                name = BoneNames[6],
                guid = "00000000000000000000000000000007",
                position = Vector3.zero,
                rotation = Quaternion.identity,
                length = 110f,
                parentId = 0,
                color = Color.red
            });

            return bones;
        }

        private static SpriteLibraryAsset CreateSetLibrary(string assetName, Dictionary<string, Sprite> sprites)
        {
            string path = $"{LibrariesDir}/{assetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            SpriteLibraryAsset library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            foreach (string category in Categories)
            {
                if (sprites.TryGetValue(category, out Sprite sprite) && sprite != null)
                {
                    library.AddCategoryLabel(sprite, category, CharacterOutfitController.DefaultLabel);
                }
            }

            AssetDatabase.CreateAsset(library, path);
            EditorUtility.SetDirty(library);
            return library;
        }

        private static SpriteLibraryAsset CreateMixAndMatchLibrary(List<Dictionary<string, Sprite>> allSets)
        {
            string path = $"{LibrariesDir}/Outfit_MixAndMatch.asset";
            if (AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            SpriteLibraryAsset library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            for (int i = 0; i < allSets.Count && i < RankTiers.Length; i++)
            {
                string label = RankTiers[i].MixLabel;
                foreach (string category in Categories)
                {
                    if (allSets[i].TryGetValue(category, out Sprite sprite) && sprite != null)
                    {
                        library.AddCategoryLabel(sprite, category, label);
                    }
                }
            }

            AssetDatabase.CreateAsset(library, path);
            EditorUtility.SetDirty(library);
            return library;
        }

        private static OutfitData CreateOutfitData(
            string assetName,
            string id,
            string displayName,
            string rankTitle,
            int priceGold,
            int bonusHealth,
            int bonusDefence,
            SpriteLibraryAsset library,
            Sprite weaponSprite)
        {
            string path = $"{DataDir}/{assetName}.asset";
            OutfitData data = AssetDatabase.LoadAssetAtPath<OutfitData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<OutfitData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.Configure(id, displayName, rankTitle, priceGold, bonusHealth, bonusDefence, library, weaponSprite);
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject CreateCharacterPrefab(
            SpriteLibraryAsset defaultLibrary,
            SpriteLibraryAsset mixLibrary,
            List<OutfitData> outfits)
        {
            GameObject root = new GameObject("DemoCharacter_Bone");

            SpriteLibrary spriteLibrary = root.AddComponent<SpriteLibrary>();
            spriteLibrary.spriteLibraryAsset = defaultLibrary;

            // The drawing already contains the stance. Rest transforms register its joints in pixels.
            Transform rootBone = CreateChild(root.transform, BoneNames[0], Vector3.zero);
            Transform spineBone = CreateChild(rootBone, BoneNames[1], Vector3.zero);
            Transform headBone = CreateChild(spineBone, BoneNames[2], new Vector3(8f, 274f, 0f) / PixelsPerUnit);
            Transform leftArmBone = CreateChild(spineBone, BoneNames[3], new Vector3(-85f, 226f, 0f) / PixelsPerUnit);
            Transform rightArmBone = CreateChild(spineBone, BoneNames[4], new Vector3(88f, 224f, 0f) / PixelsPerUnit);
            Transform leftLegBone = CreateChild(rootBone, BoneNames[5], new Vector3(-40f, -2f, 0f) / PixelsPerUnit);
            Transform rightLegBone = CreateChild(rootBone, BoneNames[6], new Vector3(51f, -2f, 0f) / PixelsPerUnit);

            Transform[] allBones = { rootBone, spineBone, headBone, leftArmBone, rightArmBone, leftLegBone, rightLegBone };

            // Optional rigid weapon slot mounted on LeftArm_Bone (foreground fist)
            Transform weaponSlot = CreateChild(leftArmBone, "WeaponSlot", new Vector3(-0.08f, -1.65f, 0f));
            SpriteRenderer weaponRenderer = weaponSlot.gameObject.AddComponent<SpriteRenderer>();
            weaponRenderer.sortingOrder = 7;
            weaponRenderer.enabled = false;

            // Create Visual Parts with Sorting Orders matching 3/4 right-facing pose
            Transform partsRoot = CreateChild(root.transform, "Parts", Vector3.zero);
            var resolvers = new List<SpriteResolver>();
            var spriteSkins = new List<SpriteSkin>();

            CreatePart(partsRoot, "RightArm", rightArmBone.position, 1, rootBone, allBones, resolvers, spriteSkins);
            CreatePart(partsRoot, "RightLeg", rightLegBone.position, 2, rootBone, allBones, resolvers, spriteSkins);
            CreatePart(partsRoot, "LeftLeg", leftLegBone.position, 3, rootBone, allBones, resolvers, spriteSkins);
            CreatePart(partsRoot, "Torso", spineBone.position, 4, rootBone, allBones, resolvers, spriteSkins);
            CreatePart(partsRoot, "Head", headBone.position, 5, rootBone, allBones, resolvers, spriteSkins);
            CreatePart(partsRoot, "LeftArm", leftArmBone.position, 6, rootBone, allBones, resolvers, spriteSkins);

            // Attach Controllers
            CharacterOutfitController outfitController = root.AddComponent<CharacterOutfitController>();
            outfitController.SetupReferences(spriteLibrary, resolvers, spriteSkins, weaponRenderer, outfits, mixLibrary);

            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                RootDir + "/Animations/DauBu.controller");

            string prefabPath = $"{PrefabsDir}/DemoCharacter_Bone.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreatePart(
            Transform parent,
            string category,
            Vector3 worldPos,
            int sortingOrder,
            Transform rootBone,
            Transform[] allBones,
            List<SpriteResolver> resolvers,
            List<SpriteSkin> spriteSkins)
        {
            GameObject partObj = new GameObject(category);
            partObj.transform.SetParent(parent, false);
            partObj.transform.position = worldPos;

            SpriteRenderer sr = partObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;

            SpriteResolver resolver = partObj.AddComponent<SpriteResolver>();
            resolver.SetCategoryAndLabel(category, CharacterOutfitController.DefaultLabel);
            resolver.ResolveSpriteToSpriteRenderer();
            resolvers.Add(resolver);

            SpriteSkin skin = partObj.AddComponent<SpriteSkin>();
            skin.autoRebind = true;
            skin.SetRootBone(rootBone);
            skin.SetBoneTransforms(allBones);
            spriteSkins.Add(skin);
        }

        private static void CreateDemoScene(GameObject characterPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Main Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.0f;
            cam.backgroundColor = new Color(0.11f, 0.13f, 0.17f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.transform.position = new Vector3(0f, 0.35f, -10f);
            camObj.AddComponent<AudioListener>();

            // Character Instance
            GameObject charInstance = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
            charInstance.transform.position = new Vector3(0f, 1.02f, 0f);
            CharacterOutfitController controller = charInstance.GetComponent<CharacterOutfitController>();

            // EventSystem
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();

            // Canvas UI
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Top Info Panel
            CreateText(canvasObj.transform, "Txt_Title", "ĐẦU BỰ — 5 CẤP ĐỘ TRANG PHỤC (2D BONE)", 40, new Vector2(0f, 840f), new Vector2(980f, 75f), new Color(1f, 0.85f, 0.35f));
            TextMeshProUGUI txtOutfitInfo = CreateText(canvasObj.transform, "Txt_OutfitInfo", "Đồ Bán Báo (Tứ Cửu)", 44, new Vector2(0f, 755f), new Vector2(980f, 75f), Color.white);
            TextMeshProUGUI txtStats = CreateText(canvasObj.transform, "Txt_Stats", "HP +50   |   DEF +10   |   Price: 0G", 34, new Vector2(0f, 685f), new Vector2(980f, 65f), new Color(0.70f, 0.92f, 0.72f));

            // 5 Rank Outfit Buttons (Row 1: Lv1-Lv3, Row 2: Lv4-Lv5)
            Button[] outfitButtons = new Button[RankTiers.Length];
            outfitButtons[0] = CreateButton(canvasObj.transform, "Btn_Lv1", "1. Tứ Cửu", new Vector2(-330f, -520f), new Vector2(300f, 100f), RankTiers[0].ButtonColor);
            outfitButtons[1] = CreateButton(canvasObj.transform, "Btn_Lv2", "2. Hồng Côn", new Vector2(0f, -520f), new Vector2(300f, 100f), RankTiers[1].ButtonColor);
            outfitButtons[2] = CreateButton(canvasObj.transform, "Btn_Lv3", "3. Song Hoa", new Vector2(330f, -520f), new Vector2(300f, 100f), RankTiers[2].ButtonColor);
            outfitButtons[3] = CreateButton(canvasObj.transform, "Btn_Lv4", "4. Nhị Lộ", new Vector2(-180f, -650f), new Vector2(340f, 100f), RankTiers[3].ButtonColor);
            outfitButtons[4] = CreateButton(canvasObj.transform, "Btn_Lv5", "5. Long Đầu", new Vector2(180f, -650f), new Vector2(340f, 100f), RankTiers[4].ButtonColor);

            // Bottom Mix-and-Match Buttons
            Button btnTorso = CreateButton(canvasObj.transform, "Btn_MixTorso", "Mix: Áo", new Vector2(-320f, -790f), new Vector2(290f, 92f), new Color(0.16f, 0.40f, 0.54f));
            Button btnArms = CreateButton(canvasObj.transform, "Btn_MixArms", "Mix: Tay", new Vector2(0f, -790f), new Vector2(290f, 92f), new Color(0.16f, 0.40f, 0.54f));
            Button btnLegs = CreateButton(canvasObj.transform, "Btn_MixLegs", "Mix: Chân", new Vector2(320f, -790f), new Vector2(290f, 92f), new Color(0.16f, 0.40f, 0.54f));

            OutfitDemoUI demoUI = canvasObj.AddComponent<OutfitDemoUI>();
            demoUI.SetupReferences(controller, txtOutfitInfo, txtStats, outfitButtons, btnTorso, btnArms, btnLegs);

            EditorSceneManager.SaveScene(scene, DemoScenePath);
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            return obj.transform;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, Vector2 anchoredPos, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            if (parent.TryGetComponent<Canvas>(out _))
            {
                bool top = anchoredPos.y > 0f;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, top ? 1f : 0f);
                anchoredPos.y += top ? -960f : 960f;
            }
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, Color bgColor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            if (parent.TryGetComponent<Canvas>(out _))
            {
                bool top = anchoredPos.y > 0f;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, top ? 1f : 0f);
                anchoredPos.y += top ? -960f : 960f;
            }
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            TextMeshProUGUI tmp = CreateText(obj.transform, "Label", label, 30, Vector2.zero, size, Color.white);
            tmp.raycastTarget = false;
            return btn;
        }
    }
}
