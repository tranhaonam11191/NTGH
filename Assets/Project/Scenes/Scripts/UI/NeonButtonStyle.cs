using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// Controls color tinting, optional idle pulse, and tactile press-down + neon glow-up
    /// feedback for Neon 90s UI buttons.
    /// Lives on NeonButton_Base and is inherited by NeonButton_Text, NeonButton_Icon,
    /// NeonButton_IconText, popup tabs, close buttons, and card CTA buttons.
    /// </summary>
    [DisallowMultipleComponent]
    public class NeonButtonStyle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Frame Layers (Pure White 9-Slice Sprites)")]
        [SerializeField] private Image m_fill;
        [SerializeField] private Image m_outerBorder;
        [SerializeField] private Image m_innerBorder;

        [Header("Optional Content Layers (Variant-Specific)")]
        [SerializeField] private Image m_icon;
        [SerializeField] private TextMeshProUGUI m_label;
        [SerializeField] private Image m_labelFill;
        [SerializeField] private Image m_labelBorder;
        [SerializeField] private Image m_badgeDot;

        [Header("Neon Color Palette")]
        [SerializeField] private Color m_fillColor = new Color(0.08f, 0.10f, 0.12f, 0.88f);
        [SerializeField] private Color m_outerBorderColor = new Color(0.16f, 0.90f, 0.86f, 1f);
        [SerializeField] private Color m_innerBorderColor = new Color(1.00f, 0.78f, 0.30f, 1f);
        [SerializeField] private Color m_iconColor = new Color(1.00f, 0.86f, 0.48f, 1f);
        [SerializeField] private Color m_labelColor = new Color(1.00f, 0.96f, 0.86f, 1f);
        [SerializeField] private Color m_labelFillColor = new Color(0.08f, 0.08f, 0.10f, 0.92f);
        [SerializeField] private Color m_labelBorderColor = new Color(0.16f, 0.90f, 0.86f, 1f);
        [SerializeField] private Color m_badgeColor = new Color(1.00f, 0.18f, 0.22f, 1f);
        [SerializeField] private bool m_showBadge;

        [Header("Idle Animation")]
        [SerializeField] private bool m_enablePulse;
        [SerializeField] private float m_pulseScale = 1.035f;
        [SerializeField] private float m_pulseDuration = 0.85f;

        [Header("Press Feedback Animation")]
        [SerializeField] private bool m_enablePressFeedback = true;
        [SerializeField] private float m_pressScale = 0.92f;
        [SerializeField] private float m_pressDuration = 0.07f;
        [SerializeField] private float m_releaseDuration = 0.16f;
        [SerializeField, Range(0f, 1f)] private float m_neonGlowBoost = 0.48f;
        [SerializeField, Range(0f, 1f)] private float m_fillGlowBoost = 0.26f;

        private Button m_button;
        private Vector3 m_baseScale = Vector3.one;
        private bool m_hasCapturedBaseScale;
        private bool m_isPressed;
        private Tween m_pulseTween;
        private Tween m_scaleTween;
        private Sequence m_colorSequence;

        public Image FillImage => m_fill;
        public Image OuterBorderImage => m_outerBorder;
        public Image InnerBorderImage => m_innerBorder;
        public Image IconImage => m_icon;
        public TextMeshProUGUI LabelText => m_label;

        private void Awake()
        {
            m_button = GetComponent<Button>();
            if (m_button != null && m_enablePressFeedback && m_button.transition == Selectable.Transition.ColorTint)
            {
                // Avoid Unity's default gray ColorTint darkening our neon glow on press.
                m_button.transition = Selectable.Transition.None;
            }

            CaptureBaseScale();
            ApplyStyle();
        }

        private void OnEnable()
        {
            m_isPressed = false;
            if (Application.isPlaying)
            {
                CaptureBaseScale();
                transform.localScale = m_baseScale;
                ApplyStyle();
                StartIdlePulseIfEnabled();
            }
        }

        private void OnDisable()
        {
            m_isPressed = false;
            KillAllTweens();
            if (Application.isPlaying)
            {
                if (m_hasCapturedBaseScale)
                    transform.localScale = m_baseScale;
                ApplyStyle();
            }
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void OnValidate()
        {
            ApplyStyle();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!m_enablePressFeedback || !Application.isPlaying)
                return;

            if (m_button != null && !m_button.interactable)
                return;

            m_isPressed = true;
            CaptureBaseScale();

            m_pulseTween?.Kill();
            m_pulseTween = null;

            m_scaleTween?.Kill();
            m_scaleTween = transform
                .DOScale(m_baseScale * m_pressScale, m_pressDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            TweenColors(pressed: true, duration: m_pressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ReleasePress();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ReleasePress();
        }

        private void ReleasePress()
        {
            if (!m_isPressed)
                return;

            m_isPressed = false;

            m_scaleTween?.Kill();
            m_scaleTween = transform
                .DOScale(m_baseScale, m_releaseDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .OnComplete(StartIdlePulseIfEnabled);

            TweenColors(pressed: false, duration: m_releaseDuration);
        }

        private void TweenColors(bool pressed, float duration)
        {
            m_colorSequence?.Kill();
            m_colorSequence = DOTween.Sequence().SetUpdate(true);

            Color fillTarget = pressed ? BrightenFill(m_fillColor, m_outerBorderColor, m_fillGlowBoost) : m_fillColor;
            Color outerTarget = pressed ? BrightenNeon(m_outerBorderColor, m_neonGlowBoost) : m_outerBorderColor;
            Color innerTarget = pressed ? BrightenNeon(m_innerBorderColor, m_neonGlowBoost) : m_innerBorderColor;
            Color iconTarget = pressed ? BrightenNeon(m_iconColor, m_neonGlowBoost * 0.85f) : m_iconColor;
            Color labelTarget = pressed ? BrightenNeon(m_labelColor, m_neonGlowBoost * 0.65f) : m_labelColor;
            Color labelFillTarget = pressed ? BrightenFill(m_labelFillColor, m_labelBorderColor, m_fillGlowBoost) : m_labelFillColor;
            Color labelBorderTarget = pressed ? BrightenNeon(m_labelBorderColor, m_neonGlowBoost) : m_labelBorderColor;

            Ease ease = pressed ? Ease.OutQuad : Ease.OutSine;

            if (m_fill != null) m_colorSequence.Join(m_fill.DOColor(fillTarget, duration).SetEase(ease));
            if (m_outerBorder != null) m_colorSequence.Join(m_outerBorder.DOColor(outerTarget, duration).SetEase(ease));
            if (m_innerBorder != null) m_colorSequence.Join(m_innerBorder.DOColor(innerTarget, duration).SetEase(ease));
            if (m_icon != null) m_colorSequence.Join(m_icon.DOColor(iconTarget, duration).SetEase(ease));
            if (m_label != null) m_colorSequence.Join(m_label.DOColor(labelTarget, duration).SetEase(ease));
            if (m_labelFill != null) m_colorSequence.Join(m_labelFill.DOColor(labelFillTarget, duration).SetEase(ease));
            if (m_labelBorder != null) m_colorSequence.Join(m_labelBorder.DOColor(labelBorderTarget, duration).SetEase(ease));
        }

        private static Color BrightenNeon(Color baseColor, float boost)
        {
            Color boosted = new Color(
                Mathf.Min(1f, baseColor.r * 1.28f),
                Mathf.Min(1f, baseColor.g * 1.28f),
                Mathf.Min(1f, baseColor.b * 1.28f),
                Mathf.Min(1f, baseColor.a + 0.12f));

            Color luminousWhite = new Color(1f, 1f, 1f, Mathf.Min(1f, baseColor.a + 0.15f));
            return Color.Lerp(boosted, luminousWhite, boost);
        }

        private static Color BrightenFill(Color fillColor, Color neonAccent, float boost)
        {
            Color infused = new Color(
                Mathf.Clamp01(fillColor.r + neonAccent.r * boost * 0.65f + boost * 0.12f),
                Mathf.Clamp01(fillColor.g + neonAccent.g * boost * 0.65f + boost * 0.12f),
                Mathf.Clamp01(fillColor.b + neonAccent.b * boost * 0.65f + boost * 0.12f),
                Mathf.Min(1f, fillColor.a + 0.06f));
            return infused;
        }

        private void CaptureBaseScale()
        {
            if (m_hasCapturedBaseScale)
                return;

            Vector3 current = transform.localScale;
            m_baseScale = current.sqrMagnitude > 0.0001f ? current : Vector3.one;
            m_hasCapturedBaseScale = true;
        }

        private void StartIdlePulseIfEnabled()
        {
            if (!m_enablePulse || !Application.isPlaying || !isActiveAndEnabled || m_isPressed)
                return;

            m_pulseTween?.Kill();
            transform.localScale = m_baseScale;
            m_pulseTween = transform
                .DOScale(m_baseScale * m_pulseScale, m_pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void KillAllTweens()
        {
            m_pulseTween?.Kill();
            m_pulseTween = null;

            m_scaleTween?.Kill();
            m_scaleTween = null;

            m_colorSequence?.Kill();
            m_colorSequence = null;
        }

        public void ApplyStyle()
        {
            if (m_fill != null) m_fill.color = m_fillColor;
            if (m_outerBorder != null) m_outerBorder.color = m_outerBorderColor;
            if (m_innerBorder != null) m_innerBorder.color = m_innerBorderColor;
            if (m_icon != null) m_icon.color = m_iconColor;
            if (m_label != null) m_label.color = m_labelColor;
            if (m_labelFill != null) m_labelFill.color = m_labelFillColor;
            if (m_labelBorder != null) m_labelBorder.color = m_labelBorderColor;
            if (m_badgeDot != null)
            {
                m_badgeDot.color = m_badgeColor;
                m_badgeDot.gameObject.SetActive(m_showBadge);
            }
        }

        public void ConfigureReferences(
            Image fill,
            Image outerBorder,
            Image innerBorder,
            Image icon = null,
            TextMeshProUGUI label = null,
            Image labelFill = null,
            Image labelBorder = null,
            Image badgeDot = null)
        {
            m_fill = fill;
            m_outerBorder = outerBorder;
            m_innerBorder = innerBorder;
            if (icon != null) m_icon = icon;
            if (label != null) m_label = label;
            if (labelFill != null) m_labelFill = labelFill;
            if (labelBorder != null) m_labelBorder = labelBorder;
            if (badgeDot != null) m_badgeDot = badgeDot;
        }

        public void SetPalette(
            Color fillColor,
            Color outerBorderColor,
            Color innerBorderColor,
            Color iconColor,
            Color labelColor,
            Color? labelFillColor = null,
            Color? labelBorderColor = null)
        {
            m_fillColor = fillColor;
            m_outerBorderColor = outerBorderColor;
            m_innerBorderColor = innerBorderColor;
            m_iconColor = iconColor;
            m_labelColor = labelColor;
            m_labelFillColor = labelFillColor ?? fillColor;
            m_labelBorderColor = labelBorderColor ?? outerBorderColor;

            if (!m_isPressed)
            {
                m_colorSequence?.Kill();
                m_colorSequence = null;
                ApplyStyle();
            }
        }

        public void SetIcon(Sprite sprite)
        {
            if (m_icon != null)
            {
                m_icon.sprite = sprite;
            }
        }

        public void SetLabel(string text)
        {
            if (m_label != null)
            {
                m_label.text = text;
            }
        }

        public void SetBadgeVisible(bool visible)
        {
            m_showBadge = visible;
            if (m_badgeDot != null)
            {
                m_badgeDot.gameObject.SetActive(visible);
            }
        }

        public void SetPulseEnabled(bool enabled)
        {
            m_enablePulse = enabled;
        }
    }
}
