using DG.Tweening;
using NTGH.Teammates;
using NTGH.UI;
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

    Vector2 m_avatarBaseAnchoredPos;
    Color m_avatarBaseColor = Color.white;
    bool m_avatarDefaultsCached;
    Sequence m_hitFeedbackSeq;

    public void ApplyLoadout(BattleLoadout loadout)
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
                slotView.Bind(card);
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
        CacheAvatarDefaults();
        UpdateHealthText(character.health, character.health);
    }

    private void CacheAvatarDefaults()
    {
        if (m_avatarDefaultsCached || m_imgAvatar == null) return;
        m_avatarBaseAnchoredPos = m_imgAvatar.rectTransform.anchoredPosition;
        m_avatarBaseColor = m_imgAvatar.color;
        m_avatarDefaultsCached = true;
    }

    public void PlayHitFeedback(bool shielded = false)
    {
        if (m_imgAvatar == null) return;
        CacheAvatarDefaults();

        m_hitFeedbackSeq?.Kill();
        m_imgAvatar.DOKill();
        m_imgAvatar.rectTransform.DOKill();

        m_imgAvatar.rectTransform.anchoredPosition = m_avatarBaseAnchoredPos;
        Color flashColor = shielded
            ? Color.Lerp(m_avatarBaseColor, new Color(0.75f, 0.90f, 1f, m_avatarBaseColor.a), 0.35f)
            : new Color(1f, 0.28f, 0.28f, m_avatarBaseColor.a);
        float duration = shielded ? 0.22f : 0.28f;
        float shakeStrength = shielded ? 6f : 14f;
        int shakeVibrato = shielded ? 16 : 24;

        m_imgAvatar.color = flashColor;

        m_hitFeedbackSeq = DOTween.Sequence().SetLink(m_imgAvatar.gameObject)
            .Join(m_imgAvatar.DOColor(m_avatarBaseColor, duration).SetEase(Ease.OutQuad))
            .Join(m_imgAvatar.rectTransform.DOShakeAnchorPos(duration, shakeStrength, shakeVibrato, 90f, false, true))
            .OnComplete(() =>
            {
                if (m_imgAvatar != null)
                {
                    m_imgAvatar.color = m_avatarBaseColor;
                    m_imgAvatar.rectTransform.anchoredPosition = m_avatarBaseAnchoredPos;
                }
            });
    }

    public bool HasEquippedCardType(TeammateCardType cardType)
    {
        if (m_teammateSlots == null) return false;
        for (int i = 0; i < m_teammateSlots.Length; i++)
        {
            var slot = m_teammateSlots[i];
            if (slot != null && slot.CurrentCard != null && slot.CurrentCard.CardType == cardType)
            {
                return true;
            }
        }
        return false;
    }

    public void PlaySupportCardsGlow()
    {
        if (m_teammateSlots == null) return;
        for (int i = 0; i < m_teammateSlots.Length; i++)
        {
            var slot = m_teammateSlots[i];
            if (slot != null && slot.CurrentCard != null && slot.CurrentCard.CardType == TeammateCardType.Support)
            {
                slot.PlaySupportGlow();
            }
        }
    }

    public void KillTweens()
    {
        m_Health?.DOKill();
        m_hitFeedbackSeq?.Kill();
        if (m_imgAvatar != null)
        {
            m_imgAvatar.DOKill();
            m_imgAvatar.rectTransform.DOKill();
        }
        if (m_teammateSlots != null)
        {
            for (int i = 0; i < m_teammateSlots.Length; i++)
            {
                m_teammateSlots[i]?.KillHighlightTween(restoreVisuals: false);
            }
        }
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
        if (m_EnemyPanel != null)
        {
            m_EnemyPanelParent = (RectTransform)m_EnemyPanel.parent;
            m_EnemyTopMargin = -m_EnemyPanel.anchoredPosition.y;
        }
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


    [SerializeField] RectTransform m_EnemyPanel;
    RectTransform m_EnemyPanelParent;
    float m_EnemyTopMargin;

    [SerializeField] TextMeshProUGUI m_StageTitle;

    //Effect
    [Header("Effect")]
    [SerializeField] ParticleSystem m_effAttack;
    [SerializeField] ParticleSystem m_effShield;
    [SerializeField] float m_shieldEffectScale = 0.35f;

    [Header("Weapon")]
    [SerializeField] Image m_WeaponImage;
    CreateItemReward weapon;

    [Header("Battle Result Popups")]
    [SerializeField] int m_winRewardGold = 350;
    [SerializeField] int m_loseGoldPenalty = 200;
    [SerializeField] int m_reviveGoldCost = 200;
    [SerializeField, Range(0.1f, 1f)] float m_reviveHealthPercent = 1f;
    
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
    float m_LastPlayerShieldTime = -10f;
    float m_LastEnemyShieldTime = -10f;

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
        if (m_effAttack != null)
        {
            ObjectPoolManager.Instance.Prewarm(m_effAttack, 8);
        }
        if (m_effShield != null)
        {
            ObjectPoolManager.Instance.Prewarm(m_effShield, 4);
        }
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

    void FitEnemyPanelToSafeArea()
    {
        if (m_EnemyPanelParent == null || Screen.height <= 0) return;

        float topInset = (Screen.height - Screen.safeArea.yMax) / Screen.height * m_EnemyPanelParent.rect.height;
        float topMargin = Mathf.Max(m_EnemyTopMargin, topInset + 16f);
        Vector2 position = m_EnemyPanel.anchoredPosition;
        if (Mathf.Approximately(position.y, -topMargin)) return;

        position.y = -topMargin;
        m_EnemyPanel.anchoredPosition = position;
    }

    void LateUpdate()
    {
        FitEnemyPanelToSafeArea();
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
            ShowBattleResultPopup();
        }
    }

    Canvas GetBattleCanvas()
    {
        return GetComponentInChildren<Canvas>();
    }

    void ShowBattleResultPopup()
    {
        Canvas battleCanvas = GetBattleCanvas();

        if (m_Result == resultType.win)
        {
            GameWinPopupController.Open(
                m_winRewardGold,
                onNext: () => SOManager.instance?.PlayCurrentLevel(),
                callerCanvas: battleCanvas);

            if (Data != null && Data.itemReward != null &&
                AdsController.instance != null && AdsController.instance.checkReward())
            {
                StartCoroutine(coPopupReward());
            }
            return;
        }

        User.EnsureLoaded();
        int actualPenalty = User.data != null
            ? Mathf.Min(User.data.gold, Mathf.Max(0, m_loseGoldPenalty))
            : 0;

        LoseWarningPopupController.Open(
            actualPenalty,
            m_reviveGoldCost,
            onRevive: HandleReviveFromWarning,
            onCloseToLose: () => HandleConfirmDefeat(actualPenalty),
            callerCanvas: battleCanvas);
    }

    void HandleReviveFromWarning(bool viaAd)
    {
        m_PlayerHp = Mathf.Clamp(m_PlayerMaxHp * m_reviveHealthPercent, 1f, m_PlayerMaxHp);
        if (Player != null)
        {
            if (Player.m_Health != null)
            {
                Player.m_Health.DOKill();
                Player.m_Health.fillAmount = m_PlayerHp / m_PlayerMaxHp;
            }
            Player.UpdateHealthText(m_PlayerHp, m_PlayerMaxHp);
        }

        m_EnemyAttackProgress = 1f;
        Enemy?.UpdateAttackTimer(m_EnemyAttackProgress);

        endGame = false;
        m_ResultShown = false;
        canClick = true;
        moving = false;
    }

    void HandleConfirmDefeat(int goldPenalty)
    {
        int deducted = 0;
        if (goldPenalty > 0)
        {
            User.EnsureLoaded();
            if (User.data != null)
            {
                deducted = Mathf.Min(User.data.gold, goldPenalty);
                if (deducted > 0 && User.data.TrySpendGold(deducted))
                {
                    User.Save();
                }
            }
        }

        GameLosePopupController.Open(
            deducted,
            onRetry: () => SOManager.instance?.PlayCurrentLevel(),
            onHome: SOManager.LoadHome,
            callerCanvas: GetBattleCanvas());
    }

    void OnDestroy()
    {
        Player?.KillTweens();
        Enemy?.KillTweens();
        if (ObjectPoolManager.HasInstance)
        {
            if (m_effAttack != null)
            {
                ObjectPoolManager.Instance.ClearPool(m_effAttack.gameObject);
            }
            if (m_effShield != null)
            {
                ObjectPoolManager.Instance.ClearPool(m_effShield.gameObject);
            }
        }
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

    void EnemyAttack()
    {
        if (m_EnemyAttackProgress > 0f)
        {
            m_EnemyAttackProgress = Mathf.Max(0f, m_EnemyAttackProgress - Time.deltaTime * 0.06f);
        }
        else
        {
            int baseDamage = Data.enemy.Character.attack + (m_EnemyLoadout != null ? Mathf.Max(0, m_EnemyLoadout.WeaponBonusAttack) : 0);
            int remainingReduction = m_PlayerLoadout != null ? m_PlayerLoadout.GetTotalDamageReduction() : 0;
            int mainDamage = ConsumeDamageReduction(baseDamage, ref remainingReduction);
            m_EnemyAttackProgress = 1f;

            float effectZ = m_effAttack != null ? m_effAttack.transform.position.z : 0f;
            Vector3 enemyStartWorld = GetAvatarWorldPosition(Enemy, effectZ);
            LaunchAttackEffect(enemyStartWorld, Player, mainDamage, () =>
            {
                Attack(Player, ref m_PlayerHp, m_PlayerMaxHp,
                    mainDamage, ConversationTrigger.PlayerHealth);
            });

            LaunchAttackCardSwords(Enemy, Player, ref remainingReduction, targetIsPlayer: true);
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

        Player.ApplyLoadout(m_PlayerLoadout);
        Enemy.ApplyLoadout(m_EnemyLoadout);

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
        bool hasSupport = HasSupportCard(target);
        target?.PlayHitFeedback(shielded: hasSupport);
        if (hasSupport)
        {
            target?.PlaySupportCardsGlow();
            PlayShieldEffect(target);
        }
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

        int remainingReduction = m_EnemyLoadout != null ? m_EnemyLoadout.GetTotalDamageReduction() : 0;
        int mainDamage = ConsumeDamageReduction(clearedItems, ref remainingReduction);

        LaunchAttackEffect(boardPosition, Enemy, mainDamage, () =>
        {
            Attack(Enemy, ref m_EnemyHp, m_EnemyMaxHp, mainDamage, ConversationTrigger.EnemyHealth);
        });

        LaunchAttackCardSwords(Player, Enemy, ref remainingReduction, targetIsPlayer: false);
    }

    private static int ConsumeDamageReduction(int rawDamage, ref int remainingReduction)
    {
        int clampedRaw = Mathf.Max(1, rawDamage);
        if (remainingReduction <= 0) return clampedRaw;

        int maxAbsorbable = Mathf.Max(0, clampedRaw - 1);
        int absorbed = Mathf.Min(maxAbsorbable, remainingReduction);
        remainingReduction -= absorbed;
        return clampedRaw - absorbed;
    }

    private bool HasSupportCard(CharacterOnGame target)
    {
        if (target == null) return false;
        if (target.HasEquippedCardType(TeammateCardType.Support)) return true;

        BattleLoadout loadout = target == Player ? m_PlayerLoadout : m_EnemyLoadout;
        return loadout != null && loadout.GetTotalDamageReduction() > 0;
    }

    private void LaunchAttackCardSwords(CharacterOnGame attacker, CharacterOnGame target, ref int remainingReduction, bool targetIsPlayer)
    {
        if (attacker == null || attacker.m_teammateSlots == null || target == null) return;

        float effectZ = m_effAttack != null ? m_effAttack.transform.position.z : 0f;
        int launchedCount = 0;

        for (int i = 0; i < attacker.m_teammateSlots.Length; i++)
        {
            var slot = attacker.m_teammateSlots[i];
            if (slot == null || slot.CurrentCard == null || slot.CurrentCard.CardType != TeammateCardType.Attack)
            {
                continue;
            }

            int cardDamage = ConsumeDamageReduction(Mathf.Max(1, slot.CurrentCard.EffectValue), ref remainingReduction);
            RectTransform cardRect = slot.transform as RectTransform;
            Vector3 cardWorld = cardRect != null
                ? GetRectWorldPosition(cardRect, effectZ)
                : GetAvatarWorldPosition(attacker, effectZ);

            float delay = 0.08f * (launchedCount + 1);
            launchedCount++;

            slot.PlayAttackPulse();
            LaunchAttackEffect(cardWorld, target, cardDamage, () =>
            {
                if (targetIsPlayer)
                {
                    Attack(Player, ref m_PlayerHp, m_PlayerMaxHp, cardDamage, ConversationTrigger.PlayerHealth);
                }
                else
                {
                    Attack(Enemy, ref m_EnemyHp, m_EnemyMaxHp, cardDamage, ConversationTrigger.EnemyHealth);
                }
            }, delay);
        }
    }

    private void PlayShieldEffect(CharacterOnGame target)
    {
        if (m_effShield == null || target == null || target.m_imgAvatar == null) return;

        float now = Time.time;
        if (target == Player)
        {
            if (now - m_LastPlayerShieldTime < 0.12f) return;
            m_LastPlayerShieldTime = now;
        }
        else
        {
            if (now - m_LastEnemyShieldTime < 0.12f) return;
            m_LastEnemyShieldTime = now;
        }

        float shieldZ = m_effAttack != null ? m_effAttack.transform.position.z : m_effShield.transform.position.z;
        Vector3 avatarWorld = GetAvatarWorldPosition(target, shieldZ);
        ParticleSystem effect = ObjectPoolManager.Instance.Rent(m_effShield, avatarWorld, m_effShield.transform.rotation);
        if (effect == null) return;

        effect.name = "SupportShieldEffect";
        effect.transform.DOKill();
        effect.transform.localScale = Vector3.one * Mathf.Max(0.05f, m_shieldEffectScale);
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        int sortingLayerID = 0;
        int sortingOrder = 5;
        if (m_effAttack != null && m_effAttack.TryGetComponent<ParticleSystemRenderer>(out var attackRenderer))
        {
            sortingLayerID = attackRenderer.sortingLayerID;
            sortingOrder = Mathf.Max(5, attackRenderer.sortingOrder + 1);
        }

        foreach (ParticleSystemRenderer psRenderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            psRenderer.sortingLayerID = sortingLayerID;
            psRenderer.sortingOrder = sortingOrder;
        }

        if (!effect.TryGetComponent<PooledParticleAutoReturn>(out _))
        {
            effect.gameObject.AddComponent<PooledParticleAutoReturn>();
        }
        var rootMain = effect.main;
        rootMain.stopAction = ParticleSystemStopAction.Callback;
        effect.Play(true);
    }

    private Vector3 GetAvatarWorldPosition(CharacterOnGame character, float effectZ)
    {
        Image avatar = character != null ? character.m_imgAvatar : null;
        return avatar != null ? GetRectWorldPosition(avatar.rectTransform, effectZ) : new Vector3(0f, 0f, effectZ);
    }

    private Vector3 GetRectWorldPosition(RectTransform rect, float effectZ)
    {
        if (rect == null) return new Vector3(0f, 0f, effectZ);

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;
        Vector3 centerWorld = rect.TransformPoint(rect.rect.center);
        Camera mainCamera = Camera.main;
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, centerWorld);
        Vector3 worldPos = mainCamera != null
            ? mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(effectZ - mainCamera.transform.position.z)))
            : centerWorld;
        worldPos.z = effectZ;
        return worldPos;
    }

    private void LaunchAttackEffect(Vector3 startPosition, CharacterOnGame target, int damageAmount, Action onHit, float startDelay = 0f)
    {
        if (startDelay > 0f)
        {
            DOVirtual.DelayedCall(startDelay, () =>
            {
                if (endGame) return;
                LaunchAttackEffect(startPosition, target, damageAmount, onHit, 0f);
            }).SetLink(gameObject);
            return;
        }
        Image avatar = target != null ? target.m_imgAvatar : null;
        Canvas canvas = avatar != null && avatar.canvas != null ? avatar.canvas.rootCanvas : null;
        RectTransform layer = canvas != null ? (RectTransform)canvas.transform : null;
        Vector3 avatarCenterWorld = avatar != null
            ? avatar.rectTransform.TransformPoint(avatar.rectTransform.rect.center)
            : Vector3.zero;
        Vector2 targetLocal = layer != null ? (Vector2)layer.InverseTransformPoint(avatarCenterWorld) : Vector2.zero;

        if (m_effAttack == null || avatar == null)
        {
            onHit?.Invoke();
            if (layer != null) ShowMatchDamage(layer, targetLocal, damageAmount);
            return;
        }

        float effectZ = m_effAttack.transform.position.z;
        Vector3 startWorld = new Vector3(startPosition.x, startPosition.y, effectZ);
        Vector3 targetWorld = GetAvatarWorldPosition(target, effectZ);

        Vector2 dir = new Vector2(targetWorld.x - startWorld.x, targetWorld.y - startWorld.y);
        // BuffAttack_01 sword texture points straight up (+Y); Billboard startRotation is clockwise in radians.
        float swordRotationRad = dir.sqrMagnitude > 0.0001f ? Mathf.Atan2(dir.x, dir.y) : 0f;

        ParticleSystem effect = ObjectPoolManager.Instance.Rent(m_effAttack, startWorld, m_effAttack.transform.rotation);
        if (effect == null)
        {
            onHit?.Invoke();
            if (layer != null) ShowMatchDamage(layer, targetLocal, damageAmount);
            return;
        }

        effect.name = "MatchAttackEffect";
        effect.transform.DOKill();
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (ParticleSystem ps in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.simulationSpace = ps.name == "Star"
                ? ParticleSystemSimulationSpace.World
                : ParticleSystemSimulationSpace.Local;
            if (ps == effect || ps.name == "Sword")
            {
                main.startRotation = swordRotationRad;
            }
        }
        if (!effect.TryGetComponent<PooledParticleAutoReturn>(out _))
        {
            effect.gameObject.AddComponent<PooledParticleAutoReturn>();
        }
        var rootMain = effect.main;
        rootMain.stopAction = ParticleSystemStopAction.Callback;
        effect.Play(true);

        Vector3 approachDir = dir.sqrMagnitude > 0.0001f ? (Vector3)dir.normalized : Vector3.up;
        Vector3 windUpWorld = startWorld - approachDir * 0.9f;
        Vector3 recoilWorld = targetWorld - approachDir * 0.35f;

        DOTween.Sequence().SetLink(effect.gameObject, LinkBehaviour.KillOnDisable)
            .AppendInterval(0.25f)
            .Append(effect.transform.DOMove(windUpWorld, 0.18f).SetEase(Ease.OutCubic))
            .Append(effect.transform.DOMove(targetWorld, 0.28f).SetEase(Ease.InCubic))
            .AppendCallback(() =>
            {
                if (endGame) return;
                onHit?.Invoke();
                if (layer != null) ShowMatchDamage(layer, targetLocal, damageAmount);
            })
            .Append(effect.transform.DOMove(recoilWorld, 0.12f).SetEase(Ease.OutBack));
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
