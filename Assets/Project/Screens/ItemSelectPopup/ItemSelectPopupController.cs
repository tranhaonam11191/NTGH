using System;
using System.Collections.Generic;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// Generic Neon 90s Item Selection Popup (Prefab Variant of NeonPopup_Base).
    /// Receives a list of SelectableItemEntry (typically from an ItemCatalogSO),
    /// a display label (e.g. "TRANG PHỤC", "NHÀ CỬA", "VŨ KHÍ"), and a selection callback.
    /// Automatically displays group filter tabs when items have multiple GroupLabel values.
    /// </summary>
    [DisallowMultipleComponent]
    public class ItemSelectPopupController : MonoBehaviour
    {
        public const string ScreenResourceName = "ItemSelectPopup";

        [Header("Base Popup (Inherited from NeonPopup_Base)")]
        [SerializeField] private NeonPopupBase m_popupBase;

        [Header("Item Select Content")]
        [SerializeField] private GameObject m_groupFilterBar;
        [SerializeField] private RectTransform m_groupFilterContainer;
        [SerializeField] private Button m_groupTabTemplate;
        [SerializeField] private ScrollRect m_scrollRect;
        [SerializeField] private RectTransform m_gridContent;
        [SerializeField] private CustomizeItemCardView m_cardPrefab;

        [Header("Ad Fallback (Editor / No Fill)")]
        [SerializeField] private bool m_simulateAdWhenUnavailable = true;

        private readonly List<CustomizeItemCardView> m_spawnedCards = new List<CustomizeItemCardView>();
        private readonly List<Button> m_groupButtons = new List<Button>();
        private readonly List<string> m_groupLabels = new List<string>();
        private readonly List<SelectableItemEntry> m_visibleItems = new List<SelectableItemEntry>();

        private IReadOnlyList<SelectableItemEntry> m_items;
        private Action<SelectableItemEntry> m_onItemSelected;
        private Func<SelectableItemEntry, bool> m_isEquippedPredicate;
        private Func<SelectableItemEntry, bool> m_isUnlockedPredicate;
        private string m_equipActionText = "CHỌN";
        private string m_equippedStatusText = "ĐANG DÙNG";
        private string m_activeGroup = string.Empty;

        // Neon 90s filter tab palette
        private static readonly Color ColTabActiveFill = new Color(0.06f, 0.20f, 0.24f, 0.96f);
        private static readonly Color ColTabActiveOuter = new Color(0.16f, 0.92f, 0.88f, 1f);
        private static readonly Color ColTabActiveInner = new Color(1.00f, 0.78f, 0.30f, 1f);
        private static readonly Color ColTabActiveLabel = new Color(1.00f, 0.96f, 0.86f, 1f);

        private static readonly Color ColTabInactiveFill = new Color(0.05f, 0.07f, 0.10f, 0.86f);
        private static readonly Color ColTabInactiveOuter = new Color(0.24f, 0.38f, 0.44f, 0.75f);
        private static readonly Color ColTabInactiveInner = new Color(0.45f, 0.38f, 0.24f, 0.40f);
        private static readonly Color ColTabInactiveLabel = new Color(0.62f, 0.70f, 0.76f, 0.90f);

        public static void Open(
            ItemCatalogSO catalog,
            Action<SelectableItemEntry> onItemSelected,
            Func<SelectableItemEntry, bool> isEquipped = null,
            Func<SelectableItemEntry, bool> isUnlocked = null,
            string labelOverride = null,
            Canvas callerCanvas = null)
        {
            if (catalog == null)
            {
                Debug.LogWarning("[ItemSelectPopupController] Cannot open popup: catalog is null.");
                return;
            }

            string label = !string.IsNullOrEmpty(labelOverride) ? labelOverride : catalog.DefaultLabel;
            Open(
                catalog.Items,
                label,
                onItemSelected,
                isEquipped,
                isUnlocked,
                catalog.EquipActionLabel,
                catalog.EquippedStatusLabel,
                callerCanvas);
        }

        public static void Open(
            IReadOnlyList<SelectableItemEntry> items,
            string label,
            Action<SelectableItemEntry> onItemSelected,
            Func<SelectableItemEntry, bool> isEquipped = null,
            Func<SelectableItemEntry, bool> isUnlocked = null,
            string equipActionText = "CHỌN",
            string equippedStatusText = "ĐANG DÙNG",
            Canvas callerCanvas = null)
        {
            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<ItemSelectPopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: popup => popup.Setup(
                    items,
                    label,
                    onItemSelected,
                    isEquipped,
                    isUnlocked,
                    equipActionText,
                    equippedStatusText),
                hasShield: true,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_popupBase == null)
                m_popupBase = GetComponent<NeonPopupBase>();

            if (m_groupTabTemplate != null)
            {
                m_groupTabTemplate.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            User.OnGoldChanged += HandleGoldChanged;
        }

        private void OnDisable()
        {
            User.OnGoldChanged -= HandleGoldChanged;
        }

        public void Setup(
            IReadOnlyList<SelectableItemEntry> items,
            string label,
            Action<SelectableItemEntry> onItemSelected,
            Func<SelectableItemEntry, bool> isEquipped = null,
            Func<SelectableItemEntry, bool> isUnlocked = null,
            string equipActionText = "CHỌN",
            string equippedStatusText = "ĐANG DÙNG")
        {
            if (m_popupBase == null)
                m_popupBase = GetComponent<NeonPopupBase>();

            m_items = items;
            m_onItemSelected = onItemSelected;
            m_isEquippedPredicate = isEquipped;
            m_isUnlockedPredicate = isUnlocked;
            m_equipActionText = string.IsNullOrEmpty(equipActionText) ? "CHỌN" : equipActionText;
            m_equippedStatusText = string.IsNullOrEmpty(equippedStatusText) ? "ĐANG DÙNG" : equippedStatusText;

            if (m_popupBase != null)
            {
                m_popupBase.SetTitle(string.IsNullOrEmpty(label) ? "CHỌN VẬT PHẨM" : label.ToUpperInvariant());                
                if (m_popupBase.CurrencyDisplay != null)
                {
                    m_popupBase.CurrencyDisplay.RefreshFromUserData();
                }
            }

            RebuildGroupFilters();
            RebuildGrid();

            if (m_scrollRect != null)
            {
                m_scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void Close()
        {
            if (m_popupBase != null)
                m_popupBase.OnClickClose();
            else
                Core.Close(this);
        }

        private void ShowToast(string message)
        {
            if (m_popupBase != null)
                m_popupBase.ShowToast(message);
        }

        public void SelectGroup(string groupLabel)
        {
            m_activeGroup = groupLabel ?? string.Empty;
            UpdateGroupTabVisuals();
            RebuildGrid();

            if (m_scrollRect != null)
            {
                m_scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void HandleGoldChanged(int _)
        {
            if (gameObject.activeInHierarchy && m_items != null)
            {
                RebuildGrid();
            }
        }

        private void RebuildGroupFilters()
        {
            m_groupLabels.Clear();
            if (m_items != null)
            {
                for (int i = 0; i < m_items.Count; i++)
                {
                    SelectableItemEntry item = m_items[i];
                    if (item == null || string.IsNullOrEmpty(item.GroupLabel))
                        continue;

                    if (!m_groupLabels.Contains(item.GroupLabel))
                    {
                        m_groupLabels.Add(item.GroupLabel);
                    }
                }
            }

            bool showFilters = m_groupLabels.Count >= 2 && m_groupFilterBar != null && m_groupTabTemplate != null;
            if (m_groupFilterBar != null)
            {
                m_groupFilterBar.SetActive(showFilters);
            }

            if (!showFilters)
            {
                m_activeGroup = string.Empty;
                return;
            }

            if (string.IsNullOrEmpty(m_activeGroup) || !m_groupLabels.Contains(m_activeGroup))
            {
                m_activeGroup = m_groupLabels[0];
            }

            while (m_groupButtons.Count < m_groupLabels.Count)
            {
                Button btn = Instantiate(m_groupTabTemplate, m_groupFilterContainer, false);
                m_groupButtons.Add(btn);
            }

            for (int i = 0; i < m_groupButtons.Count; i++)
            {
                bool active = i < m_groupLabels.Count;
                Button btn = m_groupButtons[i];
                btn.gameObject.SetActive(active);
                if (!active) continue;

                string group = m_groupLabels[i];
                btn.name = $"GroupTab_{group}";
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectGroup(group));

                NeonButtonStyle style = btn.GetComponent<NeonButtonStyle>();
                if (style != null)
                {
                    style.SetLabel(group.ToUpperInvariant());
                }
                else
                {
                    TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = group.ToUpperInvariant();
                }
            }

            UpdateGroupTabVisuals();
        }

        private void UpdateGroupTabVisuals()
        {
            for (int i = 0; i < m_groupLabels.Count && i < m_groupButtons.Count; i++)
            {
                NeonButtonStyle style = m_groupButtons[i].GetComponent<NeonButtonStyle>();
                if (style == null) continue;

                bool isSelected = m_groupLabels[i] == m_activeGroup;
                if (isSelected)
                {
                    style.SetPalette(ColTabActiveFill, ColTabActiveOuter, ColTabActiveInner, Color.white, ColTabActiveLabel);
                }
                else
                {
                    style.SetPalette(ColTabInactiveFill, ColTabInactiveOuter, ColTabInactiveInner, Color.white, ColTabInactiveLabel);
                }
            }
        }

        private void RebuildGrid()
        {
            if (m_gridContent == null || m_cardPrefab == null)
                return;

            m_visibleItems.Clear();
            if (m_items != null)
            {
                for (int i = 0; i < m_items.Count; i++)
                {
                    SelectableItemEntry item = m_items[i];
                    if (item == null) continue;

                    if (!string.IsNullOrEmpty(m_activeGroup) && item.GroupLabel != m_activeGroup)
                        continue;

                    m_visibleItems.Add(item);
                }
            }

            SetActiveCardCount(m_visibleItems.Count);

            User.EnsureLoaded();
            int playerGold = User.data.gold;

            for (int i = 0; i < m_visibleItems.Count; i++)
            {
                SelectableItemEntry item = m_visibleItems[i];
                CustomizeItemCardView card = m_spawnedCards[i];

                bool isUnlocked = IsItemUnlocked(item);
                bool isEquipped = isUnlocked && m_isEquippedPredicate != null && m_isEquippedPredicate(item);
                int adsWatched = GetAdProgress(item);

                CustomizeCardViewModel vm = new CustomizeCardViewModel
                {
                    Title = item.DisplayName,
                    Subtitle = item.Subtitle,
                    PreviewSprite = item.PreviewSprite,
                    AdsWatched = adsWatched,
                    AdsRequired = item.AdsRequired,
                    CanAffordGold = playerGold >= item.PriceGold
                };

                if (isEquipped)
                {
                    vm.State = CustomizeItemVisualState.Equipped;
                    vm.ActionText = m_equippedStatusText;
                }
                else if (isUnlocked)
                {
                    vm.State = CustomizeItemVisualState.Unlocked;
                    vm.ActionText = m_equipActionText;
                }
                else if (item.UnlockMethod == CustomizeUnlockMethod.WatchAd)
                {
                    vm.State = CustomizeItemVisualState.LockedWatchAd;
                    vm.ActionText = item.AdsRequired > 1 ? $"QC {adsWatched}/{item.AdsRequired}" : "XEM QC";
                }
                else
                {
                    vm.State = CustomizeItemVisualState.LockedGold;
                    vm.ActionText = item.PriceGold.ToString("N0");
                }

                SelectableItemEntry capturedItem = item;
                CustomizeItemCardView capturedCard = card;
                card.Bind(vm, () => HandleCardClicked(capturedItem, capturedCard));
            }
        }

        private void HandleCardClicked(SelectableItemEntry item, CustomizeItemCardView card)
        {
            if (item == null)
                return;

            if (IsItemUnlocked(item))
            {
                m_onItemSelected?.Invoke(item);
                card.PlayUnlockPunch();
                ShowToast($"Đã chọn {item.DisplayName}");
                RebuildGrid();
                return;
            }

            if (item.UnlockMethod == CustomizeUnlockMethod.WatchAd)
            {
                WatchRewardedAd(() =>
                {
                    User.EnsureLoaded();
                    int watched = User.data.IncrementCustomizeAdProgress(GetAdProgressKey(item));
                    if (watched >= item.AdsRequired)
                    {
                        UnlockInUserData(item);
                        User.Save();
                        m_onItemSelected?.Invoke(item);
                        card.PlayUnlockPunch();
                        ShowToast($"Đã mở khóa & chọn {item.DisplayName}!");
                    }
                    else
                    {
                        User.Save();
                        card.PlayUnlockPunch();
                        ShowToast($"Đã xem quảng cáo ({watched}/{item.AdsRequired})");
                    }

                    RebuildGrid();
                });
                return;
            }

            User.EnsureLoaded();
            if (User.data.TrySpendGold(item.PriceGold))
            {
                UnlockInUserData(item);
                User.Save();
                m_onItemSelected?.Invoke(item);
                card.PlayUnlockPunch();
                ShowToast($"Đã mở khóa & chọn {item.DisplayName}!");
                RebuildGrid();
            }
            else
            {
                card.PlayErrorShake();
                ShowToast("Không đủ tiền để mở khóa!");
            }
        }

        private bool IsItemUnlocked(SelectableItemEntry item)
        {
            if (item == null) return false;
            if (item.UnlockMethod == CustomizeUnlockMethod.Free) return true;

            if (m_isUnlockedPredicate != null && m_isUnlockedPredicate(item))
                return true;

            User.EnsureLoaded();
            return User.data.IsItemUnlocked(item.Id);
        }

        private static void UnlockInUserData(SelectableItemEntry item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id)) return;
            User.data.UnlockItem(item.Id);
        }

        private static int GetAdProgress(SelectableItemEntry item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id)) return 0;
            User.EnsureLoaded();
            return User.data.GetCustomizeAdProgress(GetAdProgressKey(item));
        }

        private static string GetAdProgressKey(SelectableItemEntry item) => $"item_{item.Id}";

        private void WatchRewardedAd(Action onEarnedReward)
        {
            if (AdsController.instance != null && AdsController.instance.checkReward())
            {
                AdsController.instance.UserChoseToWatchAd(() => onEarnedReward?.Invoke());
                return;
            }

            if (m_simulateAdWhenUnavailable)
            {
                Debug.Log("[ItemSelectPopupController] Rewarded ad not loaded; granting simulated ad reward.");
                onEarnedReward?.Invoke();
                return;
            }

            ShowToast("Quảng cáo chưa sẵn sàng, thử lại sau!");
        }

        private void SetActiveCardCount(int count)
        {
            while (m_spawnedCards.Count < count)
            {
                CustomizeItemCardView card = Instantiate(m_cardPrefab, m_gridContent, false);
                m_spawnedCards.Add(card);
            }

            for (int i = 0; i < m_spawnedCards.Count; i++)
            {
                m_spawnedCards[i].gameObject.SetActive(i < count);
            }
        }

        public void ConfigureItemSelectReferences(
            NeonPopupBase popupBase,
            GameObject groupFilterBar,
            RectTransform groupFilterContainer,
            Button groupTabTemplate,
            ScrollRect scrollRect,
            RectTransform gridContent,
            CustomizeItemCardView cardPrefab)
        {
            m_popupBase = popupBase;
            m_groupFilterBar = groupFilterBar;
            m_groupFilterContainer = groupFilterContainer;
            m_groupTabTemplate = groupTabTemplate;
            m_scrollRect = scrollRect;
            m_gridContent = gridContent;
            m_cardPrefab = cardPrefab;
        }
    }
}
