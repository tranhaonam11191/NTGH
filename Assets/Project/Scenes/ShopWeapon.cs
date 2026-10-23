using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ShopWeapon : PopupBase
{
    public static ShopWeapon instance;

    private void Awake()
    {
        instance = this;
    }

    List<Item_Reward> itemRewards;

    [SerializeField] Transform allItem;

    public override void Start()
    {
        base.Start();
        Setup();
    }

    public static Item_Reward ItemCurrent;

    public void Setup()
    {
        if(itemRewards == null)
        {
            itemRewards = new List<Item_Reward>();
            foreach(Transform a in allItem)
            {
                if(a.GetComponent<Item_Reward>())
                {
                    itemRewards.Add(a.GetComponent<Item_Reward>());
                }
            }
        }

        foreach(Item_Reward item in itemRewards)
        {
            item.Setup();
        }
    }
}
