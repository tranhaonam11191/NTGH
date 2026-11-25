using System;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// SS.UI popup shown after the player declines revive in LoseWarningPopup.
    /// Displays HP depleted, Gold lost, a close button (Home), and a single Retry CTA.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameLosePopupController : MonoBehaviour, IKeyBack
    {
        public const string ScreenResourceName = "GameLosePopup";

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private Button m_closeButton;

        [Header("Defeat Content")]
        [SerializeField] private TextMeshProUGUI m_statusText;
        [SerializeField] private GameObject m_goldLostPill;
        [SerializeField] private TextMeshProUGUI m_goldLostText;

        [Header("Actions")]
        [SerializeField] private Button m_retryButton;

        private Action m_onRetry;
        private Action m_onHome;
        private bool m_isResolved;
        private bool m_elevatedGameHeart;

        public static void Open(
            int goldLost,
            Action onRetry,
            Action onHome,
            Canvas callerCanvas = null)
        {
            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<GameLosePopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: popup => popup.Setup(goldLost, onRetry, onHome),
                hasShield: true,
                shieldAlpha: 0.78f,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_closeButton != null)
                m_closeButton.onClick.AddListener(OnClickHome);

            if (m_retryButton != null)
                m_retryButton.onClick.AddListener(OnClickRetry);
        }

        private void OnDestroy()
        {
            ReleaseGameHeartDisplay();

            if (m_closeButton != null)
                m_closeButton.onClick.RemoveListener(OnClickHome);

            if (m_retryButton != null)
                m_retryButton.onClick.RemoveListener(OnClickRetry);
        }

        public void Setup(int goldLost, Action onRetry, Action onHome)
        {
            m_onRetry = onRetry;
            m_onHome = onHome;
            m_isResolved = false;

            ElevateGameHeartDisplay();

            if (m_titleText != null)
                m_titleText.text = "THẤT BẠI";

            if (m_statusText != null)
                m_statusText.text = "Bạn đã hết máu!";

            int clampedLost = Mathf.Max(0, goldLost);
            if (m_goldLostPill != null)
                m_goldLostPill.SetActive(clampedLost > 0);

            if (m_goldLostText != null && clampedLost > 0)
                m_goldLostText.text = $"-{clampedLost:N0} Tiền";
        }

        public void OnKeyBack()
        {
            OnClickHome();
        }

        public void OnClickHome()
        {
            if (m_isResolved) return;
            m_isResolved = true;

            PlayClickSfx();
            Action callback = m_onHome;
            m_onHome = null;
            m_onRetry = null;

            Core.Close(this, () =>
            {
                if (callback != null)
                    callback.Invoke();
                else
                    SOManager.LoadHome();
            });
        }

        public void OnClickRetry()
        {
            if (m_isResolved) return;

            PlayClickSfx();
            User.RefreshHeartsAndSave();
            if (User.data != null && !User.data.HasHeart())
            {
                BuyHeartPopupController.Open(callerCanvas: GetComponentInParent<Canvas>());
                return;
            }

            m_isResolved = true;
            Action callback = m_onRetry;
            m_onRetry = null;
            m_onHome = null;

            Core.Close(this, () =>
            {
                if (callback != null)
                    callback.Invoke();
                else if (SOManager.instance != null)
                    SOManager.instance.PlayCurrentLevel();
            });
        }

        private static void PlayClickSfx()
        {
            if (SOManager.instance != null)
                SOManager.instance.PlayAudio("Click2");
        }

        private void ElevateGameHeartDisplay()
        {
            if (m_elevatedGameHeart)
            {
                GameController.instance?.GetGameCurrencyDisplay()?.RefreshFromUserData();
                GameController.instance?.GetGameHeartDisplay()?.RefreshFromUserData();
                return;
            }

            if (GameController.instance != null)
            {
                GameController.instance.PushBattleHudOverShield();
                m_elevatedGameHeart = true;
            }
        }

        private void ReleaseGameHeartDisplay()
        {
            if (!m_elevatedGameHeart)
                return;

            m_elevatedGameHeart = false;
            GameController.instance?.PopBattleHudOverShield();
        }

        public void SetEditorReferences(
            CurrencyDisplayView currencyDisplay,
            TextMeshProUGUI titleText,
            Button closeButton,
            TextMeshProUGUI statusText,
            GameObject goldLostPill,
            TextMeshProUGUI goldLostText,
            Button retryButton)
        {
            m_titleText = titleText;
            m_closeButton = closeButton;
            m_statusText = statusText;
            m_goldLostPill = goldLostPill;
            m_goldLostText = goldLostText;
            m_retryButton = retryButton;
        }
    }
}
