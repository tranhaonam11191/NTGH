using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    public enum CustomizeItemVisualState
    {
        Equipped = 0,
        Unlocked = 1,
        LockedGold = 2,
        LockedWatchAd = 3
    }

    public struct CustomizeCardViewModel
    {
        public string Title;
        public string Subtitle;
        public Sprite PreviewSprite;
        public CustomizeItemVisualState State;
        public string ActionText;
        public bool CanAffordGold;
        public int AdsWatched;
        public int AdsRequired;
    }

    [DisallowMultipleComponent]
    public class CustomizeItemCardView : MonoBehaviour
    {
        [Header("Card Frame (Neon 90s 9-Slice)")]
        [SerializeField] private Image m_cardFill;
        [SerializeField] private Image m_cardOuterBorder;
        [SerializeField] private Image m_cardInnerBorder;

        [Header("Preview Area")]
        [SerializeField] private Image m_previewBg;
        [SerializeField] private Image m_previewImage;
        [SerializeField] private GameObject m_lockedOverlay;
        [SerializeField] private Image m_lockCenterIcon;

        [Header("Top Status Tag & Badge")]
        [SerializeField] private Image m_statusTagBg;
        [SerializeField] private TextMeshProUGUI m_statusTagText;
        [SerializeField] private Image m_statusBadgeBg;
        [SerializeField] private Image m_statusBadgeIcon;

        [Header("Text Info")]
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private TextMeshProUGUI m_subtitleText;

        [Header("Action Button")]
        [SerializeField] private Button m_actionButton;
        [SerializeField] private Image m_actionFill;
        [SerializeField] private Image m_actionOuterBorder;
        [SerializeField] private Image m_actionInnerBorder;
        [SerializeField] private Image m_actionIcon;
        [SerializeField] private TextMeshProUGUI m_actionLabel;

        [Header("Icon Sprites")]
        [SerializeField] private Sprite m_spriteDollar;
        [SerializeField] private Sprite m_spriteAd;
        [SerializeField] private Sprite m_spriteCheck;
        [SerializeField] private Sprite m_spriteLock;

        // Neon 90s Hong Kong palette
        private static readonly Color ColCardFillDefault = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        private static readonly Color ColCardFillEquipped = new Color(0.06f, 0.15f, 0.18f, 0.96f);
        private static readonly Color ColCardFillLocked = new Color(0.05f, 0.06f, 0.09f, 0.94f);

        private static readonly Color ColCyan = new Color(0.16f, 0.92f, 0.88f, 1f);
        private static readonly Color ColEmerald = new Color(0.16f, 0.94f, 0.58f, 1f);
        private static readonly Color ColGold = new Color(1.00f, 0.78f, 0.28f, 1f);
        private static readonly Color ColMagenta = new Color(1.00f, 0.24f, 0.58f, 1f);
        private static readonly Color ColCrimson = new Color(0.95f, 0.25f, 0.32f, 1f);
        private static readonly Color ColCream = new Color(1.00f, 0.96f, 0.86f, 1f);
        private static readonly Color ColMutedText = new Color(0.70f, 0.75f, 0.82f, 1f);
        private static readonly Color ColDimmedPreview = new Color(0.42f, 0.44f, 0.50f, 1f);

        private Action m_onActionClicked;
        private Tween m_feedbackTween;

        private void Awake()
        {
            if (m_actionButton != null)
            {
                m_actionButton.onClick.AddListener(HandleActionClick);
            }
        }

        private void OnDestroy()
        {
            m_feedbackTween?.Kill();
            m_feedbackTween = null;

            if (m_actionButton != null)
            {
                m_actionButton.onClick.RemoveListener(HandleActionClick);
            }
        }

        public void Bind(CustomizeCardViewModel model, Action onActionClicked)
        {
            m_onActionClicked = onActionClicked;

            if (m_titleText != null)
                m_titleText.text = model.Title;

            if (m_subtitleText != null)
                m_subtitleText.text = model.Subtitle;

            if (m_previewImage != null)
            {
                m_previewImage.sprite = model.PreviewSprite;
                m_previewImage.preserveAspect = true;
                m_previewImage.enabled = model.PreviewSprite != null;
            }

            ApplyVisualState(model);
        }

        private void ApplyVisualState(CustomizeCardViewModel model)
        {
            bool isLocked = model.State == CustomizeItemVisualState.LockedGold ||
                            model.State == CustomizeItemVisualState.LockedWatchAd;

            if (m_lockedOverlay != null)
                m_lockedOverlay.SetActive(isLocked);

            if (m_previewImage != null)
                m_previewImage.color = isLocked ? ColDimmedPreview : Color.white;

            switch (model.State)
            {
                case CustomizeItemVisualState.Equipped:
                    ApplyColors(
                        cardFill: ColCardFillEquipped,
                        cardOuter: ColCyan,
                        cardInner: ColGold,
                        previewBg: new Color(0.06f, 0.18f, 0.22f, 0.85f),
                        tagBg: new Color(0.07f, 0.36f, 0.38f, 0.95f),
                        tagText: "ĐANG DÙNG",
                        tagTextColor: ColCyan,
                        badgeBg: ColCyan,
                        badgeIcon: m_spriteCheck,
                        badgeIconColor: new Color(0.04f, 0.08f, 0.10f, 1f),
                        actionFill: new Color(0.07f, 0.24f, 0.26f, 0.95f),
                        actionOuter: ColCyan,
                        actionInner: ColGold,
                        actionIcon: m_spriteCheck,
                        actionIconColor: ColCyan,
                        actionText: string.IsNullOrEmpty(model.ActionText) ? "ĐANG DÙNG" : model.ActionText,
                        actionTextColor: ColCyan,
                        interactable: false);
                    break;

                case CustomizeItemVisualState.Unlocked:
                    ApplyColors(
                        cardFill: ColCardFillDefault,
                        cardOuter: ColEmerald,
                        cardInner: new Color(0.16f, 0.94f, 0.58f, 0.45f),
                        previewBg: new Color(0.05f, 0.14f, 0.14f, 0.82f),
                        tagBg: new Color(0.06f, 0.28f, 0.20f, 0.95f),
                        tagText: "ĐÃ MỞ",
                        tagTextColor: ColEmerald,
                        badgeBg: ColEmerald,
                        badgeIcon: m_spriteCheck,
                        badgeIconColor: new Color(0.04f, 0.10f, 0.08f, 1f),
                        actionFill: new Color(0.07f, 0.26f, 0.19f, 0.96f),
                        actionOuter: ColEmerald,
                        actionInner: ColGold,
                        actionIcon: null,
                        actionIconColor: Color.white,
                        actionText: string.IsNullOrEmpty(model.ActionText) ? "CHỌN" : model.ActionText,
                        actionTextColor: ColCream,
                        interactable: true);
                    break;

                case CustomizeItemVisualState.LockedGold:
                {
                    Color accent = model.CanAffordGold ? ColGold : ColCrimson;
                    Color fillBtn = model.CanAffordGold
                        ? new Color(0.24f, 0.18f, 0.05f, 0.96f)
                        : new Color(0.22f, 0.07f, 0.09f, 0.96f);

                    if (m_lockCenterIcon != null)
                    {
                        m_lockCenterIcon.sprite = m_spriteLock;
                        m_lockCenterIcon.color = accent;
                    }

                    ApplyColors(
                        cardFill: ColCardFillLocked,
                        cardOuter: accent,
                        cardInner: new Color(accent.r, accent.g, accent.b, 0.42f),
                        previewBg: new Color(0.09f, 0.08f, 0.11f, 0.90f),
                        tagBg: new Color(0.24f, 0.16f, 0.04f, 0.95f),
                        tagText: "MỞ BẰNG TIỀN",
                        tagTextColor: ColGold,
                        badgeBg: accent,
                        badgeIcon: m_spriteLock,
                        badgeIconColor: new Color(0.08f, 0.06f, 0.04f, 1f),
                        actionFill: fillBtn,
                        actionOuter: accent,
                        actionInner: ColGold,
                        actionIcon: m_spriteDollar,
                        actionIconColor: model.CanAffordGold ? ColEmerald : ColCrimson,
                        actionText: model.ActionText,
                        actionTextColor: model.CanAffordGold ? ColCream : new Color(1f, 0.72f, 0.74f, 1f),
                        interactable: true);
                    break;
                }

                case CustomizeItemVisualState.LockedWatchAd:
                {
                    if (m_lockCenterIcon != null)
                    {
                        m_lockCenterIcon.sprite = m_spriteLock;
                        m_lockCenterIcon.color = ColMagenta;
                    }

                    string tag = model.AdsRequired > 1
                        ? $"XEM QC ({model.AdsWatched}/{model.AdsRequired})"
                        : "XEM QUẢNG CÁO";

                    ApplyColors(
                        cardFill: ColCardFillLocked,
                        cardOuter: ColMagenta,
                        cardInner: new Color(ColCyan.r, ColCyan.g, ColCyan.b, 0.45f),
                        previewBg: new Color(0.12f, 0.06f, 0.13f, 0.90f),
                        tagBg: new Color(0.28f, 0.06f, 0.18f, 0.95f),
                        tagText: tag,
                        tagTextColor: new Color(1.00f, 0.56f, 0.78f, 1f),
                        badgeBg: ColMagenta,
                        badgeIcon: m_spriteLock,
                        badgeIconColor: ColCream,
                        actionFill: new Color(0.28f, 0.06f, 0.18f, 0.96f),
                        actionOuter: ColMagenta,
                        actionInner: ColCyan,
                        actionIcon: m_spriteAd,
                        actionIconColor: ColCream,
                        actionText: model.ActionText,
                        actionTextColor: ColCream,
                        interactable: true);
                    break;
                }
            }

            if (m_subtitleText != null)
            {
                m_subtitleText.color = isLocked ? ColMutedText : ColGold;
            }
        }

        private void ApplyColors(
            Color cardFill,
            Color cardOuter,
            Color cardInner,
            Color previewBg,
            Color tagBg,
            string tagText,
            Color tagTextColor,
            Color badgeBg,
            Sprite badgeIcon,
            Color badgeIconColor,
            Color actionFill,
            Color actionOuter,
            Color actionInner,
            Sprite actionIcon,
            Color actionIconColor,
            string actionText,
            Color actionTextColor,
            bool interactable)
        {
            if (m_cardFill != null) m_cardFill.color = cardFill;
            if (m_cardOuterBorder != null) m_cardOuterBorder.color = cardOuter;
            if (m_cardInnerBorder != null) m_cardInnerBorder.color = cardInner;
            if (m_previewBg != null) m_previewBg.color = previewBg;

            if (m_statusTagBg != null) m_statusTagBg.color = tagBg;
            if (m_statusTagText != null)
            {
                m_statusTagText.text = tagText;
                m_statusTagText.color = tagTextColor;
            }

            if (m_statusBadgeBg != null) m_statusBadgeBg.color = badgeBg;
            if (m_statusBadgeIcon != null)
            {
                m_statusBadgeIcon.sprite = badgeIcon;
                m_statusBadgeIcon.color = badgeIconColor;
                m_statusBadgeIcon.enabled = badgeIcon != null;
            }

            if (m_actionFill != null) m_actionFill.color = actionFill;
            if (m_actionOuterBorder != null) m_actionOuterBorder.color = actionOuter;
            if (m_actionInnerBorder != null) m_actionInnerBorder.color = actionInner;

            if (m_actionIcon != null)
            {
                bool showIcon = actionIcon != null;
                m_actionIcon.gameObject.SetActive(showIcon);
                if (showIcon)
                {
                    m_actionIcon.sprite = actionIcon;
                    m_actionIcon.color = actionIconColor;
                }
            }

            if (m_actionLabel != null)
            {
                m_actionLabel.text = actionText;
                m_actionLabel.color = actionTextColor;
                if (m_actionLabel.transform.parent is RectTransform contentRt)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
                }
            }

            if (m_actionButton != null)
                m_actionButton.interactable = interactable;
        }

        public void PlayUnlockPunch()
        {
            m_feedbackTween?.Kill();
            transform.localScale = Vector3.one;
            m_feedbackTween = transform.DOPunchScale(Vector3.one * 0.08f, 0.26f, 6, 0.6f);
        }

        public void PlayErrorShake()
        {
            if (m_actionButton == null) return;
            RectTransform rt = m_actionButton.GetComponent<RectTransform>();
            if (rt == null) return;

            m_feedbackTween?.Kill();
            m_feedbackTween = rt.DOShakeAnchorPos(0.28f, new Vector2(10f, 0f), 18, 0f, false, true);
        }

        private void HandleActionClick()
        {
            m_onActionClicked?.Invoke();
        }

        public void ConfigureReferences(
            Image cardFill,
            Image cardOuterBorder,
            Image cardInnerBorder,
            Image previewBg,
            Image previewImage,
            GameObject lockedOverlay,
            Image lockCenterIcon,
            Image statusTagBg,
            TextMeshProUGUI statusTagText,
            Image statusBadgeBg,
            Image statusBadgeIcon,
            TextMeshProUGUI titleText,
            TextMeshProUGUI subtitleText,
            Button actionButton,
            Image actionFill,
            Image actionOuterBorder,
            Image actionInnerBorder,
            Image actionIcon,
            TextMeshProUGUI actionLabel,
            Sprite spriteDollar,
            Sprite spriteAd,
            Sprite spriteCheck,
            Sprite spriteLock)
        {
            m_cardFill = cardFill;
            m_cardOuterBorder = cardOuterBorder;
            m_cardInnerBorder = cardInnerBorder;
            m_previewBg = previewBg;
            m_previewImage = previewImage;
            m_lockedOverlay = lockedOverlay;
            m_lockCenterIcon = lockCenterIcon;
            m_statusTagBg = statusTagBg;
            m_statusTagText = statusTagText;
            m_statusBadgeBg = statusBadgeBg;
            m_statusBadgeIcon = statusBadgeIcon;
            m_titleText = titleText;
            m_subtitleText = subtitleText;
            m_actionButton = actionButton;
            m_actionFill = actionFill;
            m_actionOuterBorder = actionOuterBorder;
            m_actionInnerBorder = actionInnerBorder;
            m_actionIcon = actionIcon;
            m_actionLabel = actionLabel;
            m_spriteDollar = spriteDollar;
            m_spriteAd = spriteAd;
            m_spriteCheck = spriteCheck;
            m_spriteLock = spriteLock;
        }
    }
}
