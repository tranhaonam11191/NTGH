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
            User.EnsureLoaded();
            string itemId = itemReward.ItemReward.ID;

            m_img.sprite = itemReward.ItemReward.Sprite;

            isUnlock = User.data.IsItemUnlocked(itemId);
            m_imgLock.gameObject.SetActive(!isUnlock);

            m_textPrice.text = itemReward.ItemReward.Price + "$";

            if (!string.IsNullOrEmpty(User.data.current_weapon_id) && User.data.current_weapon_id == itemId)
            {
                onSelected();
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
                SS.UI.Core.Load<PopupYesNo>("PopupYesNo", LoadSceneMode.Additive, (popup) => popup.Setup(itemReward));
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
                    User.EnsureLoaded();
                    User.data.SetCurrentWeapon(string.Empty);
                    User.Save();
                }
            }

        }

    }

    public void onSelected()
    {
        ShopWeapon.ItemCurrent = this;
        m_isUsing.gameObject.SetActive(true);
        User.EnsureLoaded();
        if (User.data.current_weapon_id != itemReward.ItemReward.ID)
        {
            User.data.SetCurrentWeapon(itemReward.ItemReward.ID);
            User.Save();
        }
    }

    public void onDeSelected()
    {
        m_isUsing.gameObject.SetActive(false);
        ShopWeapon.ItemCurrent = null;
    }
}
