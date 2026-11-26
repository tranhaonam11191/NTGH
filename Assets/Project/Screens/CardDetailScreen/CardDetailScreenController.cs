using System;
using NTGH.Teammates;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// SS.UI screen displaying full details of a Teammate Card when clicking any TeammateCardView.
    /// Shows the framed card showcase, identity (name, title, faction, type, status),
    /// combat effect & skill detail, biography detail, and unlock source.
    /// </summary>
    [DisallowMultipleComponent]
    public class CardDetailScreenController : MonoBehaviour, IKeyBack
    {
        public const string ScreenResourceName = "CardDetailScreen";

        private static readonly Color ColAttackBadge = new Color(0.84f, 0.16f, 0.20f, 1f);
        private static readonly Color ColSupportBadge = new Color(0.14f, 0.66f, 0.90f, 1f);
        private static readonly Color ColEquippedBadge = new Color(0.12f, 0.64f, 0.56f, 1f);
        private static readonly Color ColOwnedBadge = new Color(0.20f, 0.48f, 0.72f, 1f);
        private static readonly Color ColStoryBadge = new Color(0.76f, 0.44f, 0.14f, 1f);
        private static readonly Color ColLockedBadge = new Color(0.32f, 0.36f, 0.44f, 1f);

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI m_txtHeaderTitle;
        [SerializeField] private Button m_btnClose;

        [Header("Card Showcase & Identity")]
        [SerializeField] private TeammateCardView m_cardView;
        [SerializeField] private TextMeshProUGUI m_txtCardName;
        [SerializeField] private TextMeshProUGUI m_txtTitleAndFaction;
        [SerializeField] private Image m_typeBadgeBg;
        [SerializeField] private TextMeshProUGUI m_txtTypeBadge;
        [SerializeField] private Image m_statusBadgeBg;
        [SerializeField] private TextMeshProUGUI m_txtStatusBadge;

        [Header("Detail Sections")]
        [SerializeField] private ScrollRect m_scrollRect;
        [SerializeField] private TextMeshProUGUI m_txtEffectSummary;
        [SerializeField] private TextMeshProUGUI m_txtSkillDetail;
        [SerializeField] private TextMeshProUGUI m_txtDetail;
        [SerializeField] private TextMeshProUGUI m_txtUnlockInfo;

        [Header("Footer")]
        [SerializeField] private Button m_btnConfirmClose;

        private Action m_onClosed;
        private bool m_isClosing;

        public static void Open(
            TeammateCardSO card,
            TeammateCardThemeSO theme = null,
            Canvas callerCanvas = null,
            Action onClosed = null)
        {
            if (card == null)
            {
                onClosed?.Invoke();
                return;
            }

            if (Resources.Load<ScreenReference>("Screens/" + ScreenResourceName) == null)
            {
                Debug.LogWarning("[CardDetailScreenController] Missing Resources/Screens/CardDetailScreen.asset.");
                onClosed?.Invoke();
                return;
            }

            if (theme == null && SOManager.instance != null && SOManager.instance.TeammateDatabase != null)
            {
                theme = SOManager.instance.TeammateDatabase.GetTheme(card.CardType);
            }

            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<CardDetailScreenController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: screen => screen.Setup(card, theme, onClosed),
                hasShield: true,
                shieldAlpha: 0.78f,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_cardView != null)
            {
                m_cardView.OpenDetailOnClick = false;
            }

            if (m_btnClose != null)
            {
                m_btnClose.onClick.AddListener(CloseScreen);
            }

            if (m_btnConfirmClose != null)
            {
                m_btnConfirmClose.onClick.AddListener(CloseScreen);
            }
        }

        private void OnDestroy()
        {
            if (m_btnClose != null)
            {
                m_btnClose.onClick.RemoveListener(CloseScreen);
            }

            if (m_btnConfirmClose != null)
            {
                m_btnConfirmClose.onClick.RemoveListener(CloseScreen);
            }
        }

        public void Setup(TeammateCardSO card, TeammateCardThemeSO theme = null, Action onClosed = null)
        {
            m_onClosed = onClosed;
            m_isClosing = false;

            if (card == null)
            {
                CloseScreen();
                return;
            }

            if (theme == null && SOManager.instance != null && SOManager.instance.TeammateDatabase != null)
            {
                theme = SOManager.instance.TeammateDatabase.GetTheme(card.CardType);
            }

            if (m_txtHeaderTitle != null)
            {
                m_txtHeaderTitle.text = "CHI TIẾT ĐỒNG ĐỘI";
            }

            if (m_cardView != null)
            {
                m_cardView.OpenDetailOnClick = false;
                m_cardView.Bind(card, theme);
            }

            if (m_txtCardName != null)
            {
                m_txtCardName.text = card.DisplayName;
            }

            if (m_txtTitleAndFaction != null)
            {
                string subtitle = BuildSubtitle(card);
                bool hasSubtitle = !string.IsNullOrWhiteSpace(subtitle);
                m_txtTitleAndFaction.gameObject.SetActive(hasSubtitle);
                if (hasSubtitle)
                {
                    m_txtTitleAndFaction.text = subtitle;
                }
            }

            bool isAttack = card.CardType == TeammateCardType.Attack;
            if (m_typeBadgeBg != null)
            {
                m_typeBadgeBg.color = theme != null
                    ? theme.BadgeColor
                    : (isAttack ? ColAttackBadge : ColSupportBadge);
            }

            if (m_txtTypeBadge != null)
            {
                m_txtTypeBadge.text = card.GetCardTypeDisplayName();
            }

            BindOwnershipStatus(card);

            if (m_txtEffectSummary != null)
            {
                m_txtEffectSummary.text = $"{card.GetEffectSummary()}  ({card.GetStatShortText()})";
                m_txtEffectSummary.color = isAttack
                    ? new Color(1f, 0.56f, 0.46f, 1f)
                    : new Color(0.46f, 0.88f, 1f, 1f);
            }

            if (m_txtSkillDetail != null)
            {
                m_txtSkillDetail.text = card.GetSkillDetailText();
            }

            if (m_txtDetail != null)
            {
                m_txtDetail.text = card.GetDetailText();
            }

            if (m_txtUnlockInfo != null)
            {
                m_txtUnlockInfo.text = card.GetUnlockSourceText();
            }

            if (m_scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                m_scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private static string BuildSubtitle(TeammateCardSO card)
        {
            bool hasTitle = !string.IsNullOrWhiteSpace(card.Title);
            bool hasFaction = !string.IsNullOrWhiteSpace(card.Faction);

            if (hasTitle && hasFaction)
            {
                return $"{card.Title}  •  {card.Faction}";
            }
            if (hasTitle)
            {
                return card.Title;
            }
            if (hasFaction)
            {
                return card.Faction;
            }
            return string.Empty;
        }

        private void BindOwnershipStatus(TeammateCardSO card)
        {
            if (m_statusBadgeBg == null && m_txtStatusBadge == null) return;

            User.EnsureLoaded();
            bool unlocked = User.data != null && User.data.IsTeammateUnlocked(card.Id);
            bool equipped = User.data != null && User.data.IsTeammateEquipped(card.Id);

            string label;
            Color color;
            if (equipped)
            {
                label = "ĐANG XUẤT TRẬN";
                color = ColEquippedBadge;
            }
            else if (unlocked)
            {
                label = "ĐÃ SỞ HỮU";
                color = ColOwnedBadge;
            }
            else if (card.StoryOnly)
            {
                label = "CỐT TRUYỆN";
                color = ColStoryBadge;
            }
            else
            {
                label = "CHƯA MỞ KHÓA";
                color = ColLockedBadge;
            }

            if (m_statusBadgeBg != null) m_statusBadgeBg.color = color;
            if (m_txtStatusBadge != null) m_txtStatusBadge.text = label;
        }

        public void OnKeyBack()
        {
            CloseScreen();
        }

        public void CloseScreen()
        {
            if (m_isClosing) return;
            m_isClosing = true;

            if (SOManager.instance != null)
            {
                SOManager.instance.PlayAudio("Click2");
            }

            var onClosed = m_onClosed;
            m_onClosed = null;
            Core.Close(this, () => onClosed?.Invoke());
        }
    }
}
