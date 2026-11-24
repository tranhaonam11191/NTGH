using DG.Tweening;
using NTGH.Teammates;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SS.UI;

public delegate void callback();

[Serializable]
public class CharacterOnGame
{

    public Image m_imgAvatar;

    public Image m_Health;


    public Image m_timeToAttack;

    public ParticleSystem m_effHit;

    public TextMeshProUGUI m_txtName;

    public TextMeshProUGUI m_txtLevel;

    public TextMeshProUGUI m_txtHealth;


    [Header("Support Loadout UI")]
    public Image m_imgWeaponSlot;

    public Image m_imgOutfitSlot;

    public TeammateCardView[] m_teammateSlots;

    public void ApplyLoadout(BattleLoadout loadout, TeammateCardDatabaseSO database)
    {
        if (loadout == null) return;

        if (m_imgWeaponSlot != null && loadout.WeaponIcon != null)
        {
            m_imgWeaponSlot.sprite = loadout.WeaponIcon;
            m_imgWeaponSlot.color = Color.white;
        }

        if (m_imgOutfitSlot != null && loadout.OutfitIcon != null)
        {
            m_imgOutfitSlot.sprite = loadout.OutfitIcon;
            m_imgOutfitSlot.color = Color.white;
        }

        if (m_teammateSlots != null)
        {
            for (int i = 0; i < m_teammateSlots.Length; i++)
            {
                var slotView = m_teammateSlots[i];
                if (slotView == null) continue;

                TeammateCardSO card = i < loadout.Teammates.Length ? loadout.Teammates[i] : null;
                TeammateCardThemeSO theme = (card != null && database != null) ? database.GetTheme(card.CardType) : null;
                slotView.Bind(card, theme, $"Ô {i + 1}");
            }
        }
    }

    public void Setup(Character character)
    {
        if (m_imgAvatar != null && character.avatar != null)
        {
            m_imgAvatar.sprite = character.avatar;
        }
        if (m_Health != null)
        {
            m_Health.fillAmount = 1f;
        }
        if (m_timeToAttack != null)
        {
            m_timeToAttack.fillAmount = 1f;
        }
        if (m_txtName != null && !string.IsNullOrEmpty(character.Name))
        {
            m_txtName.text = character.Name;
        }
        UpdateHealthText(character.health, character.health);
    }

    public void UpdateAttackTimer(float progress)
    {
        float clamped = Mathf.Clamp01(progress);
        Image timer = EnsureAttackTimer();
        if (timer == null) return;

        if (timer.sprite != null && timer.type == Image.Type.Filled)
        {
            timer.fillAmount = clamped;
        }
        else
        {
            RectTransform rect = timer.rectTransform;
            rect.anchorMax = new Vector2(clamped, rect.anchorMax.y);
        }
    }

    private Image EnsureAttackTimer()
    {
        if (m_timeToAttack != null)
        {
            return m_timeToAttack;
        }

        if (m_imgAvatar == null)
        {
            return null;
        }

        GameObject timerObj = new GameObject("AttackTimer", typeof(RectTransform), typeof(Image));
        RectTransform rect = timerObj.GetComponent<RectTransform>();
        rect.SetParent(m_imgAvatar.rectTransform, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 10f);

        Image timer = timerObj.GetComponent<Image>();
        timer.color = new Color(1f, 0.55f, 0f, 1f);
        timer.raycastTarget = false;

        Sprite barSprite = m_Health != null ? m_Health.sprite : null;
        if (barSprite != null)
        {
            timer.sprite = barSprite;
            timer.type = Image.Type.Filled;
            timer.fillMethod = Image.FillMethod.Horizontal;
            timer.fillOrigin = (int)Image.OriginHorizontal.Left;
            timer.fillAmount = 1f;
        }

        m_timeToAttack = timer;
        return m_timeToAttack;
    }

    public void UpdateHealthText(float currentHp, int maxHp)
    {
        if (m_txtHealth == null) return;
        int cur = Mathf.Max(0, Mathf.CeilToInt(currentHp));
        m_txtHealth.text = $"{FormatNumber(cur)} / {FormatNumber(maxHp)}";
    }

    private static string FormatNumber(int value)
    {
        return value.ToString("N0").Replace(",", ".");
    }
}

public class GameController : MonoBehaviour
{
    public const string SCENE_NAME = "Game";     
    public static GameController instance;

    [SerializeField] float speedMove;

    private void Awake()
    {
        instance = this;
        Core.Init();
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

    LevelData Data;

    [SerializeField] CharacterOnGame Player, Enemy;


    [SerializeField] TextMeshProUGUI m_StageTitle;

    //Effect
    [Header("Effect")]
    [SerializeField] ParticleSystem m_effAttack;

    [Header("Weapon")]
    [SerializeField] Image m_WeaponImage;
    CreateItemReward weapon;
    
    readonly HashSet<LevelConversation> m_Played = new HashSet<LevelConversation>();
    readonly List<ConversationData> m_PendingLines = new List<ConversationData>();
    bool m_ShowingStory;
    float m_PlayerHp, m_EnemyHp;
    int m_PlayerMaxHp, m_EnemyMaxHp;
    BattleLoadout m_PlayerLoadout, m_EnemyLoadout;
    float m_EnemyAttackProgress = 1f;
    bool m_FinalStand;
    bool m_HasClicked;
    float m_SecondsSinceFirstClick;
    resultType m_Result;
    bool m_ResultShown;

    public bool BlocksBoard => Data == null || endGame || m_ShowingStory || m_PendingLines.Count > 0;

    IEnumerator Start()
    {
        // SS supplies the level in its load callback. Also support opening Game directly.
        yield return null;
        if (Data == null && SOManager.instance != null)
            Setup(SOManager.instance.CurrentLevelData, openingPlayed: false, playerLoadout: SOManager.instance.BuildPlayerBattleLoadout());
    }

    public void Setup(LevelData level, bool openingPlayed = false, BattleLoadout playerLoadout = null)
    {
        if (Data != null) return;
        if (level == null || !level.HasCombatants)
        {
            Debug.LogError("Cannot start battle without a level and valid combatants.", this);
            return;
        }
        Data = level;
        m_PlayerLoadout = playerLoadout ?? (SOManager.instance != null ? SOManager.instance.BuildPlayerBattleLoadout() : new BattleLoadout());
        m_EnemyLoadout = SOManager.instance != null ? SOManager.instance.BuildEnemyBattleLoadout(Data.enemy) : new BattleLoadout();

        m_PlayerMaxHp = Mathf.Max(1, Data.defaultPlayer.Character.health + m_PlayerLoadout.OutfitBonusHealth);
        m_EnemyMaxHp = Mathf.Max(1, Data.enemy.Character.health + m_EnemyLoadout.OutfitBonusHealth);
        m_PlayerHp = m_PlayerMaxHp;
        m_EnemyHp = m_EnemyMaxHp;
        canClick = true;
        moving = false;

        m_StageTitle.text = $"STAGE {SOManager.instance.CurrentLevelNumber}\n{Data.displayName}";
        SetupCharacter();
        SetupWeapon();
        if (!openingPlayed) QueueConversations(ConversationTrigger.Start);
        StartCoroutine(PrepareBoard());
    }

    IEnumerator PrepareBoard()
    {
        yield return new WaitForSeconds(.5f);
        var grid = FindObjectOfType<GridLayoutGroup>();
        if (grid != null) grid.enabled = false;
        checkAll();
    }

    void QueueConversations(ConversationTrigger moment, float previous = 0, float current = 0)
    {
        foreach (var group in Data.conversations)
        {
            if (group == null || !group.Matches(moment, previous, current) || !m_Played.Add(group)) continue;
            m_PendingLines.AddRange(group.lines);
        }
    }

    void LateUpdate()
    {
        if (m_ShowingStory) return;
        if (m_PendingLines.Count > 0)
        {
            var lines = m_PendingLines.ToArray();
            m_PendingLines.Clear();
            m_ShowingStory = true;
            StoryController.Show(lines, () => m_ShowingStory = false, showBattleShield: true);
        }
        else if (endGame && !m_ResultShown)
        {
            m_ResultShown = true;
            Core.Load<Popup>("Popup", LoadSceneMode.Additive, popup =>
            {
                popup.Setup(new PopupData(m_Result, 0, 0));
                if (m_Result == resultType.win && Data.itemReward != null &&
                    AdsController.instance != null && AdsController.instance.checkReward())
                    StartCoroutine(coPopupReward());
            });
        }
    }

    void OnDestroy()
    {
        Player.m_Health?.DOKill();
        Enemy.m_Health?.DOKill();
    }

    void SetupWeapon()
    {
        weapon = SOManager.instance.GetWeapon();
        if(weapon != null)
        {
            m_WeaponImage.sprite = weapon.ItemReward.Sprite;
        }
    }

    private void Update()
    {
        if (Data == null || endGame) return;

        if (m_HasClicked)
            m_SecondsSinceFirstClick += Time.deltaTime;
        else if (!BlocksBoard)
            m_HasClicked = Input.GetMouseButtonDown(0);

        if (m_HasClicked)
            QueueConversations(ConversationTrigger.TimeAfterFirstClick, current: m_SecondsSinceFirstClick);

        if (!BlocksBoard) EnemyAttack();
    }

    [SerializeField] WeaponScripts enemyWeapon;
    [SerializeField] Sprite m_MatchSword;

    void EnemyAttack()
    {
        if (m_EnemyAttackProgress > 0f)
        {
            m_EnemyAttackProgress = Mathf.Max(0f, m_EnemyAttackProgress - Time.deltaTime * 0.06f);
        }
        else
        {
            int rawDamage = Data.enemy.Character.attack + (m_EnemyLoadout != null ? m_EnemyLoadout.GetTotalAttackBonus() : 0);
            int reduction = m_PlayerLoadout != null ? m_PlayerLoadout.GetTotalDamageReduction() : 0;
            int finalDamage = Mathf.Max(1, rawDamage - reduction);
            m_effAttack.Play();
            enemyWeapon.Move(Player.m_imgAvatar.transform.position);
            Attack(Player, ref m_PlayerHp, m_PlayerMaxHp,
                finalDamage, ConversationTrigger.PlayerHealth);
            m_EnemyAttackProgress = 1f;
        }

        Enemy?.UpdateAttackTimer(m_EnemyAttackProgress);
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
        Player.Setup(Data.defaultPlayer.Character);
        Enemy.Setup(Data.enemy.Character);

        var db = SOManager.instance != null ? SOManager.instance.TeammateDatabase : null;
        Player.ApplyLoadout(m_PlayerLoadout, db);
        Enemy.ApplyLoadout(m_EnemyLoadout, db);

        Player.UpdateHealthText(m_PlayerHp, m_PlayerMaxHp);
        Enemy.UpdateHealthText(m_EnemyHp, m_EnemyMaxHp);
        Enemy.UpdateAttackTimer(m_EnemyAttackProgress);
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

    void Attack(CharacterOnGame target, ref float hp, int maxHp, int damage,
        ConversationTrigger trigger)
    {
        if (endGame) return;
        SoundManager.instance.PlaySfx(_attackAudio);
        float previous = hp;
        float minimumHp = target == Enemy ? maxHp * Data.finalStandEnemyHealth : 0f;
        hp = Mathf.Clamp(hp - damage, minimumHp, maxHp);
        target.m_Health.DOKill();
        target.m_Health.DOFillAmount(hp / maxHp, 1f);
        target.UpdateHealthText(hp, maxHp);
        QueueConversations(trigger, previous / maxHp, hp / maxHp);
        if (target == Enemy && minimumHp > 0f && hp <= minimumHp && !m_FinalStand)
        {
            m_FinalStand = true;
            m_EnemyAttackProgress = .25f;
            Enemy?.UpdateAttackTimer(m_EnemyAttackProgress);
        }
        bool survivedFinalHit = m_FinalStand && target == Player && hp > 0;
        if (hp > 0 && !survivedFinalHit) return;

        endGame = true;
        m_Result = target == Enemy || survivedFinalHit ? resultType.win : resultType.lose;
        if (m_Result == resultType.win)
        {
            SOManager.instance.AdvanceAfterWin(Data);
            QueueConversations(ConversationTrigger.Victory);
        }
        QueueConversations(ConversationTrigger.End);
    }


    bool endGame;

    #region PlayerAction
    public void AttackWithMatch(int clearedItems, Vector3 boardPosition)
    {
        if (Data == null || endGame || clearedItems < Constants.MinimumMatches) return;

        Image avatar = Enemy.m_imgAvatar;
        Canvas canvas = avatar.canvas.rootCanvas;
        RectTransform layer = (RectTransform)canvas.transform;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenStart = RectTransformUtility.WorldToScreenPoint(Camera.main, boardPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenStart, uiCamera, out Vector2 start);
        Vector2 target = layer.InverseTransformPoint(avatar.rectTransform.TransformPoint(avatar.rectTransform.rect.center));

        var sword = new GameObject("MatchSword", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        sword.rectTransform.SetParent(layer, false);
        sword.sprite = m_MatchSword;
        sword.raycastTarget = false;
        RectTransform rect = sword.rectTransform;
        rect.pivot = new Vector2(0.22f, 0.95f); // Tip in the approved diagonal sprite.
        rect.sizeDelta = new Vector2(64f, 64f);
        rect.localPosition = start;
        float heading = Vector2.SignedAngle(Vector2.right, target - start);
        rect.localRotation = Quaternion.Euler(0f, 0f, heading - 122f);

        bool hit = false;
        DOVirtual.Float(0f, 1f, 0.32f, progress =>
        {
            rect.localPosition = Vector2.LerpUnclamped(start, target, progress);
            if (progress < 1f || hit || endGame) return;
            hit = true;
            Attack(Enemy, ref m_EnemyHp, m_EnemyMaxHp, clearedItems, ConversationTrigger.EnemyHealth);
            ShowMatchDamage(layer, target, clearedItems);
        }).SetEase(Ease.OutBack, 0.9f).SetLink(sword.gameObject)
            .OnComplete(() => Destroy(sword.gameObject));
    }

    private void ShowMatchDamage(RectTransform layer, Vector2 target, int amount)
    {
        var label = new GameObject("MatchDamage", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(layer, false);
        label.rectTransform.localPosition = target + new Vector2(28f, 12f);
        label.rectTransform.sizeDelta = new Vector2(84f, 42f);
        if (Enemy.m_txtHealth != null) label.font = Enemy.m_txtHealth.font;
        label.text = "-" + amount;
        label.fontSize = 30f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color32(255, 210, 100, 255);
        label.raycastTarget = false;
        DOTween.Sequence().SetLink(label.gameObject)
            .Append(label.rectTransform.DOLocalMoveY(label.rectTransform.localPosition.y + 24f, 0.4f))
            .Insert(0.15f, label.DOFade(0f, 0.25f))
            .OnComplete(() => Destroy(label.gameObject));
    }

    IEnumerator coPopupReward()
    {
        yield return new WaitForSeconds(.25f);        
        SS.UI.Core.Load<PopupReward>("PopupReward", LoadSceneMode.Additive, popup => popup.Setup(Data.itemReward.ItemReward));
    }

    #endregion

}


public enum typeShape
{
    HongCon, TuCuu, GiayCo, LongDau, QuatGiay
}
