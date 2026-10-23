using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public CreateAllDataStory allDataStory;

    public static DataForSceneGame Data;

    public static int story_num;

    public static string Level;

    [SerializeField] CreateItemReward[] itemRewards;

    [SerializeField] AudioSource m_audioSource;

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


        CheckAndGetData(Const.Stage, ref story_num);

        CheckAndGetData(Const.LEVEL, ref Level);

        if (PlayerPrefs.HasKey(Const.NUM_STORY))
        {
            story_num = PlayerPrefs.GetInt(Const.NUM_STORY);
        }
    }

    public void PlayAudio(string fileName)
    {
        SoundManager.instance.PlaySfx(fileName);
    }

    public void CheckAndGetData(string key, ref float value)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetFloat(key, 0);
        }
        value = PlayerPrefs.GetFloat(key);
    }

    public void CheckAndGetData(string key, ref int value)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(key, 0);
        }
        value = PlayerPrefs.GetInt(key);
    }

    public void CheckAndGetData(string key, ref string value)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetString(key, string.Empty);
        }
        value = PlayerPrefs.GetString(key);
    }
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        m_imgFade.color = new Color(0, 0, 0, 0);
    }

    public List<ContentStory> GetListContentStory()
    {
        return allDataStory.Data[story_num].Data;
    }

    public CreateData GetData()
    {
        if (story_num < allDataStory.Data.Count)
        {
            return allDataStory.Data[story_num];
        }
        else
        {
            return null;
        }
    }

    public CreateItemReward GetWeapon()
    {
        if (PlayerPrefs.HasKey(Const.CURRENTWEAPON))
        {
            foreach (CreateItemReward item in itemRewards)
            {
                if(item.ItemReward.ID.Equals(PlayerPrefs.GetString(Const.CURRENTWEAPON)))
                {
                    return item;
                }
            }
        }
        return null;
    }

    public Image m_imgFade;

    [HideInInspector]
    public object data;

    [HideInInspector]
    public string sceneName;

    public delegate void callbackLoadScene();

    callbackLoadScene callback;

    public void LoadScene(string sceneName, object data = null)
    {
        this.data = data;
        this.sceneName = sceneName;
        callback = coLoadScene;
        fade(callback);

    }

    void coLoadScene()
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void AddScene(string sceneName, object data = null)
    {
        this.data = data;
        this.sceneName = sceneName;
        callback = coAddScene;
        callback();
    }

    void coAddScene()
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    public static void CloseScene(string sceneName)
    {
        SceneManager.UnloadScene(sceneName);
    }

    void fade(callbackLoadScene callback)
    {
        m_imgFade.DOFade(1, .5f).OnComplete(() =>
        {
            callback();
            m_imgFade.DOFade(0, .5f);
        });
    }

}
