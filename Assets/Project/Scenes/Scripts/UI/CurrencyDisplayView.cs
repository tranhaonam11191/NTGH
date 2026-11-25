using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// Reusable Neon 90s currency HUD prefab controller.
    /// Can be dropped into Home, Shop, Story, or Game scenes and auto-syncs with Const.GOLD.
    /// </summary>
    [DisallowMultipleComponent]
    public class CurrencyDisplayView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_textAmount;
        [SerializeField] private Image m_iconDollar;
        [SerializeField] private Button m_addButton;
        [SerializeField] private bool m_autoRefreshOnEnable = true;
        [SerializeField] private bool m_formatWithSeparator = true;
        [SerializeField] private bool m_openNotEnoughGoldPopupOnClick = true;
        [SerializeField] private int m_adRewardGold = 500;

        public TextMeshProUGUI AmountText => m_textAmount;
        public Image DollarIcon => m_iconDollar;
        public Button AddButton => m_addButton;

        public event Action AddClicked;

        private void Awake()
        {
            if (m_addButton != null)
            {
                m_addButton.onClick.AddListener(HandleAddClick);
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                User.OnGoldChanged += SetAmount;
                if (m_autoRefreshOnEnable)
                {
                    RefreshFromUserData();
                }
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                User.OnGoldChanged -= SetAmount;
            }
        }

        private void OnDestroy()
        {
            if (m_addButton != null)
            {
                m_addButton.onClick.RemoveListener(HandleAddClick);
            }
        }

        private void HandleAddClick()
        {
            AddClicked?.Invoke();

            if (!m_openNotEnoughGoldPopupOnClick)
                return;

            // Avoid re-opening when clicking the currency bar inside NotEnoughGoldPopup itself.
            if (GetComponentInParent<NotEnoughGoldPopupController>() != null)
                return;

            NotEnoughGoldPopupController.Open(
                requiredGold: 0,
                adRewardGold: Mathf.Max(1, m_adRewardGold),
                onRewardClaimed: RefreshFromUserData,
                callerCanvas: GetComponentInParent<Canvas>());
        }

        public void RefreshFromUserData()
        {
            User.EnsureLoaded();
            SetAmount(User.data.gold);
        }

        public void RefreshFromPlayerPrefs()
        {
            RefreshFromUserData();
        }

        public void SetAmount(int amount)
        {
            if (m_textAmount == null) return;
            m_textAmount.text = m_formatWithSeparator ? amount.ToString("N0") : amount.ToString();
        }

        public void ConfigureReferences(TextMeshProUGUI textAmount, Image iconDollar, Button addButton)
        {
            m_textAmount = textAmount;
            m_iconDollar = iconDollar;
            m_addButton = addButton;
        }
    }
}
