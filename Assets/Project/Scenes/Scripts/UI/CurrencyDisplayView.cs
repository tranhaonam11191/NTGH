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

        public TextMeshProUGUI AmountText => m_textAmount;
        public Image DollarIcon => m_iconDollar;
        public Button AddButton => m_addButton;

        private void OnEnable()
        {
            if (m_autoRefreshOnEnable && Application.isPlaying)
            {
                RefreshFromPlayerPrefs();
            }
        }

        public void RefreshFromPlayerPrefs()
        {
            SetAmount(PlayerPrefs.GetInt(Const.GOLD));
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
