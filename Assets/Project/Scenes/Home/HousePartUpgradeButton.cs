using System;
using DG.Tweening;
using NTGH.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Floating Neon 90s upgrade button anchored directly over a house part (Awning, Door, Wall)
/// during Home House Upgrade Mode.
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
    [SerializeField] private TextMeshProUGUI m_partNameText;
    [SerializeField] private TextMeshProUGUI m_levelText;
    [SerializeField] private Image m_actionIcon;
    [SerializeField] private TextMeshProUGUI m_priceText;
    [SerializeField] private RectTransform m_priceRow;

    [Header("Icons")]
    [SerializeField] private Sprite m_dollarIcon;
    [SerializeField] private Sprite m_checkIcon;

    private Action<HomeHouseTheme.Part> m_onClicked;
    private Sequence m_showHideSeq;
    private Tween m_feedbackTween;
    private RectTransform m_rectTransform;
    private Vector2 m_baseAnchoredPos;
    private bool m_hasCapturedPos;

    // Neon 90s palettes for upgrade button states
    private static readonly Color ColAffordFill = new Color(0.05f, 0.16f, 0.20f, 0.94f);
    private static readonly Color ColAffordOuter = new Color(0.16f, 0.92f, 0.88f, 1f);
    private static readonly Color ColAffordInner = new Color(1.00f, 0.80f, 0.30f, 1f);
    private static readonly Color ColAffordPrice = new Color(1.00f, 0.90f, 0.45f, 1f);

    private static readonly Color ColLockedFill = new Color(0.20f, 0.06f, 0.10f, 0.94f);
    private static readonly Color ColLockedOuter = new Color(1.00f, 0.32f, 0.42f, 1f);
    private static readonly Color ColLockedInner = new Color(1.00f, 0.68f, 0.30f, 0.90f);
    private static readonly Color ColLockedPrice = new Color(1.00f, 0.54f, 0.60f, 1f);

    private static readonly Color ColMaxFill = new Color(0.06f, 0.18f, 0.13f, 0.90f);
    private static readonly Color ColMaxOuter = new Color(0.24f, 0.92f, 0.56f, 0.95f);
    private static readonly Color ColMaxInner = new Color(1.00f, 0.82f, 0.35f, 0.90f);
    private static readonly Color ColMaxPrice = new Color(0.45f, 1.00f, 0.72f, 1f);

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

        if (m_button != null)
            m_button.onClick.AddListener(HandleClicked);
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

        if (m_partNameText != null)
            m_partNameText.text = HomeHouseTheme.GetPartDisplayName(m_part).ToUpperInvariant();

        if (isMax)
        {
            if (m_levelText != null)
                m_levelText.text = "CẤP TỐI ĐA";

            if (m_actionIcon != null)
            {
                m_actionIcon.sprite = m_checkIcon;
                m_actionIcon.gameObject.SetActive(m_checkIcon != null);
                m_actionIcon.color = ColMaxPrice;
            }

            if (m_priceText != null)
            {
                m_priceText.text = "MAX";
                m_priceText.color = ColMaxPrice;
            }

            if (m_button != null)
                m_button.interactable = false;

            if (m_neonStyle != null)
            {
                m_neonStyle.SetPalette(ColMaxFill, ColMaxOuter, ColMaxInner, ColMaxPrice, Color.white);
            }
        }
        else
        {
            HomeHouseTheme.Level nextLevel = (HomeHouseTheme.Level)((int)currentLevel + 1);
            HomeHouseTheme.HouseItemSpec spec = HomeHouseTheme.GetItemSpec(m_part, nextLevel);
            bool alreadyUnlocked = houseTheme.IsPartLevelUnlocked(m_part, nextLevel);
            bool canAfford = alreadyUnlocked || playerGold >= spec.PriceGold;

            if (m_levelText != null)
                m_levelText.text = $"CẤP {(int)currentLevel} → {(int)nextLevel}";

            if (m_actionIcon != null)
            {
                m_actionIcon.sprite = alreadyUnlocked ? m_checkIcon : m_dollarIcon;
                m_actionIcon.gameObject.SetActive(m_actionIcon.sprite != null);
                m_actionIcon.color = canAfford ? ColAffordPrice : ColLockedPrice;
            }

            if (m_priceText != null)
            {
                m_priceText.text = alreadyUnlocked ? "NÂNG CẤP" : spec.PriceGold.ToString("N0");
                m_priceText.color = canAfford ? ColAffordPrice : ColLockedPrice;
            }

            if (m_button != null)
                m_button.interactable = true;

            if (m_neonStyle != null)
            {
                if (canAfford)
                    m_neonStyle.SetPalette(ColAffordFill, ColAffordOuter, ColAffordInner, ColAffordPrice, Color.white);
                else
                    m_neonStyle.SetPalette(ColLockedFill, ColLockedOuter, ColLockedInner, ColLockedPrice, Color.white);
            }
        }

        if (m_priceRow != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_priceRow);
    }

    public void ShowAnimated(float delay = 0f)
    {
        CaptureBasePosition();
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
        m_feedbackTween = transform.DOPunchScale(Vector3.one * 0.16f, 0.28f, 6, 0.6f).SetUpdate(true);
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

    private void KillTweens()
    {
        m_showHideSeq?.Kill();
        m_showHideSeq = null;

        m_feedbackTween?.Kill();
        m_feedbackTween = null;
    }

    public void ConfigureReferences(
        HomeHouseTheme.Part part,
        Button button,
        NeonButtonStyle neonStyle,
        CanvasGroup canvasGroup,
        TextMeshProUGUI partNameText,
        TextMeshProUGUI levelText,
        Image actionIcon,
        TextMeshProUGUI priceText,
        RectTransform priceRow,
        Sprite dollarIcon,
        Sprite checkIcon)
    {
        m_part = part;
        m_button = button;
        m_neonStyle = neonStyle;
        m_canvasGroup = canvasGroup;
        m_partNameText = partNameText;
        m_levelText = levelText;
        m_actionIcon = actionIcon;
        m_priceText = priceText;
        m_priceRow = priceRow;
        m_dollarIcon = dollarIcon;
        m_checkIcon = checkIcon;
    }
}
