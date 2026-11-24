using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.Teammates
{
    /// <summary>
    /// Reusable UI view for a Teammate Card.
    /// Separates the reusable card frame/theme (Attack = red, Support = blue) from the character portrait,
    /// so a single card frame design can render any teammate character in both Battle HUD and Home Shop.
    /// </summary>
    public class TeammateCardView : MonoBehaviour
    {
        [SerializeField] private Image m_cardBackground;
        [SerializeField] private Image m_cardBorder;
        [SerializeField] private Image m_portraitImage;
        [SerializeField] private AspectRatioFitter m_portraitFitter;
        [SerializeField] private TextMeshProUGUI m_txtName;
        [SerializeField] private TextMeshProUGUI m_txtStat;
        [SerializeField] private Image m_typeBadgeBg;
        [SerializeField] private TextMeshProUGUI m_txtTypeBadge;
        [SerializeField] private Image m_miniBarFill;
        [SerializeField] private GameObject m_emptyIndicator;

        private static readonly Color ColAttackBg = new Color(0.32f, 0.06f, 0.08f, 0.96f);
        private static readonly Color ColSupportBg = new Color(0.05f, 0.16f, 0.28f, 0.96f);
        private static readonly Color ColEmptyBg = new Color(0.06f, 0.07f, 0.10f, 0.65f);
        private static readonly Color ColAttackBorder = new Color(0.92f, 0.26f, 0.24f, 1f);
        private static readonly Color ColSupportBorder = new Color(0.20f, 0.72f, 0.96f, 1f);
        private static readonly Color ColEmptyBorder = new Color(0.38f, 0.42f, 0.48f, 0.55f);

        public TeammateCardSO CurrentCard { get; private set; }

        public void Bind(TeammateCardSO card, TeammateCardThemeSO theme = null, string emptyLabel = "TRỐNG")
        {
            CurrentCard = card;

            if (card == null)
            {
                ApplyEmptyState(emptyLabel);
                return;
            }

            if (m_emptyIndicator != null) m_emptyIndicator.SetActive(false);

            bool isAttack = card.CardType == TeammateCardType.Attack;

            if (m_cardBackground != null)
            {
                if (theme != null && theme.CardBackgroundSprite != null)
                {
                    m_cardBackground.sprite = theme.CardBackgroundSprite;
                    m_cardBackground.type = Image.Type.Simple;
                    m_cardBackground.color = theme.BackgroundTint;
                }
                else
                {
                    m_cardBackground.color = isAttack ? ColAttackBg : ColSupportBg;
                }
            }

            if (m_cardBorder != null)
            {
                if (theme != null && theme.CardBorderSprite != null)
                {
                    m_cardBorder.sprite = theme.CardBorderSprite;
                }
                m_cardBorder.color = theme != null
                    ? theme.BorderTint
                    : (isAttack ? ColAttackBorder : ColSupportBorder);
            }

            if (m_portraitImage != null)
            {
                m_portraitImage.enabled = card.Avatar != null;
                m_portraitImage.sprite = card.Avatar;
                m_portraitImage.color = Color.white;
                if (m_portraitFitter != null && card.Avatar != null)
                {
                    m_portraitFitter.aspectRatio = card.Avatar.rect.width / card.Avatar.rect.height;
                }
            }

            if (m_txtName != null)
            {
                m_txtName.text = card.DisplayName;
                m_txtName.color = Color.white;
            }

            if (m_txtStat != null)
            {
                m_txtStat.gameObject.SetActive(true);
                m_txtStat.text = card.GetStatShortText();
            }

            if (m_typeBadgeBg != null)
            {
                m_typeBadgeBg.gameObject.SetActive(true);
                m_typeBadgeBg.color = theme != null
                    ? theme.BadgeColor
                    : (isAttack ? ColAttackBorder : ColSupportBorder);
            }

            if (m_txtTypeBadge != null)
            {
                m_txtTypeBadge.text = theme != null && !string.IsNullOrEmpty(theme.TypeLabel)
                    ? theme.TypeLabel
                    : (isAttack ? "CÔNG" : "THỦ");
            }

            if (m_miniBarFill != null)
            {
                m_miniBarFill.enabled = true;
                m_miniBarFill.fillAmount = 1f;
                m_miniBarFill.color = theme != null
                    ? theme.BarFillColor
                    : (isAttack ? ColAttackBorder : ColSupportBorder);
            }
        }

        private void ApplyEmptyState(string emptyLabel)
        {
            if (m_emptyIndicator != null) m_emptyIndicator.SetActive(true);

            if (m_cardBackground != null)
            {
                m_cardBackground.color = ColEmptyBg;
            }
            if (m_cardBorder != null)
            {
                m_cardBorder.color = ColEmptyBorder;
            }
            if (m_portraitImage != null)
            {
                m_portraitImage.enabled = false;
                m_portraitImage.sprite = null;
            }
            if (m_txtName != null)
            {
                m_txtName.text = emptyLabel;
                m_txtName.color = new Color(0.58f, 0.64f, 0.72f, 0.75f);
            }
            if (m_txtStat != null)
            {
                m_txtStat.gameObject.SetActive(false);
            }
            if (m_typeBadgeBg != null)
            {
                m_typeBadgeBg.gameObject.SetActive(false);
            }
            if (m_miniBarFill != null)
            {
                m_miniBarFill.enabled = false;
            }
        }

        public void SetReferences(
            Image cardBackground,
            Image cardBorder,
            Image portraitImage,
            AspectRatioFitter portraitFitter,
            TextMeshProUGUI txtName,
            TextMeshProUGUI txtStat,
            Image typeBadgeBg,
            TextMeshProUGUI txtTypeBadge,
            Image miniBarFill)
        {
            m_cardBackground = cardBackground;
            m_cardBorder = cardBorder;
            m_portraitImage = portraitImage;
            m_portraitFitter = portraitFitter;
            m_txtName = txtName;
            m_txtStat = txtStat;
            m_typeBadgeBg = typeBadgeBg;
            m_txtTypeBadge = txtTypeBadge;
            m_miniBarFill = miniBarFill;
        }
    }
}
