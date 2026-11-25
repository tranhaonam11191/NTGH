using DG.Tweening;
using NTGH.UI;
using SS.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the interactive House Upgrade Mode on the Home screen:
/// - Slides Home HUD elements off-screen (top -> up, left -> left, bottom -> down)
///   leaving only the house backdrop and Dau Bu character visible.
/// - Slides in a Back button at the top-right corner and pops in direct upgrade buttons
///   over Awning, Door, and Wall.
/// - Upgrades the clicked part if the player has enough gold, or opens NotEnoughGoldPopup
///   with a Rewarded Ad button to earn gold.
/// - Reverses all slide animations when returning to normal Home view.
/// </summary>
[DisallowMultipleComponent]
public class HomeHouseUpgradeMode : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private HomeController m_homeController;
    [SerializeField] private HomeHouseTheme m_houseTheme;

    [Header("Home HUD Elements to Slide Out")]
    [SerializeField] private RectTransform m_topLeftHud;      // BtnSettings -> slides UP
    [SerializeField] private RectTransform m_topCenterHud;    // HeartDisplay -> slides UP
    [SerializeField] private RectTransform m_topRightHud;     // CurrencyDisplay -> slides UP
    [SerializeField] private RectTransform m_leftMenuGroup;   // LeftMenuGroup -> slides LEFT
    [SerializeField] private RectTransform m_bottomLeftBtn;   // BtnPlay -> slides DOWN
    [SerializeField] private RectTransform m_bottomRightBtn;  // BtnHouse -> slides DOWN

    [Header("House Mode Controls")]
    [SerializeField] private RectTransform m_backButtonRect;
    [SerializeField] private Button m_backButton;
    [SerializeField] private HousePartUpgradeButton m_awningUpgradeBtn;
    [SerializeField] private HousePartUpgradeButton m_doorUpgradeBtn;
    [SerializeField] private HousePartUpgradeButton m_wallUpgradeBtn;

    [Header("Animation Settings")]
    [SerializeField] private float m_slideDuration = 0.34f;
    [SerializeField] private float m_topSlideOffset = 240f;
    [SerializeField] private float m_leftSlideOffset = 260f;
    [SerializeField] private float m_bottomSlideOffset = 380f;
    [SerializeField] private float m_backButtonSlideOffset = 240f;

    [Header("Ad Reward Config")]
    [SerializeField] private int m_adRewardGold = 500;

    private bool m_isHouseMode;
    private bool m_hasCapturedPositions;
    private Vector2 m_topLeftOrigin;
    private Vector2 m_topCenterOrigin;
    private Vector2 m_topRightOrigin;
    private Vector2 m_leftMenuOrigin;
    private Vector2 m_bottomLeftOrigin;
    private Vector2 m_bottomRightOrigin;
    private Vector2 m_backButtonOrigin;

    private Sequence m_transitionSeq;
    private Canvas m_ownerCanvas;
    private ScreenManager m_screenManager;

    public bool IsHouseMode => m_isHouseMode;

    private void Awake()
    {
        if (m_homeController == null)
            m_homeController = GetComponent<HomeController>();
        if (m_houseTheme == null && m_homeController != null)
            m_houseTheme = m_homeController.HouseTheme;

        m_ownerCanvas = GetComponentInChildren<Canvas>();

        CaptureOriginalPositions();

        if (m_backButton != null)
            m_backButton.onClick.AddListener(ExitHouseMode);

        BindUpgradeButton(m_awningUpgradeBtn);
        BindUpgradeButton(m_doorUpgradeBtn);
        BindUpgradeButton(m_wallUpgradeBtn);

        SetHouseModeVisualsImmediate(false);
    }

    private void OnEnable()
    {
        User.OnGoldChanged += HandleGoldChanged;
        if (m_houseTheme != null)
            m_houseTheme.PartChanged += HandlePartChanged;
    }

    private void OnDisable()
    {
        User.OnGoldChanged -= HandleGoldChanged;
        if (m_houseTheme != null)
            m_houseTheme.PartChanged -= HandlePartChanged;
    }

    private void OnDestroy()
    {
        m_transitionSeq?.Kill();
        m_transitionSeq = null;

        if (m_backButton != null)
            m_backButton.onClick.RemoveListener(ExitHouseMode);
    }

    private void Update()
    {
        if (!m_isHouseMode)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_screenManager == null)
                m_screenManager = FindAnyObjectByType<ScreenManager>();

            // Only handle Escape when no SS.UI popup is currently open on top.
            if (m_screenManager == null || m_screenManager.IsNoMoreScreen)
            {
                ExitHouseMode();
            }
        }
    }

    public void EnterHouseMode()
    {
        if (m_isHouseMode)
            return;

        CaptureOriginalPositions();
        m_isHouseMode = true;

        m_transitionSeq?.Kill();
        m_transitionSeq = DOTween.Sequence().SetUpdate(true);

        // Slide Home HUD elements out toward their nearest screen edges
        if (ShouldTweenTopLeftHud())
            AddSlideTween(m_transitionSeq, m_topLeftHud, m_topLeftOrigin + new Vector2(0f, m_topSlideOffset), m_slideDuration, Ease.InBack);
        AddSlideTween(m_transitionSeq, m_topCenterHud, m_topCenterOrigin + new Vector2(0f, m_topSlideOffset), m_slideDuration, Ease.InBack);
        AddSlideTween(m_transitionSeq, m_topRightHud, m_topRightOrigin + new Vector2(0f, m_topSlideOffset), m_slideDuration, Ease.InBack);
        AddSlideTween(m_transitionSeq, m_leftMenuGroup, m_leftMenuOrigin + new Vector2(-m_leftSlideOffset, 0f), m_slideDuration, Ease.InBack);
        AddSlideTween(m_transitionSeq, m_bottomLeftBtn, m_bottomLeftOrigin + new Vector2(0f, -m_bottomSlideOffset), m_slideDuration, Ease.InBack);
        AddSlideTween(m_transitionSeq, m_bottomRightBtn, m_bottomRightOrigin + new Vector2(0f, -m_bottomSlideOffset), m_slideDuration, Ease.InBack);

        // Slide Back button IN from top-right
        if (m_backButtonRect != null)
        {
            m_backButtonRect.gameObject.SetActive(true);
            m_backButtonRect.anchoredPosition = m_backButtonOrigin + new Vector2(m_backButtonSlideOffset, 0f);
            m_transitionSeq.Insert(
                m_slideDuration * 0.35f,
                m_backButtonRect.DOAnchorPos(m_backButtonOrigin, m_slideDuration).SetEase(Ease.OutBack));
        }

        RefreshUpgradeButtons();

        // Pop in upgrade buttons at their respective house part positions
        float baseDelay = m_slideDuration * 0.45f;
        if (m_awningUpgradeBtn != null) m_awningUpgradeBtn.ShowAnimated(baseDelay);
        if (m_doorUpgradeBtn != null) m_doorUpgradeBtn.ShowAnimated(baseDelay + 0.06f);
        if (m_wallUpgradeBtn != null) m_wallUpgradeBtn.ShowAnimated(baseDelay + 0.12f);
    }

    public void ExitHouseMode()
    {
        if (!m_isHouseMode)
            return;

        CaptureOriginalPositions();
        m_isHouseMode = false;

        m_transitionSeq?.Kill();
        m_transitionSeq = DOTween.Sequence().SetUpdate(true);

        // Hide upgrade buttons
        if (m_awningUpgradeBtn != null) m_awningUpgradeBtn.HideAnimated();
        if (m_doorUpgradeBtn != null) m_doorUpgradeBtn.HideAnimated();
        if (m_wallUpgradeBtn != null) m_wallUpgradeBtn.HideAnimated();

        // Slide Back button OUT to top-right
        if (m_backButtonRect != null)
        {
            m_transitionSeq.Join(
                m_backButtonRect
                    .DOAnchorPos(m_backButtonOrigin + new Vector2(m_backButtonSlideOffset, 0f), m_slideDuration * 0.8f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        if (!m_isHouseMode && m_backButtonRect != null)
                            m_backButtonRect.gameObject.SetActive(false);
                    }));
        }

        // Slide Home HUD elements back IN from their respective edges
        float inStart = 0.10f;
        if (ShouldTweenTopLeftHud())
            InsertSlideTween(m_transitionSeq, inStart, m_topLeftHud, m_topLeftOrigin, m_slideDuration, Ease.OutBack);
        InsertSlideTween(m_transitionSeq, inStart, m_topCenterHud, m_topCenterOrigin, m_slideDuration, Ease.OutBack);
        InsertSlideTween(m_transitionSeq, inStart, m_topRightHud, m_topRightOrigin, m_slideDuration, Ease.OutBack);
        InsertSlideTween(m_transitionSeq, inStart, m_leftMenuGroup, m_leftMenuOrigin, m_slideDuration, Ease.OutBack);
        InsertSlideTween(m_transitionSeq, inStart, m_bottomLeftBtn, m_bottomLeftOrigin, m_slideDuration, Ease.OutBack);
        InsertSlideTween(m_transitionSeq, inStart, m_bottomRightBtn, m_bottomRightOrigin, m_slideDuration, Ease.OutBack);

        if (m_homeController != null)
        {
            m_homeController.SetData();
        }
    }

    public void RefreshUpgradeButtons()
    {
        if (m_houseTheme == null)
            return;

        User.EnsureLoaded();
        int playerGold = User.data.gold;

        if (m_awningUpgradeBtn != null) m_awningUpgradeBtn.Refresh(m_houseTheme, playerGold);
        if (m_doorUpgradeBtn != null) m_doorUpgradeBtn.Refresh(m_houseTheme, playerGold);
        if (m_wallUpgradeBtn != null) m_wallUpgradeBtn.Refresh(m_houseTheme, playerGold);
    }

    private void HandleUpgradePartClicked(HomeHouseTheme.Part part)
    {
        if (m_houseTheme == null)
            return;

        HomeHouseTheme.Level currentLevel = m_houseTheme.GetPartLevel(part);
        if (currentLevel >= HomeHouseTheme.Level.Renovated)
        {
            NeonDialogPopupController.ShowNotice(
                title: "COMING SOON",
                message: "Cấp nâng cấp tiếp theo sẽ sớm ra mắt!",
                okLabel: "OK",
                callerCanvas: m_ownerCanvas);
            return;
        }

        HomeHouseTheme.Level nextLevel = (HomeHouseTheme.Level)((int)currentLevel + 1);
        HomeHouseTheme.HouseItemSpec spec = HomeHouseTheme.GetItemSpec(part, nextLevel);
        HousePartUpgradeButton btn = GetButtonForPart(part);

        if (m_houseTheme.IsPartLevelUnlocked(part, nextLevel))
        {
            m_houseTheme.SetPartLevel(part, nextLevel);
            if (btn != null) btn.PlayUpgradePunch();
            RefreshUpgradeButtons();
            if (m_homeController != null) m_homeController.SetData();
            return;
        }

        User.EnsureLoaded();
        if (User.data.TrySpendGold(spec.PriceGold))
        {
            User.data.UnlockHouseItem(part, nextLevel);
            m_houseTheme.SetPartLevel(part, nextLevel);
            if (btn != null) btn.PlayUpgradePunch();
            RefreshUpgradeButtons();
            if (m_homeController != null) m_homeController.SetData();
        }
        else
        {
            if (btn != null) btn.PlayErrorShake();

            NotEnoughGoldPopupController.Open(
                requiredGold: spec.PriceGold,
                adRewardGold: m_adRewardGold,
                onRewardClaimed: RefreshUpgradeButtons,
                callerCanvas: m_ownerCanvas);
        }
    }

    private void HandleGoldChanged(int _)
    {
        if (m_isHouseMode)
            RefreshUpgradeButtons();
    }

    private void HandlePartChanged(HomeHouseTheme.Part _, HomeHouseTheme.Level __)
    {
        if (m_isHouseMode)
            RefreshUpgradeButtons();
    }

    private HousePartUpgradeButton GetButtonForPart(HomeHouseTheme.Part part) => part switch
    {
        HomeHouseTheme.Part.Awning => m_awningUpgradeBtn,
        HomeHouseTheme.Part.Door => m_doorUpgradeBtn,
        HomeHouseTheme.Part.Wall => m_wallUpgradeBtn,
        _ => null
    };

    private void BindUpgradeButton(HousePartUpgradeButton btn)
    {
        if (btn != null)
            btn.Bind(HandleUpgradePartClicked);
    }

    private void SetHouseModeVisualsImmediate(bool houseMode)
    {
        if (m_backButtonRect != null)
        {
            m_backButtonRect.anchoredPosition = houseMode
                ? m_backButtonOrigin
                : m_backButtonOrigin + new Vector2(m_backButtonSlideOffset, 0f);
            m_backButtonRect.gameObject.SetActive(houseMode);
        }

        if (!houseMode)
        {
            if (m_awningUpgradeBtn != null) m_awningUpgradeBtn.HideImmediate();
            if (m_doorUpgradeBtn != null) m_doorUpgradeBtn.HideImmediate();
            if (m_wallUpgradeBtn != null) m_wallUpgradeBtn.HideImmediate();
        }
    }

    private bool ShouldTweenTopLeftHud()
    {
        if (m_topLeftHud == null)
            return false;

        // If BtnSettings lives inside LeftMenuGroup (VerticalLayoutGroup), LeftMenuGroup already
        // slides it out/in cleanly. Tweening it separately fights the LayoutGroup and corrupts its position.
        if (m_leftMenuGroup != null && m_topLeftHud.IsChildOf(m_leftMenuGroup))
            return false;

        return true;
    }

    private void CaptureOriginalPositions()
    {
        if (m_hasCapturedPositions)
            return;

        if (ShouldTweenTopLeftHud()) m_topLeftOrigin = m_topLeftHud.anchoredPosition;
        if (m_topCenterHud != null) m_topCenterOrigin = m_topCenterHud.anchoredPosition;
        if (m_topRightHud != null) m_topRightOrigin = m_topRightHud.anchoredPosition;
        if (m_leftMenuGroup != null) m_leftMenuOrigin = m_leftMenuGroup.anchoredPosition;
        if (m_bottomLeftBtn != null) m_bottomLeftOrigin = m_bottomLeftBtn.anchoredPosition;
        if (m_bottomRightBtn != null) m_bottomRightOrigin = m_bottomRightBtn.anchoredPosition;
        if (m_backButtonRect != null) m_backButtonOrigin = m_backButtonRect.anchoredPosition;

        m_hasCapturedPositions = true;
    }

    private static void AddSlideTween(Sequence seq, RectTransform target, Vector2 endPos, float duration, Ease ease)
    {
        if (target == null) return;
        seq.Join(target.DOAnchorPos(endPos, duration).SetEase(ease));
    }

    private static void InsertSlideTween(Sequence seq, float atPosition, RectTransform target, Vector2 endPos, float duration, Ease ease)
    {
        if (target == null) return;
        seq.Insert(atPosition, target.DOAnchorPos(endPos, duration).SetEase(ease));
    }

    public void SetTopCenterHud(RectTransform topCenterHud)
    {
        m_topCenterHud = topCenterHud;
    }

    public void ConfigureReferences(
        HomeController homeController,
        HomeHouseTheme houseTheme,
        RectTransform topLeftHud,
        RectTransform topRightHud,
        RectTransform leftMenuGroup,
        RectTransform bottomLeftBtn,
        RectTransform bottomRightBtn,
        RectTransform backButtonRect,
        Button backButton,
        HousePartUpgradeButton awningUpgradeBtn,
        HousePartUpgradeButton doorUpgradeBtn,
        HousePartUpgradeButton wallUpgradeBtn)
    {
        m_homeController = homeController;
        m_houseTheme = houseTheme;
        m_topLeftHud = topLeftHud;
        m_topRightHud = topRightHud;
        m_leftMenuGroup = leftMenuGroup;
        m_bottomLeftBtn = bottomLeftBtn;
        m_bottomRightBtn = bottomRightBtn;
        m_backButtonRect = backButtonRect;
        m_backButton = backButton;
        m_awningUpgradeBtn = awningUpgradeBtn;
        m_doorUpgradeBtn = doorUpgradeBtn;
        m_wallUpgradeBtn = wallUpgradeBtn;
    }
}
