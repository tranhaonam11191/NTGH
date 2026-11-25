using System;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// SS.UI popup shown when the player runs out of HP in battle.
    /// Warns about losing Gold on defeat and offers two horizontal revive CTAs: Gold or Rewarded Ad.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoseWarningPopupController : MonoBehaviour, IKeyBack
    {
        public const string ScreenResourceName = "LoseWarningPopup";

        [Header("Top Bar")]
        [SerializeField] private CurrencyDisplayView m_currencyDisplay;
        [SerializeField] private HeartDisplayView m_heartDisplay;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private Button m_closeButton;

        [Header("Warning Content")]
        [SerializeField] private TextMeshProUGUI m_penaltyBadgeText;
        [SerializeField] private TextMeshProUGUI m_warningMessageText;
        [SerializeField] private TextMeshProUGUI m_subMessageText;

        [Header("Revive Buttons (Horizontal)")]
        [SerializeField] private Button m_reviveByGoldButton;
        [SerializeField] private TextMeshProUGUI m_reviveByGoldCostText;
        [SerializeField] private Button m_reviveByAdButton;

        [Header("Ad Fallback (Editor / No Fill)")]
        [SerializeField] private bool m_simulateAdWhenUnavailable = true;

        private int m_goldPenalty;
        private int m_reviveGoldCost;
        private Action<bool> m_onRevive;
        private Action m_onCloseToLose;
        private Canvas m_callerCanvas;
        private bool m_isResolved;

        public static void Open(
            int goldPenalty,
            int reviveGoldCost,
            Action<bool> onRevive,
            Action onCloseToLose,
            Canvas callerCanvas = null)
        {
            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<LoseWarningPopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: popup => popup.Setup(goldPenalty, reviveGoldCost, onRevive, onCloseToLose, callerCanvas),
                hasShield: true,
                shieldAlpha: 0.75f,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_closeButton != null)
                m_closeButton.onClick.AddListener(OnClickClose);

            if (m_reviveByGoldButton != null)
                m_reviveByGoldButton.onClick.AddListener(OnClickReviveByGold);

            if (m_reviveByAdButton != null)
                m_reviveByAdButton.onClick.AddListener(OnClickReviveByAd);
        }

        private void OnDestroy()
        {
            if (m_closeButton != null)
                m_closeButton.onClick.RemoveListener(OnClickClose);

            if (m_reviveByGoldButton != null)
                m_reviveByGoldButton.onClick.RemoveListener(OnClickReviveByGold);

            if (m_reviveByAdButton != null)
                m_reviveByAdButton.onClick.RemoveListener(OnClickReviveByAd);
        }

        public void Setup(
            int goldPenalty,
            int reviveGoldCost,
            Action<bool> onRevive,
            Action onCloseToLose,
            Canvas callerCanvas = null)
        {
            m_goldPenalty = Mathf.Max(0, goldPenalty);
            m_reviveGoldCost = Mathf.Max(0, reviveGoldCost);
            m_onRevive = onRevive;
            m_onCloseToLose = onCloseToLose;
            m_callerCanvas = callerCanvas;
            m_isResolved = false;

            if (m_currencyDisplay != null)
                m_currencyDisplay.RefreshFromUserData();

            if (m_heartDisplay != null)
                m_heartDisplay.RefreshFromUserData();

            if (m_titleText != null)
                m_titleText.text = "WARNING";

            if (m_penaltyBadgeText != null)
            {
                bool showBadge = m_goldPenalty > 0;
                m_penaltyBadgeText.transform.parent.gameObject.SetActive(showBadge);
                m_penaltyBadgeText.text = $"-{m_goldPenalty:N0}";
            }

            if (m_warningMessageText != null)
            {
                m_warningMessageText.text = m_goldPenalty > 0
                    ? $"Thua trận sẽ bị mất <color=#FF5C77>{m_goldPenalty:N0}</color> tiền!"
                    : "Thua trận sẽ bị mất tiền!";
            }

            if (m_subMessageText != null)
                m_subMessageText.text = "Hồi phục máu để tiếp tục chiến đấu ngay.";

            if (m_reviveByGoldCostText != null)
                m_reviveByGoldCostText.text = m_reviveGoldCost.ToString("N0");

            if (m_reviveByAdButton != null)
            {
                bool adAvailable = (AdsController.instance != null && AdsController.instance.checkReward())
                    || m_simulateAdWhenUnavailable;
                m_reviveByAdButton.gameObject.SetActive(adAvailable);
            }
        }

        public void OnKeyBack()
        {
            OnClickClose();
        }

        public void OnClickClose()
        {
            if (m_isResolved) return;
            m_isResolved = true;

            PlayClickSfx();
            Action callback = m_onCloseToLose;
            m_onCloseToLose = null;
            m_onRevive = null;

            Core.Close(this, () => callback?.Invoke());
        }

        private void OnClickReviveByGold()
        {
            if (m_isResolved) return;

            PlayClickSfx();
            User.EnsureLoaded();

            if (User.data.TrySpendGold(m_reviveGoldCost))
            {
                User.Save();
                CompleteRevive(viaAd: false);
                return;
            }

            NotEnoughGoldPopupController.Open(
                requiredGold: m_reviveGoldCost,
                adRewardGold: Mathf.Max(200, m_reviveGoldCost),
                onRewardClaimed: () =>
                {
                    if (m_currencyDisplay != null)
                        m_currencyDisplay.RefreshFromUserData();
                },
                callerCanvas: m_callerCanvas);
        }

        private void OnClickReviveByAd()
        {
            if (m_isResolved) return;

            PlayClickSfx();
            if (AdsController.instance != null && AdsController.instance.checkReward())
            {
                AdsController.instance.UserChoseToWatchAd(() => CompleteRevive(viaAd: true));
                return;
            }

            if (m_simulateAdWhenUnavailable)
            {
                Debug.Log("[LoseWarningPopupController] Rewarded ad not loaded; granting simulated revive.");
                CompleteRevive(viaAd: true);
            }
        }

        private void CompleteRevive(bool viaAd)
        {
            if (m_isResolved) return;
            m_isResolved = true;

            Action<bool> callback = m_onRevive;
            m_onRevive = null;
            m_onCloseToLose = null;

            Core.Close(this, () => callback?.Invoke(viaAd));
        }

        private static void PlayClickSfx()
        {
            if (SOManager.instance != null)
                SOManager.instance.PlayAudio("Click2");
        }

        public void SetHeartDisplayReference(HeartDisplayView heartDisplay)
        {
            m_heartDisplay = heartDisplay;
        }

        public void SetEditorReferences(
            CurrencyDisplayView currencyDisplay,
            TextMeshProUGUI titleText,
            Button closeButton,
            TextMeshProUGUI penaltyBadgeText,
            TextMeshProUGUI warningMessageText,
            TextMeshProUGUI subMessageText,
            Button reviveByGoldButton,
            TextMeshProUGUI reviveByGoldCostText,
            Button reviveByAdButton)
        {
            m_currencyDisplay = currencyDisplay;
            m_titleText = titleText;
            m_closeButton = closeButton;
            m_penaltyBadgeText = penaltyBadgeText;
            m_warningMessageText = warningMessageText;
            m_subMessageText = subMessageText;
            m_reviveByGoldButton = reviveByGoldButton;
            m_reviveByGoldCostText = reviveByGoldCostText;
            m_reviveByAdButton = reviveByAdButton;
        }
    }
}
