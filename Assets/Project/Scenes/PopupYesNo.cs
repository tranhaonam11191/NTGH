using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SS.UI;

public class PopupYesNo : PopupBase
{
    public static PopupYesNo instance;

    private void Awake()
    {
        instance = this;
    }

    CreateItemReward Data;

    [SerializeField] Button m_btnMoney, m_btnAds;

    public void Setup(CreateItemReward data)
    {
        User.EnsureLoaded();
        Data = data;

        m_btnMoney.interactable = Data.ItemReward.Price <= User.data.gold;

        m_btnAds.interactable = AdsController.instance.checkReward();
    }

    public void OnClickBuyItemByMoney()
    {
        User.EnsureLoaded();
        if (!User.data.TrySpendGold(Data.ItemReward.Price))
        {
            return;
        }
        Callback();
    }

    public void OnClickBuyItemByAds()
    {
        AdsController.instance.UserChoseToWatchAd(Callback);
    }

    void Callback()
    {
        User.EnsureLoaded();
        User.data.UnlockItem(Data.ItemReward.ID);
        User.data.SetCurrentWeapon(Data.ItemReward.ID);
        User.Save();
        ShopWeapon.instance.Setup();
        OnClickClose();
    }

    void buySucced()
    {
        SS.UI.Core.Load<SceneComplete>("SceneComplete", LoadSceneMode.Additive);
    }
}
