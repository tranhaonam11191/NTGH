using System.Collections.Generic;
using NTGH.Boosters;
using NTGH.Teammates;
using NTGH.UI;
using UnityEngine;
using UnityEngine.UI;
using SS.UI;

public class SOManager : MonoBehaviour
{
    public static SOManager instance;

    [SerializeField] ArcsData m_ArcsData;
    List<LevelData> m_Levels;
    int m_LevelIndex;
    bool m_Loading;

    public LevelData CurrentLevelData => m_Levels.Count == 0 ? null : m_Levels[m_LevelIndex];
    public int CurrentLevelNumber => m_LevelIndex + 1;

    [SerializeField] CreateItemReward[] itemRewards;

    [Header("Loadout Catalogs & Teammate Database")]
    [SerializeField] ItemCatalogSO m_weaponCatalog;
    [SerializeField] ItemCatalogSO m_outfitCatalog;
    [SerializeField] TeammateCardDatabaseSO m_teammateDatabase;
    [SerializeField] BoosterDatabaseSO m_boosterDatabase;

    public ItemCatalogSO WeaponCatalog => m_weaponCatalog;
    public ItemCatalogSO OutfitCatalog => m_outfitCatalog;
    public TeammateCardDatabaseSO TeammateDatabase => m_teammateDatabase;
    public BoosterDatabaseSO BoosterDatabase => m_boosterDatabase;

    [SerializeField] AudioSource m_audioSource;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        User.EnsureLoaded();
        if (User.data != null && User.data.EnsureTeammateDefaults(m_teammateDatabase))
        {
            User.Save();
        }
        MigrateLegacyWeapons();
        m_Levels = m_ArcsData != null ? m_ArcsData.GetOrderedLevels() : new List<LevelData>();
        if (m_Levels.Count > 0)
            m_LevelIndex = Mathf.Max(0, User.data.level) % m_Levels.Count;
    }

    void MigrateLegacyWeapons()
    {
        if (itemRewards == null || itemRewards.Length == 0) return;

        var ids = new List<string>(itemRewards.Length);
        foreach (CreateItemReward item in itemRewards)
        {
            if (item != null && item.ItemReward != null && !string.IsNullOrEmpty(item.ItemReward.ID))
            {
                ids.Add(item.ItemReward.ID);
            }
        }

        User.MigrateLegacyUnlockedItems(ids);
    }

    public void PlayCurrentLevel(Canvas callerCanvas = null)
    {
        var level = CurrentLevelData;
        if (m_Loading) return;

        User.RefreshHeartsAndSave();
        if (User.data != null && !User.data.HasHeart())
        {
            BuyHeartPopupController.Open(callerCanvas: callerCanvas);
            return;
        }

        if (level == null || !level.HasCombatants)
        {
            Debug.LogError("Assign a level with two characters and positive HP in ArcsData.", this);
            return;
        }

        m_Loading = true;
        Core.Init();
        User.EnsureLoaded();
        var opening = new List<ConversationData>();
        var openingKeys = new List<string>();
        if (level.conversations != null)
        {
            for (int i = 0; i < level.conversations.Count; i++)
            {
                var group = level.conversations[i];
                if (group == null || group.trigger != ConversationTrigger.Start || group.lines == null)
                {
                    continue;
                }

                string convKey = level.GetConversationKey(i);
                if (User.data != null && User.data.IsConversationWatched(convKey))
                {
                    continue;
                }

                int addedCount = 0;
                for (int j = 0; j < group.lines.Count; j++)
                {
                    var line = group.lines[j];
                    if (line != null)
                    {
                        opening.Add(line);
                        addedCount++;
                    }
                }

                if (addedCount > 0 && !string.IsNullOrEmpty(convKey))
                {
                    openingKeys.Add(convKey);
                }
            }
        }

        if (opening.Count == 0)
        {
            LoadBattle(level);
            return;
        }

        Core.Load<Canvas>("StoryIntro", onSceneLoaded: canvas =>
            StoryController.Show(opening, newlyUnlockedIds =>
            {
                MarkConversationsWatched(openingKeys);
                OnStoryIntroCompleted(level, newlyUnlockedIds, canvas);
            }));
    }

    static void MarkConversationsWatched(IEnumerable<string> conversationKeys)
    {
        if (conversationKeys == null)
        {
            return;
        }

        User.EnsureLoaded();
        if (User.data != null && User.data.MarkConversationsWatched(conversationKeys))
        {
            User.Save();
        }
    }

    void OnStoryIntroCompleted(LevelData level, IReadOnlyList<string> newlyUnlockedIds, Canvas callerCanvas)
    {
        if (newlyUnlockedIds != null && newlyUnlockedIds.Count > 0 && m_teammateDatabase != null)
        {
            TeammateSquadPopupController.OpenForPreBattle(
                m_teammateDatabase,
                newlyUnlockedIds,
                onConfirmBattle: () => LoadBattle(level),
                callerCanvas: callerCanvas);
            return;
        }

        LoadBattle(level);
    }

    void LoadBattle(LevelData level)
    {
        var playerLoadout = BuildPlayerBattleLoadout();
        Core.Load<GameController>(GameController.SCENE_NAME, onSceneLoaded: game =>
        {
            m_Loading = false;
            game.Setup(level, openingPlayed: true, playerLoadout: playerLoadout);
        });
    }

    public BattleLoadout BuildPlayerBattleLoadout()
    {
        User.EnsureLoaded();
        return BattleLoadout.FromUserData(
            User.data,
            m_weaponCatalog,
            m_outfitCatalog,
            m_teammateDatabase,
            GetWeapon());
    }

    public BattleLoadout BuildEnemyBattleLoadout(CharacterData enemyData)
    {
        return BattleLoadout.FromCharacterData(enemyData, m_weaponCatalog, m_outfitCatalog);
    }

    public void AdvanceAfterWin(LevelData completed)
    {
        if (completed == null || completed != CurrentLevelData) return;
        User.EnsureLoaded();
        m_LevelIndex = (m_LevelIndex + 1) % m_Levels.Count;
        User.data.level = m_LevelIndex;
        User.Save();
    }

    public static void LoadHome()
    {
        Core.Init();
        if (instance != null && instance.TryPlayPendingStoryVideoBeforeHome())
        {
            return;
        }

        Core.Load<HomeController>("Home");
    }

    private bool TryPlayPendingStoryVideoBeforeHome()
    {
        User.EnsureLoaded();
        if (m_Levels != null && m_Levels.Count > 0 && User.data != null)
        {
            m_LevelIndex = Mathf.Max(0, User.data.level) % m_Levels.Count;
        }

        var level = CurrentLevelData;
        if (level == null || !level.HasStoryVideo || User.data == null)
        {
            return false;
        }

        string videoKey = level.GetStoryVideoKey();
        if (User.data.IsStoryVideoWatched(videoKey))
        {
            return false;
        }

        Core.Load<StoryVideoController>(StoryVideoController.SCENE_NAME, onSceneLoaded: controller =>
        {
            if (controller != null)
            {
                controller.Setup(level);
            }
        });
        return true;
    }

    public void PlayAudio(string fileName)
    {
        SoundManager.instance.PlaySfx(fileName);
    }

    // Start is called before the first frame update
    void Start()
    {
        m_imgFade.color = new Color(0, 0, 0, 0);
    }


    public CreateItemReward GetWeapon()
    {
        User.EnsureLoaded();
        string currentWeaponId = User.data.current_weapon_id;
        if (!string.IsNullOrEmpty(currentWeaponId))
        {
            foreach (CreateItemReward item in itemRewards)
            {
                if (item != null && item.ItemReward != null && item.ItemReward.ID.Equals(currentWeaponId))
                {
                    return item;
                }
            }
        }
        return null;
    }

    public Image m_imgFade;        
}
