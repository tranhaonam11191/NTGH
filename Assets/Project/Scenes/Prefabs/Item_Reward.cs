using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SS.UI;

public class Item_Reward : MonoBehaviour
{
    [SerializeField] CreateItemReward itemReward;

    [SerializeField] Image m_imgLock, m_imgUpdate, m_img;

    [SerializeField] TextMeshProUGUI m_textPrice;

    [SerializeField] Image m_isUsing;

    bool isUnlock;

    Button m_btn;

    private void Start()
    {
        m_btn = GetComponent<Button>();
        Setup();
    }

    public void Setup()
    {
        m_isUsing.gameObject.SetActive(false);

        if (itemReward != null)
        {
            m_img.sprite = itemReward.ItemReward.Sprite;

            m_imgLock.gameObject.SetActive(!PlayerPrefs.HasKey(itemReward.ItemReward.ID));

            m_textPrice.text = itemReward.ItemReward.Price + "$";

            isUnlock = PlayerPrefs.HasKey(itemReward.ItemReward.ID);

            if (PlayerPrefs.HasKey(Const.CURRENTWEAPON))
            {
                if (PlayerPrefs.GetString(Const.CURRENTWEAPON) == itemReward.ItemReward.ID)
                {
                    onSelected();
                }
            }
        }

        m_btn.interactable = (itemReward != null);

        m_imgUpdate.gameObject.SetActive(itemReward == null);
    }

    public void onClick()
    {

        if (!isUnlock)
        {
            if (itemReward != null)
            {                
                GameManager.instance.data = itemReward;
                SS.UI.Core.Load<PopupYesNo>("PopupYesNo", LoadSceneMode.Additive);
            }
        }
        else
        {
            if (itemReward.ItemReward.itemMode == ItemMode.Weapon)
            {
                if (ShopWeapon.ItemCurrent == null || ShopWeapon.ItemCurrent != this)
                {
                    if (ShopWeapon.ItemCurrent != null)
                    {
                        ShopWeapon.ItemCurrent.onDeSelected();
                    }
                    onSelected();
                }
                else if (ShopWeapon.ItemCurrent == this)
                {
                    onDeSelected();
                    PlayerPrefs.DeleteKey(Const.CURRENTWEAPON);
                }
            }

        }

    }

    public void onSelected()
    {
        ShopWeapon.ItemCurrent = this;
        m_isUsing.gameObject.SetActive(true);
        PlayerPrefs.SetString(Const.CURRENTWEAPON, itemReward.ItemReward.ID);
    }

    public void onDeSelected()
    {
        m_isUsing.gameObject.SetActive(false);
        ShopWeapon.ItemCurrent = null;
    }
}
