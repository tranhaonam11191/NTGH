using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// Controls color tinting and optional idle pulse for Neon 90s UI buttons.
    /// Lives on NeonButton_Base and is inherited by NeonButton_Text, NeonButton_Icon,
    /// and NeonButton_IconText prefab variants.
    /// </summary>
    [DisallowMultipleComponent]
    public class NeonButtonStyle : MonoBehaviour
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

        private Tween m_pulseTween;

        public Image FillImage => m_fill;
        public Image OuterBorderImage => m_outerBorder;
        public Image InnerBorderImage => m_innerBorder;
        public Image IconImage => m_icon;
        public TextMeshProUGUI LabelText => m_label;

        private void Awake()
        {
            ApplyStyle();
        }

        private void OnEnable()
        {
            if (m_enablePulse && Application.isPlaying)
            {
                m_pulseTween?.Kill();
                transform.localScale = Vector3.one;
                m_pulseTween = transform
                    .DOScale(m_pulseScale, m_pulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void OnDisable()
        {
            m_pulseTween?.Kill();
            m_pulseTween = null;
            if (Application.isPlaying)
            {
                transform.localScale = Vector3.one;
            }
        }

        private void OnDestroy()
        {
            m_pulseTween?.Kill();
            m_pulseTween = null;
        }

        private void OnValidate()
        {
            ApplyStyle();
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
            ApplyStyle();
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
