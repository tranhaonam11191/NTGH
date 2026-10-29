using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SS.UI;

public enum resultType
{
    win, lose
}
public class PopupData
{
    public resultType result;
    public int Gold { get; set; }
    public int Exp { get; set; }
    public PopupData(resultType result, int Gold, int Exp)
    {
        this.result = result;
        this.Gold = Gold;
        this.Exp = Exp;
    }
}
public class PopupBase : MonoBehaviour
{
    [SerializeField] protected Transform m_animation;

    [SerializeField] string sceneName;
    public virtual void Start()
    {
        if(m_animation != null)
        {
            m_animation.transform.localScale = Vector3.zero;
            m_animation.transform.DOScale(Vector3.one, .5f).SetEase(Ease.OutBack);
        }
    }

    public virtual void OnClickClose()
    {
        HomeController.instance.SetData();
        if (m_animation != null)
        {
            m_animation.transform.DOScale(Vector3.zero, .5f).SetEase(Ease.InBack).OnComplete(() => SS.UI.Core.Close());
        }
        else
        {
            SS.UI.Core.Close();
        }        
    }
}

public class Popup : PopupBase
{
    public static Popup instance;

    public PopupData Data;

    [SerializeField] TextMeshProUGUI m_textEXP, m_textGOLD, m_textDetail;

    [SerializeField] GameObject m_btnNext, m_btnRetry, m_btnAds;    

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public override void Start()
    {
        base.Start();

        m_btnAds.GetComponent<Button>().interactable = AdsController.instance.checkReward();

        if (GameManager.instance.data != null)
        {
            Data = (PopupData)GameManager.instance.data;

            m_textDetail.text = Data.result == resultType.win ? "YOU WIN" : "YOU LOSE";

            m_btnNext.SetActive(Data.result == resultType.win); m_btnAds.SetActive(m_btnNext.activeInHierarchy);

            m_btnRetry.SetActive(Data.result != resultType.win);

            m_textEXP.color = m_textGOLD.color = (Data.result != resultType.win) ? Color.gray : Color.white; Debug.Log("Gold popup: " + Data.Gold);

            m_textEXP.text = "EXP: " + Data.Exp.ToString();

            m_textGOLD.text = "Money: " + Data.Gold.ToString();
        }
    }


    public void OnCLickNext()
    {
        InCreaseValue();
        SS.UI.Core.Load<StoryController>("Story");

    }

    public void OnClickRetry()
    {
        SS.UI.Core.Load<StoryController>("Story");
    }

    public void OnClickHome()
    {
        InCreaseValue();
        SS.UI.Core.Load<HomeController>("Home");
    }

    void InCreaseValue(int coefficient = 1)
    {
        GameManager.story_num++;        
        Debug.Log("story_num: " + GameManager.story_num);
        PlayerPrefs.SetInt(Const.NUM_STORY, GameManager.story_num);

        int valueEXP = PlayerPrefs.GetInt(Const.EXP) + (Data.Exp * coefficient);
        PlayerPrefs.SetInt(Const.EXP, valueEXP);

        int valueGOLD = PlayerPrefs.GetInt(Const.GOLD) + (Data.Gold * coefficient);
        PlayerPrefs.SetInt(Const.GOLD, valueGOLD);
    }

    public void OnClickWatchAds()
    {
        AdsController.instance.UserChoseToWatchAd(functionCallback);
    }


    void functionCallback()
    {
        m_btnAds.SetActive(false);

        InCreaseValue(2);

        m_textEXP.text = "EXP: " + (Data.Exp * 2).ToString();

        m_textGOLD.text = "Money: " + (Data.Gold * 2).ToString();
    }
}
