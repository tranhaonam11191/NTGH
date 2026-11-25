using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// Reusable Neon 90s Heart/Lives HUD pill.
    /// Shows current hearts (e.g. 5/5), live regeneration countdown (MM:SS or FULL),
    /// and opens BuyHeartPopup when clicked.
    /// </summary>
    [DisallowMultipleComponent]
    public class HeartDisplayView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_textHeartCount;
        [SerializeField] private TextMeshProUGUI m_textTimer;
        [SerializeField] private Image m_iconHeart;
        [SerializeField] private Button m_addButton;
        [SerializeField] private bool m_autoRefreshOnEnable = true;
        [SerializeField] private bool m_openBuyPopupOnClick = true;

        private float m_nextTickTime;

        public TextMeshProUGUI HeartCountText => m_textHeartCount;
        public TextMeshProUGUI TimerText => m_textTimer;
        public Image HeartIcon => m_iconHeart;
        public Button AddButton => m_addButton;

        public event Action Clicked;

        private void Awake()
        {
            if (m_addButton != null)
            {
                m_addButton.onClick.AddListener(HandleClick);
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                User.OnHeartsChanged += HandleHeartsChanged;
                if (m_autoRefreshOnEnable)
                {
                    RefreshFromUserData();
                }

                m_nextTickTime = Time.unscaledTime + 1f;
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                User.OnHeartsChanged -= HandleHeartsChanged;
            }
        }

        private void OnDestroy()
        {
            if (m_addButton != null)
            {
                m_addButton.onClick.RemoveListener(HandleClick);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (Time.unscaledTime < m_nextTickTime)
            {
                return;
            }

            m_nextTickTime = Time.unscaledTime + 1f;

            User.EnsureLoaded();
            if (User.data == null)
            {
                return;
            }

            if (User.data.RefreshHearts())
            {
                User.Save();
            }

            UpdateVisuals();
        }

        public void RefreshFromUserData()
        {
            User.EnsureLoaded();
            if (User.data != null && User.data.RefreshHearts())
            {
                User.Save();
            }

            UpdateVisuals();
        }

        private void HandleHeartsChanged(int _)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (User.data == null)
            {
                return;
            }

            int current = User.data.hearts;
            int max = UserData.MAX_HEARTS;

            if (m_textHeartCount != null)
            {
                m_textHeartCount.text = $"{current}/{max}";
            }

            if (m_textTimer != null)
            {
                if (current >= max)
                {
                    m_textTimer.text = "FULL";
                }
                else
                {
                    int remaining = User.data.GetSecondsUntilNextHeart();
                    m_textTimer.text = UserData.FormatHeartTimer(remaining);
                }
            }
        }

        private void HandleClick()
        {
            if (SOManager.instance != null)
            {
                SOManager.instance.PlayAudio("Click2");
            }

            Clicked?.Invoke();

            if (m_openBuyPopupOnClick)
            {
                Canvas ownerCanvas = GetComponentInParent<Canvas>();
                BuyHeartPopupController.Open(callerCanvas: ownerCanvas);
            }
        }

        public void ConfigureReferences(
            TextMeshProUGUI textHeartCount,
            TextMeshProUGUI textTimer,
            Image iconHeart,
            Button addButton)
        {
            m_textHeartCount = textHeartCount;
            m_textTimer = textTimer;
            m_iconHeart = iconHeart;
            m_addButton = addButton;
        }
    }
}
