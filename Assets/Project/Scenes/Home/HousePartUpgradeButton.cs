using System;
using DG.Tweening;
using NTGH.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compact icon-only Neon 90s upgrade arrow button anchored over a house part (Awning, Door, Wall)
/// during Home House Upgrade Mode.
/// - Displays an upward upgrade arrow with a gentle bob animation when upgradeable.
/// - Remains clickable at max level so clicking opens the "Coming Soon" notice popup.
/// </summary>
[DisallowMultipleComponent]
public class HousePartUpgradeButton : MonoBehaviour
{
    [Header("Target House Part")]
    [SerializeField] private HomeHouseTheme.Part m_part = HomeHouseTheme.Part.Awning;

    [Header("UI References")]
    [SerializeField] private Button m_button;
    [SerializeField] private NeonButtonStyle m_neonStyle;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private Image m_arrowIcon;

    [Header("Idle Bob Animation")]
    [SerializeField] private float m_bobDistance = 5f;
    [SerializeField] private float m_bobDuration = 0.65f;

    private Action<HomeHouseTheme.Part> m_onClicked;
    private Sequence m_showHideSeq;
    private Tween m_feedbackTween;
    private Tween m_bobTween;
    private RectTransform m_rectTransform;
    private Vector2 m_baseAnchoredPos;
    private Vector2 m_arrowBasePos;
    private bool m_hasCapturedPos;
    private bool m_hasCapturedArrowPos;

    // Neon 90s palettes for upgrade arrow button states
    private static readonly Color ColAffordFill = new Color(0.05f, 0.16f, 0.22f, 0.92f);
    private static readonly Color ColAffordOuter = new Color(0.16f, 0.92f, 0.88f, 1f);
    private static readonly Color ColAffordInner = new Color(1.00f, 0.80f, 0.30f, 1f);
    private static readonly Color ColAffordArrow = new Color(1.00f, 0.92f, 0.45f, 1f);

    private static readonly Color ColLockedFill = new Color(0.20f, 0.06f, 0.10f, 0.92f);
    private static readonly Color ColLockedOuter = new Color(1.00f, 0.32f, 0.44f, 1f);
    private static readonly Color ColLockedInner = new Color(1.00f, 0.68f, 0.30f, 0.90f);
    private static readonly Color ColLockedArrow = new Color(1.00f, 0.60f, 0.64f, 1f);

    private static readonly Color ColMaxFill = new Color(0.06f, 0.16f, 0.13f, 0.88f);
    private static readonly Color ColMaxOuter = new Color(0.24f, 0.90f, 0.56f, 0.92f);
    private static readonly Color ColMaxInner = new Color(1.00f, 0.82f, 0.35f, 0.85f);
    private static readonly Color ColMaxArrow = new Color(0.52f, 1.00f, 0.76f, 0.95f);

    public HomeHouseTheme.Part Part => m_part;

    private void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();
        if (m_button == null)
            m_button = GetComponent<Button>();
        if (m_neonStyle == null)
            m_neonStyle = GetComponent<NeonButtonStyle>();
        if (m_canvasGroup == null)
            m_canvasGroup = GetComponent<CanvasGroup>();

        CaptureBasePosition();
        CaptureArrowBasePosition();

        if (m_button != null)
            m_button.onClick.AddListener(HandleClicked);
    }

    private void OnDisable()
    {
        StopArrowBob();
    }

    private void OnDestroy()
    {
        KillTweens();
        if (m_button != null)
            m_button.onClick.RemoveListener(HandleClicked);
    }

    public void Bind(Action<HomeHouseTheme.Part> onClicked)
    {
        m_onClicked = onClicked;
    }

    public void Refresh(HomeHouseTheme houseTheme, int playerGold)
    {
        if (houseTheme == null)
            return;

        HomeHouseTheme.Level currentLevel = houseTheme.GetPartLevel(m_part);
        bool isMax = currentLevel >= HomeHouseTheme.Level.Renovated;

        // Keep button interactable even at max level so clicking shows the "Coming Soon" dialog.
        if (m_button != null)
            m_button.interactable = true;

        if (isMax)
        {
            if (m_neonStyle != null)
            {
                m_neonStyle.SetPalette(ColMaxFill, ColMaxOuter, ColMaxInner, ColMaxArrow, Color.white);
            }
            else if (m_arrowIcon != null)
            {
                m_arrowIcon.color = ColMaxArrow;
            }

            StopArrowBob();
        }
        else
        {
            HomeHouseTheme.Level nextLevel = (HomeHouseTheme.Level)((int)currentLevel + 1);
            HomeHouseTheme.HouseItemSpec spec = HomeHouseTheme.GetItemSpec(m_part, nextLevel);
            bool alreadyUnlocked = houseTheme.IsPartLevelUnlocked(m_part, nextLevel);
            bool canAfford = alreadyUnlocked || playerGold >= spec.PriceGold;

            if (m_neonStyle != null)
            {
                if (canAfford)
                    m_neonStyle.SetPalette(ColAffordFill, ColAffordOuter, ColAffordInner, ColAffordArrow, Color.white);
                else
                    m_neonStyle.SetPalette(ColLockedFill, ColLockedOuter, ColLockedInner, ColLockedArrow, Color.white);
            }
            else if (m_arrowIcon != null)
            {
                m_arrowIcon.color = canAfford ? ColAffordArrow : ColLockedArrow;
            }

            if (gameObject.activeInHierarchy)
                StartArrowBob();
        }
    }

    public void ShowAnimated(float delay = 0f)
    {
        CaptureBasePosition();
        CaptureArrowBasePosition();
        KillTweens();
        gameObject.SetActive(true);

        if (m_rectTransform != null)
            m_rectTransform.anchoredPosition = m_baseAnchoredPos;

        transform.localScale = Vector3.one * 0.55f;
        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = 0f;
            m_canvasGroup.blocksRaycasts = true;
        }

        m_showHideSeq = DOTween.Sequence().SetUpdate(true);
        if (delay > 0f)
            m_showHideSeq.AppendInterval(delay);

        m_showHideSeq.Append(transform.DOScale(Vector3.one, 0.28f).SetEase(Ease.OutBack));
        if (m_canvasGroup != null)
            m_showHideSeq.Join(m_canvasGroup.DOFade(1f, 0.20f).SetEase(Ease.OutQuad));

        m_showHideSeq.OnComplete(StartArrowBob);
    }

    public void HideAnimated()
    {
        if (!gameObject.activeSelf)
            return;

        KillTweens();
        if (m_canvasGroup != null)
            m_canvasGroup.blocksRaycasts = false;

        m_showHideSeq = DOTween.Sequence().SetUpdate(true);
        m_showHideSeq.Append(transform.DOScale(Vector3.one * 0.55f, 0.20f).SetEase(Ease.InBack));
        if (m_canvasGroup != null)
            m_showHideSeq.Join(m_canvasGroup.DOFade(0f, 0.16f).SetEase(Ease.InQuad));

        m_showHideSeq.OnComplete(() =>
        {
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        });
    }

    public void HideImmediate()
    {
        KillTweens();
        transform.localScale = Vector3.one;
        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = 0f;
            m_canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    public void PlayUpgradePunch()
    {
        m_feedbackTween?.Kill();
        transform.localScale = Vector3.one;
        m_feedbackTween = transform.DOPunchScale(Vector3.one * 0.18f, 0.28f, 6, 0.6f).SetUpdate(true);
    }

    public void PlayErrorShake()
    {
        CaptureBasePosition();
        m_feedbackTween?.Kill();
        if (m_rectTransform != null)
        {
            m_rectTransform.anchoredPosition = m_baseAnchoredPos;
            m_feedbackTween = m_rectTransform
                .DOShakeAnchorPos(0.30f, new Vector2(12f, 0f), 18, 0f, false, true)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (m_rectTransform != null)
                        m_rectTransform.anchoredPosition = m_baseAnchoredPos;
                });
        }
    }

    private void StartArrowBob()
    {
        if (m_arrowIcon == null || !Application.isPlaying || !gameObject.activeInHierarchy)
            return;

        CaptureArrowBasePosition();
        m_bobTween?.Kill();
        RectTransform arrowRt = m_arrowIcon.rectTransform;
        arrowRt.anchoredPosition = m_arrowBasePos - new Vector2(0f, m_bobDistance * 0.5f);
        m_bobTween = arrowRt
            .DOAnchorPosY(m_arrowBasePos.y + m_bobDistance * 0.5f, m_bobDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void StopArrowBob()
    {
        m_bobTween?.Kill();
        m_bobTween = null;

        if (m_arrowIcon != null && m_hasCapturedArrowPos)
            m_arrowIcon.rectTransform.anchoredPosition = m_arrowBasePos;
    }

    private void HandleClicked()
    {
        m_onClicked?.Invoke(m_part);
    }

    private void CaptureBasePosition()
    {
        if (m_hasCapturedPos || m_rectTransform == null)
            return;

        m_baseAnchoredPos = m_rectTransform.anchoredPosition;
        m_hasCapturedPos = true;
    }

    private void CaptureArrowBasePosition()
    {
        if (m_hasCapturedArrowPos || m_arrowIcon == null)
            return;

        m_arrowBasePos = m_arrowIcon.rectTransform.anchoredPosition;
        m_hasCapturedArrowPos = true;
    }

    private void KillTweens()
    {
        m_showHideSeq?.Kill();
        m_showHideSeq = null;

        m_feedbackTween?.Kill();
        m_feedbackTween = null;

        StopArrowBob();
    }

    public void ConfigureReferences(
        HomeHouseTheme.Part part,
        Button button,
        NeonButtonStyle neonStyle,
        CanvasGroup canvasGroup,
        Image arrowIcon)
    {
        m_part = part;
        m_button = button;
        m_neonStyle = neonStyle;
        m_canvasGroup = canvasGroup;
        m_arrowIcon = arrowIcon;
    }
}
