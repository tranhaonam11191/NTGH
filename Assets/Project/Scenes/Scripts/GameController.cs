using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public delegate void callback();

public class DataForSceneGame
{
    public Character character_User;

    public Character character_Enemy;

    public ItemReward itemReward;

    public DataForSceneGame(Character character_User, Character character_Enemy, ItemReward itemReward)
    {
        this.character_Enemy = character_Enemy;
        this.character_User = character_User;
        this.itemReward = itemReward;
    }
}

[Serializable]
public class CharacterOnGame
{
    public Image m_Mana;

    public Image m_imgAvatar;

    public Image m_Health;

    public Image m_Skill;

    public Image m_timeToAttack;

    public ParticleSystem m_effHit;

    public void Setup(Character character)
    {
        m_imgAvatar.sprite = character.avatar;

        m_Skill.fillAmount = 0;

        if (m_timeToAttack != null)
        {
            m_timeToAttack.fillAmount = 1;
        }
    }
}

public class GameController : MonoBehaviour
{
    public static GameController instance;

    [SerializeField] float speedMove;

    private void Awake()
    {
        instance = this;
    }
    [SerializeField] GameItem gameItemBase;
    public Item[] arrGameItem;
    [SerializeField] Transform m_content;

    public static GameItem CurrentItem1;
    public static GameItem CurrentItem2;

    public List<GameItem> lstGameItem = new List<GameItem>();

    public List<GameItem> lstGameCompleteHorizontal = new List<GameItem>();
    public List<GameItem> lstGameItemCompleteVertical = new List<GameItem>();

    public List<List<GameItem>> allListItemVertical = new List<List<GameItem>>();
    public List<List<GameItem>> allListItemHorizontal = new List<List<GameItem>>();


    public static bool moving;

    public static bool canClick;

    bool GameOver;

    DataForSceneGame Data;

    [SerializeField] CharacterOnGame Player, Enemy;

    [SerializeField] TextMeshProUGUI m_textGold, m_textEXP, m_textLEVEL;

    //Effect
    [Header("Effect")]
    [SerializeField] ParticleSystem m_effAttack;
    [SerializeField] ParticleSystem m_effHealth;
    [SerializeField] ParticleSystem m_effExp;
    [SerializeField] ParticleSystem m_effMana;

    [Header("Weapon")]
    [SerializeField] Image m_WeaponImage;
    CreateItemReward weapon;
    
    // Start is called before the first frame update
    IEnumerator Start()
    {
        if(GameManager.instance == null)
        {
            SceneManager.LoadScene("Main");
            yield return null;
        }
        Data = (DataForSceneGame)GameManager.instance.data;
        if (Data == null)
        {
            SceneManager.LoadScene("Main");
            yield return null;
        }

        canClick = true;
        moving = false;

        SpecialAttack.SetActive(false);
        Gun.SetActive(false);
        //Setup();
        SetupCharacter();
        SetupLevel();
        SetupGold();
        SetupWeapon();
        yield return new WaitForSeconds(.5f);
        FindObjectOfType<GridLayoutGroup>().enabled = false;
        checkAll();

        //foreach (GameItem item in lstGameItem)
        //{
        //    item.CheckItemComplete();
        //}
        //StartCoroutine(fallingItem(1f));        
    }

    void SetupWeapon()
    {
        weapon = GameManager.instance.GetWeapon();
        if(weapon != null)
        {
            m_WeaponImage.sprite = weapon.ItemReward.Sprite;
        }
    }

    int valueFillAmount;

    void SetupLevel()
    {
        //m_textLEVEL.text = GameManager.Level;
        m_textEXP.text = Exp.ToString();                
    }

    int gold;

    void SetupGold()
    {
        m_textGold.text = gold.ToString();
    }


    private void Update()
    {
        if(!endGame)
        {
            if (Data != null)
            {
                EnemyAttack();
            }                
        }        
    }

    [SerializeField] WeaponScripts enemyWeapon, playerWeapon;

    void EnemyAttack()
    {
        float fillAmount = Enemy.m_timeToAttack.fillAmount;
        if (fillAmount > 0)
        {
            Enemy.m_timeToAttack.fillAmount -= .001f;
        }
        else
        {
            if(Attack(Player, Data.character_Enemy.attack, enemyWeapon, Data.character_Enemy.health))
            {
                if (!endGame)
                {
                    endGame = true;
                    GameManager.instance.AddScene("Popup", new PopupData(resultType.lose, gold, Exp));                    
                }
            }
            Enemy.m_timeToAttack.fillAmount = 1;
        }
    }

    #region oldCode
    void Setup()
    {
        for (int j = 0; j < 8; j++)
        {
            List<GameItem> lstGameItemHorizontal = new List<GameItem>();
            for (int i = 0; i < 8; i++)
            {
                GameItem item = Instantiate(gameItemBase, m_content);
                item.Setup(j + 1, i + 1, i, i + " " + j, speedMove);
                lstGameItem.Add(item);
                lstGameItemHorizontal.Add(item);
            }
            allListItemHorizontal.Add(lstGameItemHorizontal);
        }
        for (int j = 0; j < 8; j++)
        {
            List<GameItem> lstGameItemVertical = new List<GameItem>();
            for (int i = j; i <= j + 56; i += 8)
            {
                GameItem item = lstGameItem[i];
                lstGameItemVertical.Add(item);
                item.SetLst(lstGameItemVertical);
            }
            allListItemVertical.Add(lstGameItemVertical);
        }
    }

    void SetupCharacter()
    {
        Player.Setup(Data.character_User);

        Enemy.Setup(Data.character_Enemy);
    }

    public List<GameItem> lstOK = new List<GameItem>();


    public void checkAll()
    {
        lstOK.Clear();
        lstMove.Clear();
        checkHorizontal();
        checkVertical();
        Setcomplete();
    }
    public void checkHorizontal()
    {
        List<GameItem> lst = new List<GameItem>(checkList(allListItemHorizontal));
        Debug.Log("Horizontal.count: " + lst.Count);
        foreach (GameItem item in lst)
        {
            if (!lstOK.Contains(item))
            {
                lstOK.Add(item);
            }
        }

    }

    public void checkVertical()
    {
        List<GameItem> lst = new List<GameItem>(checkList(allListItemVertical));
        Debug.Log("Vertical.count: " + lst.Count);
        foreach (GameItem item in lst)
        {
            if (!lstOK.Contains(item))
            {
                lstOK.Add(item);
            }
        }
    }

    public List<GameItem> checkList(List<List<GameItem>> LstlstgameItem)
    {
        List<GameItem> lstReturn = new List<GameItem>();

        foreach (List<GameItem> lstGameItem in LstlstgameItem)
        {
            List<GameItem> _lst = new List<GameItem>();
            for (int i = 0; i < lstGameItem.Count - 2; i++)
            {
                GameItem gameItem = lstGameItem[i];
                GameItem gameItem2 = lstGameItem[i + 1];
                GameItem gameItem3 = lstGameItem[i + 2];
                if (gameItem.Item.item == gameItem2.Item.item && gameItem2.Item.item == gameItem3.Item.item)
                {
                    if (!_lst.Contains(gameItem))
                    {
                        _lst.Add(gameItem);
                    }
                    if (!_lst.Contains(gameItem2))
                    {
                        _lst.Add(gameItem2);
                    }
                    if (!_lst.Contains(gameItem3))
                    {
                        _lst.Add(gameItem3);
                    }
                }
            }
            if (_lst.Count >= 3)
            {
                foreach (GameItem item in _lst)
                {
                    if (!lstReturn.Contains(item))
                    {
                        lstReturn.Add(item);
                    }
                    else
                        Debug.LogError("lstreturn.contains item");
                }
            }
        }

        return lstReturn;
    }

    public List<GameItem> lstMove = new List<GameItem>();
    public void Setcomplete()
    {
        if (lstOK.Count >= 3)
        {
            canClick = false; Debug.Log("canClick false: " + canClick);
            foreach (GameItem item in lstOK)
            {
                item.OnComplete();
            }
            foreach (GameItem item in lstOK)
            {
                item.checkFront();
            }
            foreach (GameItem item in lstMove)
            {
                item.MoveDown();
            }
            foreach (GameItem item in lstOK)
            {
                item.MoveUp();
            }
            //StartCoroutine(coMoveDown());            
        }
        else
        {
            canClick = true; Debug.Log("canClick true: " + canClick);
        }

        //lstMove.Clear();
        //lstOK.Clear();
    }
    IEnumerator coMoveDown()
    {
        yield return new WaitForSeconds(.25f);
        foreach (GameItem item in lstMove)
        {
            item.MoveDown();
        }
        StartCoroutine(coMoveUp());
    }

    IEnumerator coMoveUp()
    {
        yield return new WaitForSeconds(.25f);
        foreach (GameItem item in lstOK)
        {
            item.MoveUp();
        }
    }


    public void switchItem(GameItem item1, GameItem item2)
    {
        if (checkSwitch(item1, item2))
        {
            canClick = false;
            moving = true;
            Item _item1 = item1.Item;
            _item1.transform.SetParent(item2.posItem);
            _item1.transform.DOMove(item2.transform.position, .5f);
            Item _item2 = item2.Item;
            _item2.transform.SetParent(item1.posItem);
            _item2.transform.DOMove(item1.transform.position, .5f).OnComplete(() =>
            {
                moving = false;

                lstOK.Clear();
                checkHorizontal();
                checkVertical();

                Debug.Log("lstOK: " + lstOK.Count);
                if (lstOK.Count < 3)
                {
                    moving = true;
                    _item1.transform.SetParent(item1.posItem);
                    _item1.transform.DOMove(item1.transform.position, speedMove);
                    _item2.transform.SetParent(item2.posItem);
                    _item2.transform.DOMove(item2.transform.position, speedMove).OnComplete(() =>
                    {
                        canClick = true;
                        moving = false;
                        lstOK.Clear();
                        lstMove.Clear();
                    });

                }
                else
                {
                    moving = false;
                    Setcomplete();
                }

                CurrentItem1 = CurrentItem2 = null;
            }
            );
        }
        else
        {
            canClick = true;
        }
    }

    public int rowCheckFalling = 64;




    public bool checkSwitch(GameItem item1, GameItem item2)
    {
        if (item1.gameItemData.x == item2.gameItemData.x)
        {
            return true;
        }
        if (item1.gameItemData.y == item2.gameItemData.y)
        {
            return true;
        }
        return false;
    }

    public List<GameItem> lstCompleteAll = new List<GameItem>();
    int j = 0;

    #endregion
    [SerializeField] string _attackAudio;

    public bool Attack(CharacterOnGame character, int damage, WeaponScripts weapon, int health, bool SpecialEff = false)
    {
        if(!SpecialEff)
        {
            m_effAttack.Play();
            SoundManager.instance.PlaySfx(_attackAudio);            
        }        

        weapon.Move(character.m_imgAvatar.transform.position);

        character.m_Health.DOFillAmount(character.m_Health.fillAmount - (damage * (1 / (float)health)), 1f);

        return character.m_Health.fillAmount <= 0;
    }

    int Gold, Exp;

    bool endGame;

    #region PlayerAction
    public void PlayerAttack(bool specialAttack = false)
    {
        if(Attack(Enemy, Data.character_User.attack, playerWeapon, Data.character_User.health, specialAttack))
        {
            if(!endGame)
            {
                endGame = true;
                Debug.Log("Gold Win: " + gold);
                GameManager.instance.AddScene("Popup", new PopupData(resultType.win, gold, Exp));

                if(AdsController.instance.checkReward() && Data.itemReward != null)
                {
                    StartCoroutine(coPopupReward());
                }                
            }            
        }
    }

    IEnumerator coPopupReward()
    {
        yield return new WaitForSeconds(.25f);
        Debug.Log("Item Name: " + Data.itemReward.Name);
        GameManager.instance.AddScene("PopupReward", Data.itemReward);
    }

    [SerializeField] string _increaseAudio;
    public void PlayerInCreaseHealth()
    {
        if(!endGame)
        {
            m_effHealth.Play();
            SoundManager.instance.PlaySfx(_increaseAudio);            
            Player.m_Health.DOFillAmount(Player.m_Health.fillAmount + .01f, 1f);
        }        
    }

    [SerializeField] string _manaAudio;
    public void PlayerInCreaseMana()
    {
        if(!endGame)
        {
            m_effMana.Play();
            SoundManager.instance.PlaySfx(_manaAudio);            
            float value = Player.m_Mana.fillAmount + .1f;
            Debug.Log("Mana: " + value);
            Player.m_Mana.DOFillAmount(value, 1f).OnComplete(() =>
            {
                if (Player.m_Mana.fillAmount >= 1)
                {
                    Debug.Log("FullMana");
                    Player.m_Mana.fillAmount = 0;
                    SkillSpecial();
                }
            });
        }                
    }

    public void PlayerInCreaseGold()
    {
        if(!endGame)
        {
            gold += 10;
            SetupGold();
        }        
    }
    [SerializeField] string _effExpAudio;
    public void PlayerInCreaseEXP()
    {
        if(!endGame)
        {
            m_effExp.Play();
            SoundManager.instance.PlaySfx(_effExpAudio);            
            Exp += 1;
            SetupLevel();
        }                
    }

    [SerializeField] GameObject SpecialAttack;

    [SerializeField] GameObject Gun;

    bool isPlayingSpecialSkill;

    public void SkillSpecial()
    {
        if(!isPlayingSpecialSkill)
        {            
            isPlayingSpecialSkill = true;
            Vector3 oldPosImage = SpecialAttack.transform.localPosition;
            SpecialAttack.transform.localPosition = new Vector3(SpecialAttack.transform.localPosition.x + 20, SpecialAttack.transform.localPosition.y, SpecialAttack.transform.localPosition.z);
            SpecialAttack.SetActive(true);
            SpecialAttack.transform.DOLocalMove(oldPosImage, 1f).SetEase(Ease.InOutBack).OnComplete(() =>
            {
                StartCoroutine(coSkillSpecial());
            });
        }        
    }

    IEnumerator coSkillSpecial()
    {
        yield return new WaitForSeconds(3f);
        SpecialAttack.SetActive(false);
        Gun.SetActive(true);
        PlayerAttack(true);
        PlayerAttack(true);
        PlayerAttack(true);
        StartCoroutine(coEndSpecial());
    }

    IEnumerator coEndSpecial()
    {
        yield return new WaitForSeconds(2f);
        Gun.SetActive(false);
        isPlayingSpecialSkill = false;
    }

    #endregion

}


public enum typeShape
{
    Attack, Health, Mana, Gold, Exp
}