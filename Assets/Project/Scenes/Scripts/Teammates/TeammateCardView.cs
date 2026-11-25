using DG.Tweening;
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

        private Color m_baseBgColor = ColEmptyBg;
        private Color m_baseBorderColor = ColEmptyBorder;
        private Color m_baseBadgeColor = ColEmptyBorder;
        private Color m_basePortraitColor = Color.white;
        private Vector3 m_baseScale = Vector3.one;
        private bool m_baseScaleCached;
        private Sequence m_highlightSeq;

        public void Bind(TeammateCardSO card, TeammateCardThemeSO theme = null, string emptyLabel = "TRỐNG")
        {
            KillHighlightTween(restoreVisuals: false);
            CacheBaseScale();
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

            CacheBaseColors();
        }

        private void CacheBaseScale()
        {
            if (m_baseScaleCached) return;
            m_baseScale = transform.localScale;
            m_baseScaleCached = true;
        }

        private void CacheBaseColors()
        {
            if (m_cardBackground != null) m_baseBgColor = m_cardBackground.color;
            if (m_cardBorder != null) m_baseBorderColor = m_cardBorder.color;
            if (m_typeBadgeBg != null) m_baseBadgeColor = m_typeBadgeBg.color;
            if (m_portraitImage != null) m_basePortraitColor = m_portraitImage.color;
        }

        public void PlaySupportGlow()
        {
            PlayCardHighlight(
                flashBorder: new Color(0.75f, 0.98f, 1f, 1f),
                flashBg: Color.Lerp(m_baseBgColor, new Color(0.35f, 0.88f, 1f, 1f), 0.72f),
                flashPortrait: Color.Lerp(m_basePortraitColor, new Color(0.85f, 0.97f, 1f, 1f), 0.45f),
                scaleMultiplier: 1.1f,
                duration: 0.42f);
        }

        public void PlayAttackPulse()
        {
            PlayCardHighlight(
                flashBorder: new Color(1f, 0.85f, 0.55f, 1f),
                flashBg: Color.Lerp(m_baseBgColor, new Color(1f, 0.45f, 0.25f, 1f), 0.55f),
                flashPortrait: Color.Lerp(m_basePortraitColor, new Color(1f, 0.92f, 0.82f, 1f), 0.35f),
                scaleMultiplier: 1.08f,
                duration: 0.32f);
        }

        private void PlayCardHighlight(Color flashBorder, Color flashBg, Color flashPortrait, float scaleMultiplier, float duration)
        {
            CacheBaseScale();
            KillHighlightTween(restoreVisuals: true);

            if (m_cardBorder != null) m_cardBorder.color = flashBorder;
            if (m_cardBackground != null) m_cardBackground.color = flashBg;
            if (m_typeBadgeBg != null) m_typeBadgeBg.color = Color.white;
            if (m_portraitImage != null && m_portraitImage.enabled) m_portraitImage.color = flashPortrait;

            float upTime = duration * 0.28f;
            float downTime = duration * 0.72f;

            Sequence seq = DOTween.Sequence().SetLink(gameObject);
            seq.Join(DOTween.Sequence()
                .Append(transform.DOScale(m_baseScale * scaleMultiplier, upTime).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(m_baseScale, downTime).SetEase(Ease.OutBack)));

            if (m_cardBorder != null)
                seq.Join(m_cardBorder.DOColor(m_baseBorderColor, duration).SetEase(Ease.OutQuad));
            if (m_cardBackground != null)
                seq.Join(m_cardBackground.DOColor(m_baseBgColor, duration).SetEase(Ease.OutQuad));
            if (m_typeBadgeBg != null)
                seq.Join(m_typeBadgeBg.DOColor(m_baseBadgeColor, duration).SetEase(Ease.OutQuad));
            if (m_portraitImage != null && m_portraitImage.enabled)
                seq.Join(m_portraitImage.DOColor(m_basePortraitColor, duration).SetEase(Ease.OutQuad));

            seq.OnComplete(RestoreBaseVisuals);
            m_highlightSeq = seq;
        }

        private void RestoreBaseVisuals()
        {
            if (m_baseScaleCached) transform.localScale = m_baseScale;
            if (m_cardBackground != null) m_cardBackground.color = m_baseBgColor;
            if (m_cardBorder != null) m_cardBorder.color = m_baseBorderColor;
            if (m_typeBadgeBg != null) m_typeBadgeBg.color = m_baseBadgeColor;
            if (m_portraitImage != null && m_portraitImage.enabled) m_portraitImage.color = m_basePortraitColor;
        }

        public void KillHighlightTween(bool restoreVisuals = true)
        {
            m_highlightSeq?.Kill();
            m_highlightSeq = null;
            transform.DOKill();
            if (m_cardBackground != null) m_cardBackground.DOKill();
            if (m_cardBorder != null) m_cardBorder.DOKill();
            if (m_typeBadgeBg != null) m_typeBadgeBg.DOKill();
            if (m_portraitImage != null) m_portraitImage.DOKill();
            if (restoreVisuals) RestoreBaseVisuals();
        }

        private void OnDestroy()
        {
            KillHighlightTween(restoreVisuals: false);
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
