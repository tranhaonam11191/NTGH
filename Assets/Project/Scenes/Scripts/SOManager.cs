using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SS.UI;

public class SOManager : MonoBehaviour
{
    public static SOManager instance;

    public List<LevelData> allDataStory;

    public static DataForSceneGame Data;

    public static int story_num;

    public static string Level;

    [SerializeField] CreateItemReward[] itemRewards;

    [SerializeField] AudioSource m_audioSource;

    LevelData _currentLevelData;

    public LevelData CurrentLevelData
    {
        get { return _currentLevelData; }
        set { _currentLevelData = value; }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        story_num = PlayerPrefs.GetInt(Const.NUM_STORY, 0);
        Level = PlayerPrefs.GetString(Const.LEVEL, string.Empty);
        _currentLevelData = allDataStory[story_num];
    }

    public void PlayAudio(string fileName)
    {
        SoundManager.instance.PlaySfx(fileName);
    }

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
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
