using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.Boosters
{
    public enum BoosterSlotVisualState
    {
        Locked,
        Available,
        Empty
    }

    [DisallowMultipleComponent]
    public class BoosterSlotView : MonoBehaviour
    {
        private static readonly Color GreenBadgeColor = new Color32(34, 197, 94, 255);
        private static readonly Color RedBadgeColor = new Color32(239, 68, 68, 255);
        private static readonly Color LockedIconTint = new Color(0.32f, 0.35f, 0.42f, 0.72f);
        private static readonly Color BusyIconTint = new Color(0.72f, 0.76f, 0.84f, 0.88f);

        [SerializeField] private BoosterType m_boosterType = BoosterType.Hammer;
        [SerializeField] private Button m_button;
        [SerializeField] private Image m_iconImage;
        [SerializeField] private GameObject m_lockOverlay;
        [SerializeField] private TextMeshProUGUI m_lockLevelText;
        [SerializeField] private GameObject m_badgeRoot;
        [SerializeField] private Image m_badgeCircleImage;
        [SerializeField] private TextMeshProUGUI m_badgeCountText;
        [SerializeField] private GameObject m_activeHighlight;

        private Action<BoosterType> m_onClicked;
        private Tween m_pulseTween;
        private Vector3 m_baseScale = Vector3.one;
        private bool m_baseScaleCached;

        public BoosterType BoosterType => m_boosterType;
        public BoosterSlotVisualState CurrentState { get; private set; }

        private void Awake()
        {
            CacheBaseScale();
            if (m_button == null)
            {
                m_button = GetComponent<Button>();
            }
            if (m_iconImage == null)
            {
                m_iconImage = GetComponent<Image>();
            }
            if (m_button != null)
            {
                m_button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDisable()
        {
            KillPulseTween(restoreScale: true);
        }

        private void OnDestroy()
        {
            KillPulseTween(restoreScale: false);
            if (m_button != null)
            {
                m_button.onClick.RemoveListener(HandleClick);
            }
        }

        private void CacheBaseScale()
        {
            if (m_baseScaleCached) return;
            m_baseScale = transform.localScale;
            m_baseScaleCached = true;
        }

        public void Bind(BoosterType type, Action<BoosterType> onClicked)
        {
            m_boosterType = type;
            m_onClicked = onClicked;
        }

        public void RefreshState(BoosterConfig config, bool isUnlocked, int count, bool isTargeting, bool isBoardBusy)
        {
            CacheBaseScale();

            if (config != null && config.icon != null && m_iconImage != null)
            {
                m_iconImage.sprite = config.icon;
            }

            if (!isUnlocked)
            {
                CurrentState = BoosterSlotVisualState.Locked;

                if (m_iconImage != null)
                    m_iconImage.color = LockedIconTint;

                if (m_lockOverlay != null)
                    m_lockOverlay.SetActive(true);

                if (m_lockLevelText != null)
                    m_lockLevelText.text = config != null ? $"Lv.{config.unlockLevel}" : "LOCK";

                if (m_badgeRoot != null)
                    m_badgeRoot.SetActive(false);

                if (m_activeHighlight != null)
                    m_activeHighlight.SetActive(false);

                KillPulseTween(restoreScale: true);
                return;
            }

            if (m_lockOverlay != null)
                m_lockOverlay.SetActive(false);

            if (m_badgeRoot != null)
                m_badgeRoot.SetActive(true);

            if (count > 0)
            {
                CurrentState = BoosterSlotVisualState.Available;

                if (m_iconImage != null)
                    m_iconImage.color = isBoardBusy ? BusyIconTint : Color.white;

                if (m_badgeCircleImage != null)
                    m_badgeCircleImage.color = GreenBadgeColor;

                if (m_badgeCountText != null)
                    m_badgeCountText.text = count.ToString();

                if (m_activeHighlight != null)
                    m_activeHighlight.SetActive(isTargeting);

                if (isTargeting)
                    StartPulseTween();
                else
                    KillPulseTween(restoreScale: true);
            }
            else
            {
                CurrentState = BoosterSlotVisualState.Empty;

                if (m_iconImage != null)
                    m_iconImage.color = isBoardBusy ? BusyIconTint : Color.white;

                if (m_badgeCircleImage != null)
                    m_badgeCircleImage.color = RedBadgeColor;

                if (m_badgeCountText != null)
                    m_badgeCountText.text = "+";

                if (m_activeHighlight != null)
                    m_activeHighlight.SetActive(false);

                KillPulseTween(restoreScale: true);
            }
        }

        private void StartPulseTween()
        {
            if (m_pulseTween != null && m_pulseTween.IsActive()) return;
            transform.localScale = m_baseScale;
            m_pulseTween = transform
                .DOScale(m_baseScale * 1.09f, 0.42f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        private void KillPulseTween(bool restoreScale)
        {
            m_pulseTween?.Kill();
            m_pulseTween = null;
            if (restoreScale && m_baseScaleCached)
            {
                transform.localScale = m_baseScale;
            }
        }

        private void HandleClick()
        {
            m_onClicked?.Invoke(m_boosterType);
        }

        public void SetEditorReferences(
            BoosterType boosterType,
            Button button,
            Image iconImage,
            GameObject lockOverlay,
            TextMeshProUGUI lockLevelText,
            GameObject badgeRoot,
            Image badgeCircleImage,
            TextMeshProUGUI badgeCountText,
            GameObject activeHighlight)
        {
            m_boosterType = boosterType;
            m_button = button;
            m_iconImage = iconImage;
            m_lockOverlay = lockOverlay;
            m_lockLevelText = lockLevelText;
            m_badgeRoot = badgeRoot;
            m_badgeCircleImage = badgeCircleImage;
            m_badgeCountText = badgeCountText;
            m_activeHighlight = activeHighlight;
        }
    }
}
