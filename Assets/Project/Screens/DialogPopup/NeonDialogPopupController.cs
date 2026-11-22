using System;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    public enum NeonDialogType
    {
        Notice = 0,  // Single OK button
        Confirm = 1  // Yes / No buttons
    }

    /// <summary>
    /// Simple reusable Neon 90s Dialog Popup (Prefab Variant of NeonPopup_Base) supporting two modes:
    /// 1. Notice: single OK button to acknowledge/close (e.g. "Coming Soon", info alerts).
    /// 2. Confirm: Yes / No buttons with separate callbacks.
    /// </summary>
    [DisallowMultipleComponent]
    public class NeonDialogPopupController : MonoBehaviour
    {
        public const string ScreenResourceName = "NeonDialogPopup";

        [Header("Base Popup (Inherited from NeonPopup_Base)")]
        [SerializeField] private NeonPopupBase m_popupBase;

        [Header("Dialog Content")]
        [SerializeField] private TextMeshProUGUI m_messageText;

        [Header("Notice Mode (Single OK Button)")]
        [SerializeField] private GameObject m_noticeButtonGroup;
        [SerializeField] private Button m_okButton;
        [SerializeField] private TextMeshProUGUI m_okLabel;

        [Header("Confirm Mode (Yes / No Buttons)")]
        [SerializeField] private GameObject m_confirmButtonGroup;
        [SerializeField] private Button m_yesButton;
        [SerializeField] private TextMeshProUGUI m_yesLabel;
        [SerializeField] private Button m_noButton;
        [SerializeField] private TextMeshProUGUI m_noLabel;

        private NeonDialogType m_dialogType = NeonDialogType.Notice;
        private Action m_onPrimaryAction;
        private Action m_onSecondaryAction;
        private bool m_hasResolved;

        public static void ShowNotice(
            string title,
            string message,
            string okLabel = "OK",
            Action onOk = null,
            Canvas callerCanvas = null)
        {
            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<NeonDialogPopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: popup => popup.SetupNotice(title, message, okLabel, onOk),
                hasShield: true,
                hideTopScreen: false);
        }

        public static void ShowConfirm(
            string title,
            string message,
            Action onYes,
            Action onNo = null,
            string yesLabel = "YES",
            string noLabel = "NO",
            Canvas callerCanvas = null)
        {
            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<NeonDialogPopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: popup => popup.SetupConfirm(title, message, onYes, onNo, yesLabel, noLabel),
                hasShield: true,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_popupBase == null)
                m_popupBase = GetComponent<NeonPopupBase>();

            if (m_okButton != null)
                m_okButton.onClick.AddListener(OnClickOk);

            if (m_yesButton != null)
                m_yesButton.onClick.AddListener(OnClickYes);

            if (m_noButton != null)
                m_noButton.onClick.AddListener(OnClickNo);
        }

        private void OnDestroy()
        {
            if (m_okButton != null)
                m_okButton.onClick.RemoveListener(OnClickOk);

            if (m_yesButton != null)
                m_yesButton.onClick.RemoveListener(OnClickYes);

            if (m_noButton != null)
                m_noButton.onClick.RemoveListener(OnClickNo);
        }

        public void SetupNotice(string title, string message, string okLabel = "OK", Action onOk = null)
        {
            EnsureBasePopup();
            m_dialogType = NeonDialogType.Notice;
            m_onPrimaryAction = onOk;
            m_onSecondaryAction = null;
            m_hasResolved = false;

            ApplyCommonVisuals(title, message);

            if (m_noticeButtonGroup != null)
                m_noticeButtonGroup.SetActive(true);
            if (m_confirmButtonGroup != null)
                m_confirmButtonGroup.SetActive(false);

            if (m_okLabel != null)
                m_okLabel.text = string.IsNullOrEmpty(okLabel) ? "OK" : okLabel.ToUpperInvariant();
        }

        public void SetupConfirm(
            string title,
            string message,
            Action onYes,
            Action onNo = null,
            string yesLabel = "YES",
            string noLabel = "NO")
        {
            EnsureBasePopup();
            m_dialogType = NeonDialogType.Confirm;
            m_onPrimaryAction = onYes;
            m_onSecondaryAction = onNo;
            m_hasResolved = false;

            ApplyCommonVisuals(title, message);

            if (m_noticeButtonGroup != null)
                m_noticeButtonGroup.SetActive(false);
            if (m_confirmButtonGroup != null)
                m_confirmButtonGroup.SetActive(true);

            if (m_yesLabel != null)
                m_yesLabel.text = string.IsNullOrEmpty(yesLabel) ? "YES" : yesLabel.ToUpperInvariant();
            if (m_noLabel != null)
                m_noLabel.text = string.IsNullOrEmpty(noLabel) ? "NO" : noLabel.ToUpperInvariant();
        }

        private void ApplyCommonVisuals(string title, string message)
        {
            if (m_popupBase != null)
            {
                m_popupBase.SetTitle(string.IsNullOrEmpty(title) ? "THÔNG BÁO" : title.ToUpperInvariant());
            }

            if (m_messageText != null)
            {
                m_messageText.text = message ?? string.Empty;
            }
        }

        private void EnsureBasePopup()
        {
            if (m_popupBase == null)
                m_popupBase = GetComponent<NeonPopupBase>();
        }

        private void OnClickOk()
        {
            if (m_hasResolved)
                return;

            m_hasResolved = true;
            Action callback = m_onPrimaryAction;
            Close();
            callback?.Invoke();
        }

        private void OnClickYes()
        {
            if (m_hasResolved)
                return;

            m_hasResolved = true;
            Action callback = m_onPrimaryAction;
            Close();
            callback?.Invoke();
        }

        private void OnClickNo()
        {
            if (m_hasResolved)
                return;

            m_hasResolved = true;
            Action callback = m_onSecondaryAction;
            Close();
            callback?.Invoke();
        }

        public void Close()
        {
            if (m_popupBase != null)
                m_popupBase.OnClickClose();
            else
                Core.Close(this);
        }

        public void ConfigureReferences(
            NeonPopupBase popupBase,
            TextMeshProUGUI messageText,
            GameObject noticeButtonGroup,
            Button okButton,
            TextMeshProUGUI okLabel,
            GameObject confirmButtonGroup,
            Button yesButton,
            TextMeshProUGUI yesLabel,
            Button noButton,
            TextMeshProUGUI noLabel)
        {
            m_popupBase = popupBase;
            m_messageText = messageText;
            m_noticeButtonGroup = noticeButtonGroup;
            m_okButton = okButton;
            m_okLabel = okLabel;
            m_confirmButtonGroup = confirmButtonGroup;
            m_yesButton = yesButton;
            m_yesLabel = yesLabel;
            m_noButton = noButton;
            m_noLabel = noLabel;
        }
    }
}
