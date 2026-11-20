using System.Collections.Generic;
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
        m_Levels = m_ArcsData != null ? m_ArcsData.GetOrderedLevels() : new List<LevelData>();
        if (m_Levels.Count > 0)
            m_LevelIndex = Mathf.Max(0, PlayerPrefs.GetInt(Const.NUM_STORY, 0)) % m_Levels.Count;
    }

    public void PlayCurrentLevel()
    {
        var level = CurrentLevelData;
        if (m_Loading) return;
        if (level == null || !level.HasCombatants)
        {
            Debug.LogError("Assign a level with two characters and positive HP in ArcsData.", this);
            return;
        }

        m_Loading = true;
        Core.Init();
        var opening = new List<ConversationData>();
        foreach (var group in level.conversations)
        {
            if (group?.trigger == ConversationTrigger.Start && group.lines != null)
                opening.AddRange(group.lines.FindAll(line => line != null));
        }
        if (opening.Count == 0)
        {
            LoadBattle(level);
            return;
        }

        Core.Load<Canvas>("StoryIntro", onSceneLoaded: _ =>
            StoryController.Show(opening, () => LoadBattle(level)));
    }

    void LoadBattle(LevelData level)
    {
        Core.Load<GameController>(GameController.SCENE_NAME, onSceneLoaded: game =>
        {
            m_Loading = false;
            game.Setup(level, openingPlayed: true);
        });
    }

    public void AdvanceAfterWin(LevelData completed)
    {
        if (completed == null || completed != CurrentLevelData) return;
        m_LevelIndex = (m_LevelIndex + 1) % m_Levels.Count;
        PlayerPrefs.SetInt(Const.NUM_STORY, m_LevelIndex);
        PlayerPrefs.Save();
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
        if (PlayerPrefs.HasKey(Const.CURRENTWEAPON))
        {
            foreach (CreateItemReward item in itemRewards)
            {
                if (item.ItemReward.ID.Equals(PlayerPrefs.GetString(Const.CURRENTWEAPON)))
                {
                    return item;
                }
            }
        }
        return null;
    }

    public Image m_imgFade;        
}
