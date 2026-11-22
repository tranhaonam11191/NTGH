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

    [Header("Item Catalogs (ScriptableObjects)")]
    [SerializeField] private ItemCatalogSO m_outfitCatalog;
    [SerializeField] private ItemCatalogSO m_houseCatalog;
    [SerializeField] private ItemCatalogSO m_weaponCatalog;

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
        if (m_weaponCatalog != null)
        {
            ItemSelectPopupController.Open(
                m_weaponCatalog,
                onItemSelected: HandleWeaponSelected,
                isEquipped: item => User.data != null && User.data.current_weapon_id == item.Id,
                isUnlocked: item => User.data != null && User.data.IsItemUnlocked(item.Id),
                labelOverride: "VŨ KHÍ",
                callerCanvas: GetComponentInChildren<Canvas>());
            return;
        }

        SS.UI.Core.Load<ShopWeapon>("ShopWeapon", LoadSceneMode.Additive);
    }

    public void OnClickEvent()
    {
        Debug.Log("[HomeController] OnClickEvent");
    }

    public void OnClickOutfit()
    {
        if (m_outfitCatalog == null)
        {
            Debug.LogWarning("[HomeController] Outfit catalog is not assigned.");
            return;
        }

        ItemSelectPopupController.Open(
            m_outfitCatalog,
            onItemSelected: HandleOutfitSelected,
            isEquipped: item => Outfits != null && Outfits.CurrentOutfitId == item.Id,
            isUnlocked: item => Outfits != null && Outfits.IsUnlocked(item.Id),
            labelOverride: "TRANG PHỤC",
            callerCanvas: GetComponentInChildren<Canvas>());
    }

    public void OnClickHouse()
    {
        if (m_houseCatalog == null)
        {
            Debug.LogWarning("[HomeController] House catalog is not assigned.");
            return;
        }

        ItemSelectPopupController.Open(
            m_houseCatalog,
            onItemSelected: HandleHouseSelected,
            isEquipped: IsHouseEntryEquipped,
            isUnlocked: IsHouseEntryUnlocked,
            labelOverride: "NHÀ CỬA",
            callerCanvas: GetComponentInChildren<Canvas>());
    }

    private void HandleOutfitSelected(SelectableItemEntry item)
    {
        if (item == null || Outfits == null) return;
        Outfits.UnlockAndEquipById(item.Id);
        SetData();
    }

    private void HandleHouseSelected(SelectableItemEntry item)
    {
        if (item == null || m_houseTheme == null) return;
        if (TryParseHousePayload(item.Payload, out HomeHouseTheme.Part part, out HomeHouseTheme.Level level))
        {
            m_houseTheme.SetPartLevel(part, level);
            SetData();
        }
    }

    private void HandleWeaponSelected(SelectableItemEntry item)
    {
        if (item == null) return;
        User.EnsureLoaded();
        User.data.UnlockItem(item.Id);
        User.data.SetCurrentWeapon(item.Id);
        User.Save();
        SetData();
    }

    private bool IsHouseEntryEquipped(SelectableItemEntry item)
    {
        if (item == null || m_houseTheme == null) return false;
        return TryParseHousePayload(item.Payload, out HomeHouseTheme.Part part, out HomeHouseTheme.Level level)
               && m_houseTheme.GetPartLevel(part) == level;
    }

    private bool IsHouseEntryUnlocked(SelectableItemEntry item)
    {
        if (item == null || m_houseTheme == null) return false;
        return TryParseHousePayload(item.Payload, out HomeHouseTheme.Part part, out HomeHouseTheme.Level level)
               && m_houseTheme.IsPartLevelUnlocked(part, level);
    }

    private static bool TryParseHousePayload(string payload, out HomeHouseTheme.Part part, out HomeHouseTheme.Level level)
    {
        part = HomeHouseTheme.Part.Awning;
        level = HomeHouseTheme.Level.Dilapidated;
        if (string.IsNullOrEmpty(payload)) return false;

        string[] tokens = payload.Split(':');
        if (tokens.Length != 2) return false;

        if (!System.Enum.TryParse(tokens[0], out part)) return false;
        if (!int.TryParse(tokens[1], out int lvlInt)) return false;

        level = (HomeHouseTheme.Level)Mathf.Clamp(lvlInt, (int)HomeHouseTheme.Level.Dilapidated, (int)HomeHouseTheme.Level.Renovated);
        return true;
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
