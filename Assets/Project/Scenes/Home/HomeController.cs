using NTGH.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using SS.UI;

public class HomeController : MonoBehaviour
{
    public static HomeController instance;

    [SerializeField] private HomeOutfitView m_outfitView;
    [SerializeField] private HomeHouseTheme m_houseTheme;
    [SerializeField] private CurrencyDisplayView m_currencyDisplay;
    public HomeHouseTheme HouseTheme => m_houseTheme;
    public OutfitProgression Outfits => m_outfitView != null ? m_outfitView.Progression : null;

    private void Awake()
    {
        instance = this;
        SS.UI.Core.Init();
    }
    [SerializeField] TextMeshProUGUI m_textGold, m_textExp;

    public void Start()
    {
        if (Outfits != null)
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
        User.EnsureLoaded();

        if (m_textExp != null)
            m_textExp.text = User.data.exp.ToString();

        int gold = User.data.gold;
        if (m_currencyDisplay != null)
            m_currencyDisplay.SetAmount(gold);
        else if (m_textGold != null)
            m_textGold.text = gold.ToString("N0");
    }
    public void onCLickPlay()
    {
        SOManager.instance.PlayCurrentLevel();
    }

    public void OnClickWeapon()
    {
        SS.UI.Core.Load<ShopWeapon>("ShopWeapon", LoadSceneMode.Additive);
    }

    public void OnClickEvent()
    {
        Debug.Log("[HomeController] OnClickEvent");
    }

    public void OnClickOutfit()
    {
        OpenCustomizePopup(HomeCustomizeTab.Outfit);
    }

    public void OnClickHouse()
    {
        OpenCustomizePopup(HomeCustomizeTab.House);
    }

    private void OpenCustomizePopup(HomeCustomizeTab initialTab)
    {
        SS.UI.Core.Init();

        Canvas homeCanvas = GetComponentInChildren<Canvas>();
        Canvas ssCanvas = SS.UI.Core.Canvas;
        if (homeCanvas != null && ssCanvas != null && ssCanvas.sortingOrder <= homeCanvas.sortingOrder)
        {
            ssCanvas.sortingOrder = homeCanvas.sortingOrder + 10;
        }

        SS.UI.Core.Add<HomeCustomizePopupController>(
            "HomeCustomizePopup",
            showAnimation: "ScaleShow",
            hideAnimation: "ScaleHide",
            useExistingScreen: true,
            onScreenLoad: popup => popup.Setup(this, initialTab),
            hasShield: true,
            hideTopScreen: false);
    }

    public void OnClickAddCurrency()
    {
        Debug.Log("[HomeController] OnClickAddCurrency");
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
        if (_soundSettings != null)
            _soundSettings.Setup();
    }
}
