using System;
using DG.Tweening;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// SS.UI Neon 90s popup for refilling hearts:
    /// - Buy with coin/gold -> refills to FULL hearts (5/5).
    /// - Watch rewarded ad -> grants +1 heart.
    /// Shown automatically when attempting to enter battle with 0 hearts,
    /// or manually when tapping the HeartDisplay HUD on Home.
    /// </summary>
    [DisallowMultipleComponent]
    public class BuyHeartPopupController : MonoBehaviour, IKeyBack
    {
        public const string ScreenResourceName = "BuyHeartPopup";
        public const int DefaultFullRefillCoinCost = 500;

        [Header("Top Bar")]
        [SerializeField] private CurrencyDisplayView m_currencyDisplay;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private Button m_closeButton;

        [Header("Heart Status Content")]
        [SerializeField] private TextMeshProUGUI m_heartCountBadgeText;
        [SerializeField] private TextMeshProUGUI m_timerText;
        [SerializeField] private TextMeshProUGUI m_messageText;

        [Header("Action Buttons")]
        [SerializeField] private Button m_buyFullByCoinButton;
        [SerializeField] private TextMeshProUGUI m_buyFullCoinCostText;
        [SerializeField] private Button m_watchAdButton;
        [SerializeField] private TextMeshProUGUI m_watchAdSubLabel;

        [Header("Toast")]
        [SerializeField] private CanvasGroup m_toastCanvasGroup;
        [SerializeField] private TextMeshProUGUI m_toastText;

        [Header("Config")]
        [SerializeField] private int m_defaultFullRefillCoinCost = DefaultFullRefillCoinCost;
        [SerializeField] private bool m_simulateAdWhenUnavailable = true;

        private int m_fullRefillCoinCost;
        private Action m_onHeartsUpdated;
        private Canvas m_callerCanvas;
        private bool m_isClosing;
        private float m_nextTickTime;
        private Sequence m_toastSequence;
        private Tween m_autoCloseTween;

        public static void Open(
            int fullRefillCoinCost = DefaultFullRefillCoinCost,
            Action onHeartsUpdated = null,
            Canvas callerCanvas = null)
        {
            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<BuyHeartPopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: popup => popup.Setup(fullRefillCoinCost, onHeartsUpdated, callerCanvas),
                hasShield: true,
                shieldAlpha: 0.78f,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_closeButton != null)
                m_closeButton.onClick.AddListener(OnClickClose);

            if (m_buyFullByCoinButton != null)
                m_buyFullByCoinButton.onClick.AddListener(OnClickBuyFullByCoin);

            if (m_watchAdButton != null)
                m_watchAdButton.onClick.AddListener(OnClickWatchAd);

            if (m_toastCanvasGroup != null)
            {
                m_toastCanvasGroup.alpha = 0f;
                m_toastCanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnEnable()
        {
            m_isClosing = false;
            User.OnGoldChanged += HandleGoldChanged;
            User.OnHeartsChanged += HandleHeartsChanged;
            m_nextTickTime = Time.unscaledTime + 1f;
        }

        private void OnDisable()
        {
            User.OnGoldChanged -= HandleGoldChanged;
            User.OnHeartsChanged -= HandleHeartsChanged;
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();

            if (m_closeButton != null)
                m_closeButton.onClick.RemoveListener(OnClickClose);

            if (m_buyFullByCoinButton != null)
                m_buyFullByCoinButton.onClick.RemoveListener(OnClickBuyFullByCoin);

            if (m_watchAdButton != null)
                m_watchAdButton.onClick.RemoveListener(OnClickWatchAd);
        }

        private void Update()
        {
            if (Time.unscaledTime < m_nextTickTime)
            {
                return;
            }

            m_nextTickTime = Time.unscaledTime + 1f;

            User.EnsureLoaded();
            if (User.data != null && User.data.RefreshHearts())
            {
                User.Save();
            }

            RefreshUI();
        }

        public void Setup(
            int fullRefillCoinCost = DefaultFullRefillCoinCost,
            Action onHeartsUpdated = null,
            Canvas callerCanvas = null)
        {
            KillTweens();
            m_isClosing = false;
            m_fullRefillCoinCost = fullRefillCoinCost > 0
                ? fullRefillCoinCost
                : Mathf.Max(1, m_defaultFullRefillCoinCost);
            m_onHeartsUpdated = onHeartsUpdated;
            m_callerCanvas = callerCanvas;

            if (m_toastCanvasGroup != null)
            {
                m_toastCanvasGroup.alpha = 0f;
            }

            User.RefreshHeartsAndSave();
            RefreshUI();
        }

        private void HandleGoldChanged(int _)
        {
            if (m_currencyDisplay != null)
                m_currencyDisplay.RefreshFromUserData();
        }

        private void HandleHeartsChanged(int _)
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            User.EnsureLoaded();
            if (User.data == null)
            {
                return;
            }

            if (m_currencyDisplay != null)
                m_currencyDisplay.RefreshFromUserData();

            int currentHearts = User.data.hearts;
            int maxHearts = UserData.MAX_HEARTS;
            bool isFull = currentHearts >= maxHearts;

            if (m_titleText != null)
            {
                m_titleText.text = currentHearts <= 0 ? "HẾT TIM" : "MUA TIM";
            }

            if (m_heartCountBadgeText != null)
            {
                m_heartCountBadgeText.text = $"{currentHearts}/{maxHearts}";
            }

            if (m_timerText != null)
            {
                if (isFull)
                {
                    m_timerText.text = "Tim đã đầy (5/5)";
                }
                else
                {
                    int remainingSeconds = User.data.GetSecondsUntilNextHeart();
                    m_timerText.text = $"Hồi +1 tim sau: <color=#29EBE0>{UserData.FormatHeartTimer(remainingSeconds)}</color>";
                }
            }

            if (m_messageText != null)
            {
                if (currentHearts <= 0)
                {
                    m_messageText.text =
                        $"Bạn đã hết tim để vào trận!\n" +
                        $"Mua <color=#FFD15C>FULL {maxHearts} TIM</color> bằng tiền hoặc xem QC nhận <color=#29EBE0>+1 tim</color>.";
                }
                else if (isFull)
                {
                    m_messageText.text = "Tim của bạn đã đầy. Sẵn sàng chiến đấu!";
                }
                else
                {
                    m_messageText.text =
                        $"Mua <color=#FFD15C>FULL {maxHearts} TIM</color> bằng tiền hoặc xem QC nhận <color=#29EBE0>+1 tim</color>.";
                }
            }

            if (m_buyFullCoinCostText != null)
            {
                m_buyFullCoinCostText.text = m_fullRefillCoinCost.ToString("N0");
            }

            if (m_watchAdSubLabel != null)
            {
                m_watchAdSubLabel.text = "+1 Tim";
            }

            if (m_buyFullByCoinButton != null)
            {
                m_buyFullByCoinButton.interactable = !isFull;
            }

            if (m_watchAdButton != null)
            {
                bool adAvailable = (AdsController.instance != null && AdsController.instance.checkReward())
                    || m_simulateAdWhenUnavailable;
                m_watchAdButton.interactable = !isFull && adAvailable;
            }
        }

        private void OnClickBuyFullByCoin()
        {
            if (m_isClosing) return;

            PlayClickSfx();
            User.RefreshHeartsAndSave();

            if (User.data == null) return;

            if (User.data.IsHeartsFull())
            {
                ShowToast("Tim đã đầy!");
                RefreshUI();
                return;
            }

            if (User.data.TrySpendGold(m_fullRefillCoinCost))
            {
                User.data.RefillFullHearts();
                User.Save();

                if (HomeController.instance != null)
                {
                    HomeController.instance.SetData();
                }

                m_onHeartsUpdated?.Invoke();
                RefreshUI();
                ShowToast($"Đã hồi đầy {UserData.MAX_HEARTS} tim!");

                m_autoCloseTween?.Kill();
                m_autoCloseTween = DOVirtual.DelayedCall(0.6f, Close).SetUpdate(true);
                return;
            }

            NotEnoughGoldPopupController.Open(
                requiredGold: m_fullRefillCoinCost,
                adRewardGold: Mathf.Max(500, m_fullRefillCoinCost),
                onRewardClaimed: RefreshUI,
                callerCanvas: m_callerCanvas);
        }

        private void OnClickWatchAd()
        {
            if (m_isClosing) return;

            PlayClickSfx();
            User.RefreshHeartsAndSave();

            if (User.data == null) return;

            if (User.data.IsHeartsFull())
            {
                ShowToast("Tim đã đầy!");
                RefreshUI();
                return;
            }

            if (AdsController.instance != null && AdsController.instance.checkReward())
            {
                AdsController.instance.UserChoseToWatchAd(GrantAdHeartReward);
                return;
            }

            if (m_simulateAdWhenUnavailable)
            {
                Debug.Log("[BuyHeartPopupController] Rewarded ad not loaded; granting simulated +1 heart.");
                GrantAdHeartReward();
                return;
            }

            ShowToast("Quảng cáo chưa sẵn sàng, thử lại sau!");
        }

        private void GrantAdHeartReward()
        {
            User.EnsureLoaded();
            if (User.data == null) return;

            User.data.AddHearts(1);
            User.Save();

            if (HomeController.instance != null)
            {
                HomeController.instance.SetData();
            }

            m_onHeartsUpdated?.Invoke();
            RefreshUI();
            ShowToast("+1 Tim!");

            if (User.data.IsHeartsFull())
            {
                m_autoCloseTween?.Kill();
                m_autoCloseTween = DOVirtual.DelayedCall(0.6f, Close).SetUpdate(true);
            }
        }

        public void OnKeyBack()
        {
            OnClickClose();
        }

        public void OnClickClose()
        {
            PlayClickSfx();
            Close();
        }

        public void Close()
        {
            if (m_isClosing) return;
            m_isClosing = true;

            KillTweens();
            Core.Close(this);
        }

        private void ShowToast(string message)
        {
            if (m_toastCanvasGroup == null || m_toastText == null)
            {
                return;
            }

            m_toastText.text = message;
            m_toastSequence?.Kill();
            m_toastCanvasGroup.alpha = 0f;
            m_toastSequence = DOTween.Sequence()
                .SetUpdate(true)
                .Append(m_toastCanvasGroup.DOFade(1f, 0.16f))
                .AppendInterval(1.2f)
                .Append(m_toastCanvasGroup.DOFade(0f, 0.28f));
        }

        private void KillTweens()
        {
            m_toastSequence?.Kill();
            m_toastSequence = null;
            m_autoCloseTween?.Kill();
            m_autoCloseTween = null;
        }

        private static void PlayClickSfx()
        {
            if (SOManager.instance != null)
                SOManager.instance.PlayAudio("Click2");
        }

        public void SetEditorReferences(
            CurrencyDisplayView currencyDisplay,
            TextMeshProUGUI titleText,
            Button closeButton,
            TextMeshProUGUI heartCountBadgeText,
            TextMeshProUGUI timerText,
            TextMeshProUGUI messageText,
            Button buyFullByCoinButton,
            TextMeshProUGUI buyFullCoinCostText,
            Button watchAdButton,
            TextMeshProUGUI watchAdSubLabel,
            CanvasGroup toastCanvasGroup,
            TextMeshProUGUI toastText)
        {
            m_currencyDisplay = currencyDisplay;
            m_titleText = titleText;
            m_closeButton = closeButton;
            m_heartCountBadgeText = heartCountBadgeText;
            m_timerText = timerText;
            m_messageText = messageText;
            m_buyFullByCoinButton = buyFullByCoinButton;
            m_buyFullCoinCostText = buyFullCoinCostText;
            m_watchAdButton = watchAdButton;
            m_watchAdSubLabel = watchAdSubLabel;
            m_toastCanvasGroup = toastCanvasGroup;
            m_toastText = toastText;
        }
    }
}
