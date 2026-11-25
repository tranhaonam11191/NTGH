using System;
using DG.Tweening;
using NTGH.Boosters;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// SS.UI Neon 90s popup for purchasing a Match-3 booster in Pack 1 or Pack 3.
    /// Opens NotEnoughGoldPopup when the player does not have enough gold.
    /// Pauses battle board/enemy timer while open.
    /// </summary>
    [DisallowMultipleComponent]
    public class BuyBoosterPopupController : MonoBehaviour, IKeyBack
    {
        public const string ScreenResourceName = "BuyBoosterPopup";

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private Button m_closeButton;

        [Header("Booster Info")]
        [SerializeField] private Image m_boosterIconImage;
        [SerializeField] private TextMeshProUGUI m_boosterNameText;
        [SerializeField] private TextMeshProUGUI m_boosterDescText;
        [SerializeField] private TextMeshProUGUI m_ownedCountText;

        [Header("Purchase Pack Buttons")]
        [SerializeField] private Button m_buyPack1Button;
        [SerializeField] private TextMeshProUGUI m_pack1PriceText;
        [SerializeField] private Button m_buyPack3Button;
        [SerializeField] private TextMeshProUGUI m_pack3PriceText;
        [SerializeField] private TextMeshProUGUI m_pack3SaveTagText;

        [Header("Toast")]
        [SerializeField] private CanvasGroup m_toastCanvasGroup;
        [SerializeField] private TextMeshProUGUI m_toastText;

        private BoosterConfig m_config;
        private Action m_onPurchased;
        private Canvas m_callerCanvas;
        private bool m_isClosing;
        private bool m_hasBattleHudElevation;
        private bool m_hasBattleBoardBlock;
        private Sequence m_toastSequence;
        private Tween m_autoCloseTween;

        public static void Open(
            BoosterConfig config,
            Action onPurchased = null,
            Canvas callerCanvas = null)
        {
            if (config == null) return;

            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<BuyBoosterPopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: popup => popup.Setup(config, onPurchased, callerCanvas),
                hasShield: true,
                shieldAlpha: 0.78f,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_closeButton != null)
                m_closeButton.onClick.AddListener(OnClickClose);

            if (m_buyPack1Button != null)
                m_buyPack1Button.onClick.AddListener(OnClickBuyPack1);

            if (m_buyPack3Button != null)
                m_buyPack3Button.onClick.AddListener(OnClickBuyPack3);

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
            User.OnBoosterCountChanged += HandleBoosterCountChanged;
        }

        private void OnDisable()
        {
            User.OnGoldChanged -= HandleGoldChanged;
            User.OnBoosterCountChanged -= HandleBoosterCountChanged;
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
            ReleaseBattleHooks();

            if (m_closeButton != null)
                m_closeButton.onClick.RemoveListener(OnClickClose);

            if (m_buyPack1Button != null)
                m_buyPack1Button.onClick.RemoveListener(OnClickBuyPack1);

            if (m_buyPack3Button != null)
                m_buyPack3Button.onClick.RemoveListener(OnClickBuyPack3);
        }

        public void Setup(BoosterConfig config, Action onPurchased = null, Canvas callerCanvas = null)
        {
            KillTweens();
            m_isClosing = false;
            m_config = config;
            m_onPurchased = onPurchased;
            m_callerCanvas = callerCanvas;

            if (m_toastCanvasGroup != null)
            {
                m_toastCanvasGroup.alpha = 0f;
            }

            AcquireBattleHooks();
            RefreshUI();
        }

        private void AcquireBattleHooks()
        {
            if (GameController.instance == null) return;

            if (!m_hasBattleHudElevation)
            {
                GameController.instance.PushBattleHudOverShield();
                m_hasBattleHudElevation = true;
            }
            else
            {
                GameController.instance.GetGameCurrencyDisplay()?.RefreshFromUserData();
            }

            if (!m_hasBattleBoardBlock)
            {
                GameController.instance.PushBoosterPopupBlock();
                m_hasBattleBoardBlock = true;
            }
        }

        private void ReleaseBattleHooks()
        {
            if (m_hasBattleHudElevation)
            {
                m_hasBattleHudElevation = false;
                GameController.instance?.PopBattleHudOverShield();
            }

            if (m_hasBattleBoardBlock)
            {
                m_hasBattleBoardBlock = false;
                GameController.instance?.PopBoosterPopupBlock();
            }
        }

        private void HandleGoldChanged(int _)
        {
            GameController.instance?.GetGameCurrencyDisplay()?.RefreshFromUserData();
            RefreshUI();
        }

        private void HandleBoosterCountChanged(BoosterType _, int __)
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (m_config == null) return;

            User.EnsureLoaded();
            int owned = User.data != null ? User.data.GetBoosterCount(m_config.type) : 0;
            int price1 = m_config.GetPriceForPack(1);
            int price3 = m_config.GetPriceForPack(3);

            if (m_titleText != null)
                m_titleText.text = "MUA BOOSTER";

            if (m_boosterIconImage != null && m_config.icon != null)
                m_boosterIconImage.sprite = m_config.icon;

            if (m_boosterNameText != null)
                m_boosterNameText.text = m_config.displayName;

            if (m_boosterDescText != null)
                m_boosterDescText.text = m_config.description;

            if (m_ownedCountText != null)
                m_ownedCountText.text = $"Đang có: <color=#29EBE0>{owned}</color>";

            if (m_pack1PriceText != null)
                m_pack1PriceText.text = price1.ToString("N0");

            if (m_pack3PriceText != null)
                m_pack3PriceText.text = price3.ToString("N0");

            if (m_pack3SaveTagText != null)
            {
                int singleTotal = price1 * 3;
                int savePercent = singleTotal > 0 ? Mathf.RoundToInt((1f - (float)price3 / singleTotal) * 100f) : 0;
                bool hasDiscount = savePercent > 0;
                m_pack3SaveTagText.transform.parent.gameObject.SetActive(hasDiscount);
                if (hasDiscount)
                {
                    m_pack3SaveTagText.text = $"-{savePercent}%";
                }
            }
        }

        private void OnClickBuyPack1()
        {
            TryBuyPack(1);
        }

        private void OnClickBuyPack3()
        {
            TryBuyPack(3);
        }

        private void TryBuyPack(int packSize)
        {
            if (m_isClosing || m_config == null) return;

            PlayClickSfx();
            User.EnsureLoaded();
            if (User.data == null) return;

            int price = m_config.GetPriceForPack(packSize);
            if (User.data.TrySpendGold(price))
            {
                User.data.AddBooster(m_config.type, packSize);
                User.Save();

                if (HomeController.instance != null)
                {
                    HomeController.instance.SetData();
                }

                GameController.instance?.GetGameCurrencyDisplay()?.RefreshFromUserData();
                m_onPurchased?.Invoke();
                RefreshUI();
                ShowToast($"+{packSize} {m_config.displayName}!");

                m_autoCloseTween?.Kill();
                m_autoCloseTween = DOVirtual.DelayedCall(0.55f, Close).SetUpdate(true);
                return;
            }

            // Not enough gold -> open NotEnoughGoldPopup
            NotEnoughGoldPopupController.Open(
                requiredGold: price,
                adRewardGold: Mathf.Max(500, price),
                onRewardClaimed: () =>
                {
                    GameController.instance?.GetGameCurrencyDisplay()?.RefreshFromUserData();
                    RefreshUI();
                },
                callerCanvas: m_callerCanvas);
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
                .AppendInterval(1.1f)
                .Append(m_toastCanvasGroup.DOFade(0f, 0.25f));
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
            TextMeshProUGUI titleText,
            Button closeButton,
            Image boosterIconImage,
            TextMeshProUGUI boosterNameText,
            TextMeshProUGUI boosterDescText,
            TextMeshProUGUI ownedCountText,
            Button buyPack1Button,
            TextMeshProUGUI pack1PriceText,
            Button buyPack3Button,
            TextMeshProUGUI pack3PriceText,
            TextMeshProUGUI pack3SaveTagText,
            CanvasGroup toastCanvasGroup,
            TextMeshProUGUI toastText)
        {
            m_titleText = titleText;
            m_closeButton = closeButton;
            m_boosterIconImage = boosterIconImage;
            m_boosterNameText = boosterNameText;
            m_boosterDescText = boosterDescText;
            m_ownedCountText = ownedCountText;
            m_buyPack1Button = buyPack1Button;
            m_pack1PriceText = pack1PriceText;
            m_buyPack3Button = buyPack3Button;
            m_pack3PriceText = pack3PriceText;
            m_pack3SaveTagText = pack3SaveTagText;
            m_toastCanvasGroup = toastCanvasGroup;
            m_toastText = toastText;
        }
    }
}
