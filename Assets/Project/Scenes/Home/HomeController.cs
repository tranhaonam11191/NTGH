using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using SS.UI;

public class HomeController : MonoBehaviour
{
    public static HomeController instance;

    [SerializeField] private HomeOutfitView m_outfitView;
    public OutfitProgression Outfits => m_outfitView.Progression;

    private void Awake()
    {
        instance = this;
    }
    [SerializeField] TextMeshProUGUI m_textGold, m_textExp;

    public void Start()
    {        
        Outfits.RankChanged += OnOutfitUpgraded;
        SetData();            

        //AdsController.instance.RequestBanner();
    }

    private void OnOutfitUpgraded(int rank) => SetData();

    private void OnDestroy()
    {
        if (m_outfitView && Outfits != null)
            Outfits.RankChanged -= OnOutfitUpgraded;
    }

    public void SetData()
    {
        m_textExp.text = PlayerPrefs.HasKey(Const.EXP) ? PlayerPrefs.GetInt(Const.EXP).ToString() : "0";

        m_textGold.text = PlayerPrefs.GetInt(Const.GOLD).ToString();
    }
    public void onCLickPlay()
    {
        SOManager.instance.PlayCurrentLevel();
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
