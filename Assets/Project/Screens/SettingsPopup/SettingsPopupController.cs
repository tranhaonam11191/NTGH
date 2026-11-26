using System;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// Shared Neon 90s Settings popup (SS.UI screen) used by both Home and Game scenes.
    /// - Home mode: audio settings only (SFX / BGM).
    /// - Game mode: audio settings + Exit Game button (deducts 1 heart with confirmation).
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingsPopupController : MonoBehaviour, IKeyBack
    {
        public const string ScreenResourceName = "SettingsPopup";

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private Button m_closeButton;

        [Header("Audio Controls")]
        [SerializeField] private Toggle m_sfxToggle;
        [SerializeField] private TextMeshProUGUI m_sfxStateText;
        [SerializeField] private Image m_sfxTrackImage;
        [SerializeField] private RectTransform m_sfxKnob;

        [SerializeField] private Toggle m_bgmToggle;
        [SerializeField] private TextMeshProUGUI m_bgmStateText;
        [SerializeField] private Image m_bgmTrackImage;
        [SerializeField] private RectTransform m_bgmKnob;

        [Header("Game-Only Exit Action")]
        [SerializeField] private GameObject m_exitSection;
        [SerializeField] private Button m_exitGameButton;
        [SerializeField] private RectTransform m_panelRoot;

        [Header("Panel Heights")]
        [SerializeField] private float m_homePanelHeight = 460f;
        [SerializeField] private float m_gamePanelHeight = 610f;

        private static readonly Color ToggleOnTrackColor = new Color(0.10f, 0.52f, 0.50f, 1f);
        private static readonly Color ToggleOffTrackColor = new Color(0.16f, 0.19f, 0.25f, 1f);
        private static readonly Color ToggleOnTextColor = new Color(0.16f, 0.92f, 0.88f, 1f);
        private static readonly Color ToggleOffTextColor = new Color(0.55f, 0.60f, 0.68f, 1f);

        private bool m_showExitGame;
        private Action m_onExitGame;
        private Action m_onClosed;
        private Canvas m_callerCanvas;
        private bool m_isClosing;
        private bool m_hasNotifiedClosed;

        public static void OpenForHome(Canvas callerCanvas = null)
        {
            Open(showExitGame: false, onExitGame: null, onClosed: null, callerCanvas: callerCanvas);
        }

        public static void OpenForGame(Action onExitGame, Action onClosed = null, Canvas callerCanvas = null)
        {
            Open(showExitGame: true, onExitGame: onExitGame, onClosed: onClosed, callerCanvas: callerCanvas);
        }

        private static void Open(bool showExitGame, Action onExitGame, Action onClosed, Canvas callerCanvas)
        {
            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<SettingsPopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: popup => popup.Setup(showExitGame, onExitGame, onClosed, callerCanvas),
                hasShield: true,
                shieldAlpha: 0.76f,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_closeButton != null)
                m_closeButton.onClick.AddListener(OnClickClose);

            if (m_sfxToggle != null)
                m_sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);

            if (m_bgmToggle != null)
                m_bgmToggle.onValueChanged.AddListener(OnBgmToggleChanged);

            if (m_exitGameButton != null)
                m_exitGameButton.onClick.AddListener(OnClickExitGame);
        }

        private void OnDestroy()
        {
            if (m_closeButton != null)
                m_closeButton.onClick.RemoveListener(OnClickClose);

            if (m_sfxToggle != null)
                m_sfxToggle.onValueChanged.RemoveListener(OnSfxToggleChanged);

            if (m_bgmToggle != null)
                m_bgmToggle.onValueChanged.RemoveListener(OnBgmToggleChanged);

            if (m_exitGameButton != null)
                m_exitGameButton.onClick.RemoveListener(OnClickExitGame);

            NotifyClosedOnce();
        }

        public void Setup(bool showExitGame, Action onExitGame, Action onClosed, Canvas callerCanvas = null)
        {
            m_showExitGame = showExitGame;
            m_onExitGame = onExitGame;
            m_onClosed = onClosed;
            m_callerCanvas = callerCanvas;
            m_isClosing = false;
            m_hasNotifiedClosed = false;

            if (m_titleText != null)
                m_titleText.text = "CÀI ĐẶT";

            if (m_exitSection != null)
                m_exitSection.SetActive(m_showExitGame);

            if (m_panelRoot != null)
            {
                Vector2 size = m_panelRoot.sizeDelta;
                size.y = m_showExitGame ? m_gamePanelHeight : m_homePanelHeight;
                m_panelRoot.sizeDelta = size;
            }

            RefreshAudioToggles();
        }

        private void RefreshAudioToggles()
        {
            bool sfxOn = SoundManager.SfxVolume > 0f;
            bool bgmOn = SoundManager.BgmVolume > 0f;

            if (m_sfxToggle != null)
                m_sfxToggle.SetIsOnWithoutNotify(sfxOn);
            UpdateToggleVisual(sfxOn, m_sfxStateText, m_sfxTrackImage, m_sfxKnob);

            if (m_bgmToggle != null)
                m_bgmToggle.SetIsOnWithoutNotify(bgmOn);
            UpdateToggleVisual(bgmOn, m_bgmStateText, m_bgmTrackImage, m_bgmKnob);
        }

        private void OnSfxToggleChanged(bool isOn)
        {
            SoundManager.SfxVolume = isOn ? 1f : 0f;
            SoundManager.Save();
            UpdateToggleVisual(isOn, m_sfxStateText, m_sfxTrackImage, m_sfxKnob);

            if (isOn)
                PlayClickSfx();
        }

        private void OnBgmToggleChanged(bool isOn)
        {
            SoundManager.BgmVolume = isOn ? 1f : 0f;
            SoundManager.Save();
            if (SoundManager.instance != null && SoundManager.instance.BGM != null)
            {
                SoundManager.instance.BGM.volume = SoundManager.BgmVolume * SoundManager.DefaultBgmVolumeScale;
            }

            UpdateToggleVisual(isOn, m_bgmStateText, m_bgmTrackImage, m_bgmKnob);
            PlayClickSfx();
        }

        private static void UpdateToggleVisual(bool isOn, TextMeshProUGUI stateText, Image trackImage, RectTransform knob)
        {
            if (stateText != null)
            {
                stateText.text = isOn ? "BẬT" : "TẮT";
                stateText.color = isOn ? ToggleOnTextColor : ToggleOffTextColor;
            }

            if (trackImage != null)
                trackImage.color = isOn ? ToggleOnTrackColor : ToggleOffTrackColor;

            if (knob != null)
                knob.anchoredPosition = new Vector2(isOn ? 24f : -24f, 0f);
        }

        private void OnClickExitGame()
        {
            if (m_isClosing || !m_showExitGame)
                return;

            PlayClickSfx();

            NeonDialogPopupController.ShowConfirm(
                title: "THOÁT GAME",
                message: "Thoát giữa trận sẽ bị trừ <color=#FF5C77>1 tim</color>.\nBạn có chắc chắn muốn thoát?",
                onYes: ConfirmExitGame,
                onNo: null,
                yesLabel: "THOÁT",
                noLabel: "TIẾP TỤC",
                callerCanvas: m_callerCanvas);
        }

        private void ConfirmExitGame()
        {
            if (m_isClosing)
                return;

            m_isClosing = true;
            Action exitCallback = m_onExitGame;
            m_onExitGame = null;

            Core.Close(this, () =>
            {
                if (exitCallback != null)
                {
                    exitCallback.Invoke();
                }
                else
                {
                    User.CommitPendingBattleHeartAndSave(1);
                    SOManager.LoadHome();
                }

                NotifyClosedOnce();
            });
        }

        public void OnKeyBack()
        {
            OnClickClose();
        }

        public void OnClickClose()
        {
            if (m_isClosing)
                return;

            m_isClosing = true;
            PlayClickSfx();
            Core.Close(this, NotifyClosedOnce);
        }

        private void NotifyClosedOnce()
        {
            if (m_hasNotifiedClosed)
                return;

            m_hasNotifiedClosed = true;
            Action callback = m_onClosed;
            m_onClosed = null;
            callback?.Invoke();
        }

        private static void PlayClickSfx()
        {
            if (SOManager.instance != null)
                SOManager.instance.PlayAudio("Click2");
        }

        public void SetEditorReferences(
            RectTransform panelRoot,
            TextMeshProUGUI titleText,
            Button closeButton,
            Toggle sfxToggle,
            TextMeshProUGUI sfxStateText,
            Image sfxTrackImage,
            RectTransform sfxKnob,
            Toggle bgmToggle,
            TextMeshProUGUI bgmStateText,
            Image bgmTrackImage,
            RectTransform bgmKnob,
            GameObject exitSection,
            Button exitGameButton)
        {
            m_panelRoot = panelRoot;
            m_titleText = titleText;
            m_closeButton = closeButton;
            m_sfxToggle = sfxToggle;
            m_sfxStateText = sfxStateText;
            m_sfxTrackImage = sfxTrackImage;
            m_sfxKnob = sfxKnob;
            m_bgmToggle = bgmToggle;
            m_bgmStateText = bgmStateText;
            m_bgmTrackImage = bgmTrackImage;
            m_bgmKnob = bgmKnob;
            m_exitSection = exitSection;
            m_exitGameButton = exitGameButton;
        }
    }
}
