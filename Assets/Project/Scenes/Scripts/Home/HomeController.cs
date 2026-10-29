using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using SS.UI;

public class HomeController : MonoBehaviour
{
    public static HomeController instance;

    private void Awake()
    {
        instance = this;
    }
    [SerializeField] TextMeshProUGUI m_textGold, m_textExp;

    public void Start()
    {        
        SetData();            

        //AdsController.instance.RequestBanner();
    }

    public void SetData()
    {
        m_textExp.text = PlayerPrefs.HasKey(Const.EXP) ? PlayerPrefs.GetInt(Const.EXP).ToString() : "0";

        m_textGold.text = PlayerPrefs.HasKey(Const.EXP) ? PlayerPrefs.GetInt(Const.GOLD).ToString() : "0";
    }

    public void onCLickPlay()
    {        
        SS.UI.Core.Load<StoryController>("Story");
    }

    public void OnClickWeapon()
    {        
        SS.UI.Core.Load<ShopWeapon>("ShopWeapon", LoadSceneMode.Additive);
    }

    public void OnClickMap()
    {

    }

    public void OnCLickPeople()
    {

    }

    [SerializeField] SoundSettings _soundSettings;

    public void OnClickSettings()
    {
        _soundSettings.Setup();
    }
}
