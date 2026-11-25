using System;
using DG.Tweening;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// Neon 90s notification popup (Prefab Variant of NeonPopup_Base) shown when the player
    /// does not have enough gold to perform an upgrade. Offers a Rewarded Ad CTA to earn gold.
    /// </summary>
    [DisallowMultipleComponent]
    public class NotEnoughGoldPopupController : MonoBehaviour
    {
        public const string ScreenResourceName = "NotEnoughGoldPopup";

        [Header("Base Popup (Inherited from NeonPopup_Base)")]
        [SerializeField] private NeonPopupBase m_popupBase;

        [Header("Notification Content")]
        [SerializeField] private TextMeshProUGUI m_messageText;
        [SerializeField] private Button m_watchAdButton;
        [SerializeField] private TextMeshProUGUI m_watchAdLabel;
        [SerializeField] private Button m_cancelButton;

        [Header("Ad Fallback (Editor / No Fill)")]
        [SerializeField] private bool m_simulateAdWhenUnavailable = true;

        private int m_requiredGold;
        private int m_adRewardGold = 500;
        private Action m_onRewardClaimed;
        private Tween m_autoCloseTween;

        public static void Open(
            int requiredGold = 0,
            int adRewardGold = 500,
            Action onRewardClaimed = null,
            Canvas callerCanvas = null)
        {
            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<NotEnoughGoldPopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: popup => popup.Setup(requiredGold, adRewardGold, onRewardClaimed),
                hasShield: true,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_popupBase == null)
                m_popupBase = GetComponent<NeonPopupBase>();

            if (m_watchAdButton != null)
                m_watchAdButton.onClick.AddListener(OnClickWatchAd);

            if (m_cancelButton != null)
                m_cancelButton.onClick.AddListener(OnClickCancel);
        }

        private void OnEnable()
        {
            User.OnGoldChanged += HandleGoldChanged;
        }

        private void OnDisable()
        {
            User.OnGoldChanged -= HandleGoldChanged;
            m_autoCloseTween?.Kill();
            m_autoCloseTween = null;
        }

        private void OnDestroy()
        {
            m_autoCloseTween?.Kill();
            m_autoCloseTween = null;

            if (m_watchAdButton != null)
                m_watchAdButton.onClick.RemoveListener(OnClickWatchAd);

            if (m_cancelButton != null)
                m_cancelButton.onClick.RemoveListener(OnClickCancel);
        }

        public void Setup(int requiredGold, int adRewardGold, Action onRewardClaimed = null)
        {
            if (m_popupBase == null)
                m_popupBase = GetComponent<NeonPopupBase>();

            m_requiredGold = Mathf.Max(0, requiredGold);
            m_adRewardGold = Mathf.Max(1, adRewardGold);
            m_onRewardClaimed = onRewardClaimed;

            m_autoCloseTween?.Kill();
            m_autoCloseTween = null;

            if (m_watchAdButton != null)
                m_watchAdButton.interactable = true;

            if (m_popupBase != null)
            {
                m_popupBase.SetTitle("NHẬN TIỀN MIỄN PHÍ");
                if (m_popupBase.CurrencyDisplay != null)
                {
                    m_popupBase.CurrencyDisplay.RefreshFromUserData();
                }
            }

            RefreshTexts();
        }

        private void HandleGoldChanged(int _)
        {
            if (gameObject.activeInHierarchy)
            {
                RefreshTexts();
            }
        }

        private void RefreshTexts()
        {
            if (m_messageText != null)
            {
                m_messageText.text =
                    $"Xem quảng cáo để nhận ngay\n" +
                    $"<color=#FFD15C>+{m_adRewardGold:N0}</color> tiền miễn phí!";
            }

            if (m_watchAdLabel != null)
            {
                m_watchAdLabel.text = $"XEM QC  +{m_adRewardGold:N0}";
                RectTransform btnRt = m_watchAdLabel.transform.parent as RectTransform;
                if (btnRt != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(btnRt);
                }
            }
        }

        private void OnClickWatchAd()
        {
            WatchRewardedAd(GrantAdReward);
        }

        private void GrantAdReward()
        {
            User.EnsureLoaded();
            User.data.AddGold(m_adRewardGold);
            User.Save();

            if (HomeController.instance != null)
            {
                HomeController.instance.SetData();
            }

            m_onRewardClaimed?.Invoke();

            if (m_popupBase != null)
            {
                m_popupBase.ShowToast($"+{m_adRewardGold:N0} tiền!");
            }

            if (m_watchAdButton != null)
                m_watchAdButton.interactable = false;

            m_autoCloseTween?.Kill();
            m_autoCloseTween = DOVirtual.DelayedCall(0.55f, Close).SetUpdate(true);
        }

        private void OnClickCancel()
        {
            Close();
        }

        public void Close()
        {
            m_autoCloseTween?.Kill();
            m_autoCloseTween = null;

            if (m_popupBase != null)
                m_popupBase.OnClickClose();
            else
                Core.Close(this);
        }

        private void WatchRewardedAd(Action onEarnedReward)
        {
            if (AdsController.instance != null && AdsController.instance.checkReward())
            {
                AdsController.instance.UserChoseToWatchAd(() => onEarnedReward?.Invoke());
                return;
            }

            if (m_simulateAdWhenUnavailable)
            {
                Debug.Log("[NotEnoughGoldPopupController] Rewarded ad not loaded; granting simulated ad reward.");
                onEarnedReward?.Invoke();
                return;
            }

            if (m_popupBase != null)
            {
                m_popupBase.ShowToast("Quảng cáo chưa sẵn sàng, thử lại sau!");
            }
        }

        public void ConfigureReferences(
            NeonPopupBase popupBase,
            TextMeshProUGUI messageText,
            Button watchAdButton,
            TextMeshProUGUI watchAdLabel,
            Button cancelButton)
        {
            m_popupBase = popupBase;
            m_messageText = messageText;
            m_watchAdButton = watchAdButton;
            m_watchAdLabel = watchAdLabel;
            m_cancelButton = cancelButton;
        }
    }
}
