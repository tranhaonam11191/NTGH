using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PopupYesNo : PopupBase
{
    public static PopupYesNo instance;

    private void Awake()
    {
        instance = this;
    }

    CreateItemReward Data;

    [SerializeField] Button m_btnMoney, m_btnAds;

    private void Start()
    {
        Data = (CreateItemReward)GameManager.instance.data;

        m_btnMoney.interactable = Data.ItemReward.Price <= PlayerPrefs.GetInt(Const.GOLD);

        m_btnAds.interactable = AdsController.instance.checkReward();
    }

    public void OnClickBuyItemByMoney()
    {        
        int gold = PlayerPrefs.GetInt(Const.GOLD);
        gold -= Data.ItemReward.Price;
        PlayerPrefs.SetInt(Const.GOLD, gold);
        Callback();
    }

    public void OnClickBuyItemByAds()
    {
        AdsController.instance.UserChoseToWatchAd(Callback);
    }

    void Callback()
    {
        PlayerPrefs.SetString(Data.ItemReward.ID, Data.ItemReward.ID);
        PlayerPrefs.SetString(Const.CURRENTWEAPON, Data.ItemReward.ID);
        ShopWeapon.instance.Setup();
        OnClickClose();
    }

    void buySucced()
    {
        GameManager.instance.AddScene("SceneComplete");
    }
}
