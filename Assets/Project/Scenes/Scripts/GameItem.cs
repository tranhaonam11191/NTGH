using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum itemMode
{
    Attack, Health, Star, Level, Food, Skill, None
}

[Serializable]
public class GameItemData
{
    public int x, y, id;
}
public class GameItem : MonoBehaviour
{
    public bool isComplete = false;

    public Transform posItem;

    public Item Item
    {
        get
        {
            return posItem.GetComponentInChildren<Item>();
        }
    }

    public GameItemData gameItemData;

    [SerializeField] GameObject ImageIsSelected;

    GameController gameController;

    List<GameItem> lstVertical = new List<GameItem>();
    private void Start()
    {
        gameController = GameController.instance;
    }
    float speedMove;
    public void Setup(int x, int y, int id, string index, float speedMove)
    {
        this.speedMove = speedMove;
        gameController = GameController.instance;
        Item item = Instantiate(gameController.arrGameItem[UnityEngine.Random.Range(0, gameController.arrGameItem.Length)]);
        setupItem(item);
        ImageIsSelected.SetActive(false);
        gameItemData.x = x;
        gameItemData.y = y;
        gameItemData.id = id;
        name = "" + index + x + y + id;
    }

    void setupItem(Item item)
    {
        item.GetComponent<RectTransform>().SetParent(posItem);
        item.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        item.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        item.GetComponent<RectTransform>().localPosition = Vector2.zero;
        item.GetComponent<RectTransform>().localScale = Vector2.one;
        item.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        item.GetComponent<RectTransform>().offsetMin = Vector2.zero;
    }
    [SerializeField] int lstindex;
    public void SetLst(List<GameItem> lst)
    {
        lstVertical = lst;
        lstindex = lstVertical.IndexOf(this);
    }


    public void OnClick()
    {

        if (GameController.canClick)
        {
            if (GameController.CurrentItem1 == null)
            {
                Debug.Log("GameController.CurrentItem1");
                GameController.CurrentItem1 = this;
                GameController.CurrentItem1.Selected();
            }
            else
            {
                Debug.Log("!GameController.CurrentItem1");
                if (GameController.CurrentItem1 == this)
                {
                    Debug.Log("GameController.CurrentItem1 == this");
                    GameController.CurrentItem1.UnSelected();
                    GameController.CurrentItem1 = null;
                }
                else
                {
                    Debug.Log("GameController.CurrentItem1 != this");
                    GameController.CurrentItem2 = this;
                    GameController.instance.switchItem(GameController.CurrentItem1, GameController.CurrentItem2);
                }
            }
        }
    }

    public void Selected()
    {
        ImageIsSelected.SetActive(true);
    }

    public void UnSelected()
    {
        ImageIsSelected.SetActive(false);
    }


    [SerializeField] GameObject m_textObj;


    public int valueFail, valueUp;
    public void OnComplete()
    {
        isComplete = true;
        Item.GetComponent<Image>().enabled = false;
        valueUp = 0;
        //if (lstVertical.IndexOf(this) != 0)
        //{
        //checkFront();
        //MoveUp();
        //}
        //else
        //{
        //    createNew();
        //}
    }

    public void checkFront()
    {
        int index = lstVertical.IndexOf(this);
        if (index != 0)
        {
            for (int i = index; i >= 0; i--)
            {
                GameItem item = lstVertical[i];
                if (!item.isComplete)
                {
                    if (!gameController.lstMove.Contains(item))
                    {
                        item.valueFail = 0;
                        gameController.lstMove.Add(item);
                    }
                    valueUp++;
                    item.valueFail++;
                }
            }
        }
    }
    public void SetTargetUp()
    {
        int index = lstVertical.IndexOf(this) - valueUp;
        GameItem item = lstVertical[index];
        target = item.transform.position;
    }

    public void MoveUp()
    {
        if (isComplete)
        {
            //if (lstVertical.IndexOf(this) > 0)
            //{

            int index = lstVertical.IndexOf(this) - valueUp;
            if (index >= 0)
            {
                GameItem item = lstVertical[index];
                //if (index == 0 && item != this)
                //{
                if (!item.Equals(this))
                {
                    Item thisItme = posItem.GetComponentInChildren<Item>();
                    thisItme.transform.SetParent(item.posItem);
                    thisItme.transform.DOMove(item.transform.position, .5f).OnComplete(() =>
                    {
                        isComplete = false;
                        gameController.lstOK.Remove(this);
                    });
                }
                else
                {
                    StartCoroutine(coMoveUp());
                }
                item.createNew(.5f);
            }
        }
    }

    IEnumerator coMoveUp()
    {
        yield return new WaitForSeconds(.5f);
        isComplete = false;
        gameController.lstOK.Remove(this);
    }



    [SerializeField] Transform posCreateNew;

    bool isCreateNew;
    public void createNew(float time)
    {
        isCreateNew = true;
        StartCoroutine(coCreateNew(time));
    }

    IEnumerator coCreateNew(float time)
    {
        yield return new WaitForSeconds(time);

        valueUp = 0;
        //valueFail = 0;
        Item item = Instantiate(gameController.arrGameItem[UnityEngine.Random.Range(0, gameController.arrGameItem.Length)], posCreateNew);
        item.transform.localPosition = Vector3.zero;
        item.GetComponent<RectTransform>().sizeDelta = item.transform.parent.GetComponent<RectTransform>().sizeDelta;
        item.transform.DOMove(posItem.position, .5f).OnComplete(() =>
        {

            Debug.Log("Item1: " + Item.item);
            setupItem(item);
            item.transform.SetAsFirstSibling();
            Debug.Log("Item2: " + Item.item);
            // Destroy(posItem.transform.GetChild(1).GetComponent<Item>());
            Destroy(posItem.transform.GetChild(1).gameObject);
            isComplete = false;
            gameController.lstOK.Remove(this);
            if (gameController.lstOK.Count == 0 && gameController.lstMove.Count == 0)
            {
                StartCoroutine(coCheckAll());
            }            
            isCreateNew = false;
        });
    }

    IEnumerator coCheckAll()
    {
        yield return new WaitForSeconds(.5f);
        gameController.checkAll();
    }

    public Vector3 target;
    public void SetTarget()
    {
        int index = lstVertical.IndexOf(this) + valueFail;
        GameItem item = lstVertical[index];
        target = item.transform.position;

    }

    public void MoveDown()
    {
        Item thisItme = posItem.GetComponentInChildren<Item>();
        //if (thisItme == null)
        //{
        int index = lstVertical.IndexOf(this) + valueFail;
        if (index < 8)
        {

            GameItem item = lstVertical[index];

            Debug.Log("name: " + name + "Item: " + item + "thisItem: " + thisItme);

            thisItme.transform.SetParent(item.posItem);
            thisItme.transform.DOMove(item.transform.position, .5f).OnComplete(() =>
            {
                valueFail = 0;
                gameController.lstMove.Remove(this);
            });
        }
        else
        {
            Debug.LogError("index: " + index + " name: " + name);
        }
    }

}


