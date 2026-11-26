using System;
using System.Collections;
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

    /// <summary>
    /// Abstract base class for all Match-3 boosters.
    /// Each concrete booster prefab inherits from this class and implements its own gameplay logic.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class BoosterBase : MonoBehaviour
    {
        private static readonly Color GreenBadgeColor = new Color32(34, 197, 94, 255);
        private static readonly Color RedBadgeColor = new Color32(239, 68, 68, 255);
        private static readonly Color LockedIconTint = new Color(0.32f, 0.35f, 0.42f, 0.72f);
        private static readonly Color BusyIconTint = new Color(0.72f, 0.76f, 0.84f, 0.88f);

        [Header("Booster UI References")]
        [SerializeField] protected Button m_button;
        [SerializeField] protected Image m_iconImage;
        [SerializeField] protected GameObject m_lockOverlay;
        [SerializeField] protected TextMeshProUGUI m_lockLevelText;
        [SerializeField] protected GameObject m_badgeRoot;
        [SerializeField] protected Image m_badgeCircleImage;
        [SerializeField] protected TextMeshProUGUI m_badgeCountText;
        [SerializeField] protected GameObject m_activeHighlight;

        private Action<BoosterBase> m_onClicked;
        private Tween m_pulseTween;
        private Sequence m_unlockSequence;
        private Vector3 m_baseScale = Vector3.one;
        private bool m_baseScaleCached;

        public abstract BoosterType BoosterType { get; }
        public virtual bool RequiresTarget => true;
        public BoosterSlotVisualState CurrentState { get; private set; }
        public RectTransform IconRectTransform => m_iconImage != null ? m_iconImage.rectTransform : transform as RectTransform;

        protected virtual void Awake()
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

        protected virtual void OnDisable()
        {
            KillPulseTween(restoreScale: true);
            m_unlockSequence?.Kill();
            m_unlockSequence = null;
        }

        protected virtual void OnDestroy()
        {
            KillPulseTween(restoreScale: false);
            m_unlockSequence?.Kill();
            m_unlockSequence = null;
            if (m_button != null)
            {
                m_button.onClick.RemoveListener(HandleClick);
            }
        }

        /// <summary>
        /// Plays the unlock reveal animation when a newly claimed booster flies into this slot.
        /// </summary>
        public void PlayUnlockAnimation(BoosterConfig config, int count, Action onComplete = null)
        {
            CacheBaseScale();
            KillPulseTween(restoreScale: true);
            m_unlockSequence?.Kill();
            m_unlockSequence = null;

            RefreshState(config, isUnlocked: true, count: count, isTargeting: false, isBoardBusy: false);

            // Briefly keep lock overlay visible to animate it bursting away
            CanvasGroup lockCg = null;
            if (m_lockOverlay != null)
            {
                m_lockOverlay.SetActive(true);
                m_lockOverlay.transform.localScale = Vector3.one;
                lockCg = m_lockOverlay.GetComponent<CanvasGroup>();
                if (lockCg == null) lockCg = m_lockOverlay.AddComponent<CanvasGroup>();
                lockCg.alpha = 1f;
            }

            if (m_badgeRoot != null)
            {
                m_badgeRoot.transform.localScale = Vector3.zero;
            }

            Image hlImg = null;
            if (m_activeHighlight != null)
            {
                m_activeHighlight.SetActive(true);
                hlImg = m_activeHighlight.GetComponent<Image>();
                if (hlImg != null)
                {
                    Color c = hlImg.color;
                    c.a = 1f;
                    hlImg.color = c;
                }
            }

            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlaySfx("ClearSFX");
            }

            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            // Slot juicy punch
            transform.localScale = m_baseScale * 0.88f;
            seq.Append(transform.DOScale(m_baseScale * 1.22f, 0.16f).SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(m_baseScale, 0.24f).SetEase(Ease.OutBack));

            // Lock overlay burst out
            if (m_lockOverlay != null && lockCg != null)
            {
                seq.Join(m_lockOverlay.transform.DOScale(1.35f, 0.22f).SetEase(Ease.OutQuad));
                seq.Join(lockCg.DOFade(0f, 0.22f).SetEase(Ease.InQuad));
            }

            // Badge pop in
            if (m_badgeRoot != null)
            {
                seq.Insert(0.14f, m_badgeRoot.transform.DOScale(Vector3.one, 0.28f).SetEase(Ease.OutBack));
            }

            // Highlight ring flash & fade
            if (hlImg != null)
            {
                seq.Insert(0.18f, hlImg.DOFade(0f, 0.28f).SetEase(Ease.OutQuad));
            }

            seq.OnComplete(() =>
            {
                if (m_lockOverlay != null)
                {
                    m_lockOverlay.SetActive(false);
                    m_lockOverlay.transform.localScale = Vector3.one;
                    if (lockCg != null) lockCg.alpha = 1f;
                }

                if (m_activeHighlight != null)
                {
                    m_activeHighlight.SetActive(false);
                    if (hlImg != null)
                    {
                        Color c = hlImg.color;
                        c.a = 1f;
                        hlImg.color = c;
                    }
                }

                if (m_badgeRoot != null)
                {
                    m_badgeRoot.transform.localScale = Vector3.one;
                }

                transform.localScale = m_baseScale;
                m_unlockSequence = null;
                onComplete?.Invoke();
            });

            m_unlockSequence = seq;
        }

        private void CacheBaseScale()
        {
            if (m_baseScaleCached) return;
            m_baseScale = transform.localScale;
            m_baseScaleCached = true;
        }

        public void Bind(Action<BoosterBase> onClicked)
        {
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

        /// <summary>
        /// Called when this booster enters targeting mode (for RequiresTarget == true boosters).
        /// </summary>
        public virtual void OnTargetingStarted(ShapesManager board) { }

        /// <summary>
        /// Called when targeting mode is cancelled or interrupted before execution.
        /// </summary>
        public virtual void OnTargetingCancelled(ShapesManager board) { }

        /// <summary>
        /// Polled every frame while this booster is actively targeting and the board is idle & unblocked.
        /// Subclasses must call tryConsumeCharge() right before committing execution.
        /// </summary>
        public virtual void OnTargetingUpdate(ShapesManager board, Func<bool> tryConsumeCharge) { }

        /// <summary>
        /// Executed immediately for boosters with RequiresTarget == false (e.g. Shuffle).
        /// Board busy lock is already acquired before this coroutine starts.
        /// </summary>
        public virtual IEnumerator ExecuteImmediate(ShapesManager board)
        {
            yield break;
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
            m_onClicked?.Invoke(this);
        }

        public void SetEditorReferences(
            Button button,
            Image iconImage,
            GameObject lockOverlay,
            TextMeshProUGUI lockLevelText,
            GameObject badgeRoot,
            Image badgeCircleImage,
            TextMeshProUGUI badgeCountText,
            GameObject activeHighlight)
        {
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
