using DG.Tweening;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// Reusable Neon 90s base popup controller for SS.UI screens.
    /// Lives on NeonPopup_Base.prefab; specialized popups (e.g. ItemSelectPopup)
    /// are created as Prefab Variants inheriting this frame, header, currency bar, and toast.
    /// </summary>
    [DisallowMultipleComponent]
    public class NeonPopupBase : MonoBehaviour, IKeyBack
    {
        [Header("Base Popup References")]
        [SerializeField] protected RectTransform m_panelRoot;
        [SerializeField] protected TextMeshProUGUI m_titleText;
        [SerializeField] protected Button m_closeButton;        
        [SerializeField] protected CurrencyDisplayView m_currencyDisplay;
        [SerializeField] protected TextMeshProUGUI m_hintText;
        [SerializeField] protected RectTransform m_contentContainer;

        [Header("Base Toast")]
        [SerializeField] protected CanvasGroup m_toastCanvasGroup;
        [SerializeField] protected TextMeshProUGUI m_toastText;

        protected bool m_isClosing;
        private Sequence m_toastSequence;

        public RectTransform ContentContainer => m_contentContainer;
        public CurrencyDisplayView CurrencyDisplay => m_currencyDisplay;

        protected virtual void Awake()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnClickClose);
            }

            if (m_toastCanvasGroup != null)
            {
                m_toastCanvasGroup.alpha = 0f;
                m_toastCanvasGroup.blocksRaycasts = false;
            }
        }

        protected virtual void OnEnable()
        {
            m_isClosing = false;
            if (m_currencyDisplay != null)
            {
                m_currencyDisplay.RefreshFromUserData();
            }
        }

        protected virtual void OnDestroy()
        {
            m_toastSequence?.Kill();
            m_toastSequence = null;

            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnClickClose);
            }
        }

        public void SetTitle(string title)
        {
            if (m_titleText != null)
            {
                m_titleText.text = title ?? string.Empty;
            }
        }        

        public void ShowToast(string message)
        {
            if (m_toastCanvasGroup == null || m_toastText == null)
                return;

            m_toastText.text = message;
            m_toastSequence?.Kill();
            m_toastCanvasGroup.alpha = 0f;
            m_toastSequence = DOTween.Sequence()
                .Append(m_toastCanvasGroup.DOFade(1f, 0.16f))
                .AppendInterval(1.4f)
                .Append(m_toastCanvasGroup.DOFade(0f, 0.28f));
        }

        public virtual void OnKeyBack()
        {
            OnClickClose();
        }

        public virtual void OnClickClose()
        {
            if (m_isClosing)
                return;

            m_isClosing = true;
            Core.Close(this);
        }

        public static void EnsureSSCanvasAbove(Canvas callerCanvas)
        {
            Core.Init();
            EnsureEventSystem();
            Canvas ssCanvas = Core.Canvas;
            if (callerCanvas != null && ssCanvas != null && ssCanvas.sortingOrder <= callerCanvas.sortingOrder)
            {
                ssCanvas.sortingOrder = callerCanvas.sortingOrder + 10;
            }
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemGo.GetComponent<EventSystem>().sendNavigationEvents = true;
        }

        public void ConfigureBaseReferences(
            RectTransform panelRoot,
            TextMeshProUGUI titleText,
            Button closeButton,
            GameObject currencyRow,
            CurrencyDisplayView currencyDisplay,
            TextMeshProUGUI hintText,
            RectTransform contentContainer,
            CanvasGroup toastCanvasGroup,
            TextMeshProUGUI toastText)
        {
            m_panelRoot = panelRoot;
            m_titleText = titleText;
            m_closeButton = closeButton;            
            m_currencyDisplay = currencyDisplay;
            m_hintText = hintText;
            m_contentContainer = contentContainer;
            m_toastCanvasGroup = toastCanvasGroup;
            m_toastText = toastText;
        }
    }
}
