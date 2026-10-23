using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public enum ItemMode
{
    Character, Weapon
}

[Serializable]
public class ItemReward
{
    public string Name;
    public string ID;
    public Sprite Sprite;
    public ItemMode itemMode;
    public int Price;
}

internal class SerilizeAbleAttribute : Attribute
{
}

public class PopupReward : PopupBase
{
    [SerializeField] TextMeshProUGUI m_textName;

    [SerializeField] Image m_img;    

    ItemReward data;

    public override void Start()
    {
        base.Start();
        data = (ItemReward)GameManager.instance.data;
        m_textName.text = data.Name;
        m_img.sprite = data.Sprite;
    }

    public void OnClickAds()
    {
        AdsController.instance.UserChoseToWatchAd(callbackOnCompleteAds);
    }

    public void callbackOnCompleteAds()
    {
        Debug.Log("OnCompleteCallback ads");
        PlayerPrefs.SetString(data.ID, data.ID);
        OnClickClose();
    }

    public void OnClickHome()
    {
        GameManager.instance.LoadScene("Home");
    }
}
