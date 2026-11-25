using System;
using DG.Tweening;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// SS.UI popup shown on battle victory.
    /// Grants Gold reward once on open and offers horizontal Watch Ad (x2) + Next buttons.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameWinPopupController : MonoBehaviour, IKeyBack
    {
        public const string ScreenResourceName = "GameWinPopup";

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI m_titleText;

        [Header("Reward Content")]
        [SerializeField] private RectTransform m_rewardIconBox;
        [SerializeField] private TextMeshProUGUI m_rewardAmountText;
        [SerializeField] private GameObject m_multiplierBadge;

        [Header("Bottom Buttons (Horizontal)")]
        [SerializeField] private Button m_watchAdX2Button;
        [SerializeField] private Button m_nextButton;

        [Header("Ad Fallback (Editor / No Fill)")]
        [SerializeField] private bool m_simulateAdWhenUnavailable = true;

        private int m_baseRewardGold;
        private bool m_hasDoubled;
        private bool m_isResolved;
        private Action m_onNext;
        private Tween m_rewardPopTween;

        public static void Open(
            int rewardGold,
            Action onNext,
            Canvas callerCanvas = null)
        {
            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<GameWinPopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: popup => popup.Setup(rewardGold, onNext),
                hasShield: true,
                shieldAlpha: 0.78f,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_watchAdX2Button != null)
                m_watchAdX2Button.onClick.AddListener(OnClickWatchAdX2);

            if (m_nextButton != null)
                m_nextButton.onClick.AddListener(OnClickNext);
        }

        private void OnDestroy()
        {
            m_rewardPopTween?.Kill();
            m_rewardPopTween = null;

            if (m_watchAdX2Button != null)
                m_watchAdX2Button.onClick.RemoveListener(OnClickWatchAdX2);

            if (m_nextButton != null)
                m_nextButton.onClick.RemoveListener(OnClickNext);
        }

        public void Setup(int rewardGold, Action onNext)
        {
            m_baseRewardGold = Mathf.Max(0, rewardGold);
            m_hasDoubled = false;
            m_isResolved = false;
            m_onNext = onNext;

            // Grant base reward immediately once (safe against double-claim).
            if (m_baseRewardGold > 0)
            {
                User.EnsureLoaded();
                User.data.AddGold(m_baseRewardGold);
                User.Save();
            }

            if (m_titleText != null)
                m_titleText.text = "CHIẾN THẮNG";

            if (m_multiplierBadge != null)
                m_multiplierBadge.SetActive(false);

            UpdateRewardDisplay(m_baseRewardGold, animate: true);

            if (m_watchAdX2Button != null)
            {
                bool canWatchAd = m_baseRewardGold > 0 &&
                    ((AdsController.instance != null && AdsController.instance.checkReward()) || m_simulateAdWhenUnavailable);
                m_watchAdX2Button.gameObject.SetActive(canWatchAd);
            }
        }

        public void OnKeyBack()
        {
            OnClickNext();
        }

        private void OnClickWatchAdX2()
        {
            if (m_isResolved || m_hasDoubled || m_baseRewardGold <= 0) return;

            PlayClickSfx();
            if (AdsController.instance != null && AdsController.instance.checkReward())
            {
                AdsController.instance.UserChoseToWatchAd(GrantDoubleReward);
                return;
            }

            if (m_simulateAdWhenUnavailable)
            {
                Debug.Log("[GameWinPopupController] Rewarded ad not loaded; granting simulated x2 reward.");
                GrantDoubleReward();
            }
        }

        private void GrantDoubleReward()
        {
            if (m_hasDoubled) return;
            m_hasDoubled = true;

            User.EnsureLoaded();
            User.data.AddGold(m_baseRewardGold);
            User.Save();

            if (m_multiplierBadge != null)
                m_multiplierBadge.SetActive(true);

            if (m_watchAdX2Button != null)
                m_watchAdX2Button.gameObject.SetActive(false);

            UpdateRewardDisplay(m_baseRewardGold * 2, animate: true);
        }

        public void OnClickNext()
        {
            if (m_isResolved) return;
            m_isResolved = true;

            PlayClickSfx();
            Action callback = m_onNext;
            m_onNext = null;

            Core.Close(this, () =>
            {
                if (callback != null)
                    callback.Invoke();
                else if (SOManager.instance != null)
                    SOManager.instance.PlayCurrentLevel();
            });
        }

        private void UpdateRewardDisplay(int displayGold, bool animate)
        {
            if (m_rewardAmountText != null)
                m_rewardAmountText.text = $"+{displayGold:N0}";

            if (animate && m_rewardIconBox != null)
            {
                m_rewardPopTween?.Kill();
                m_rewardIconBox.localScale = Vector3.one * 0.72f;
                m_rewardPopTween = m_rewardIconBox
                    .DOScale(Vector3.one, 0.28f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
        }

        private static void PlayClickSfx()
        {
            if (SOManager.instance != null)
                SOManager.instance.PlayAudio("Click2");
        }

        public void SetEditorReferences(
            TextMeshProUGUI titleText,
            RectTransform rewardIconBox,
            TextMeshProUGUI rewardAmountText,
            GameObject multiplierBadge,
            Button watchAdX2Button,
            Button nextButton)
        {
            m_titleText = titleText;
            m_rewardIconBox = rewardIconBox;
            m_rewardAmountText = rewardAmountText;
            m_multiplierBadge = multiplierBadge;
            m_watchAdX2Button = watchAdX2Button;
            m_nextButton = nextButton;
        }
    }
}
