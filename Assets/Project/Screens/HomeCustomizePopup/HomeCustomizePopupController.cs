using System.Collections.Generic;
using DG.Tweening;
using NTGH.Demo.OutfitSystem;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    public enum HomeCustomizeTab
    {
        Outfit = 0,
        House = 1
    }

    [DisallowMultipleComponent]
    public class HomeCustomizePopupController : MonoBehaviour, IKeyBack
    {
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private CurrencyDisplayView m_currencyDisplay;
        [SerializeField] private Button m_closeButton;

        [Header("Main Tabs")]
        [SerializeField] private Button m_tabOutfitButton;
        [SerializeField] private NeonButtonStyle m_tabOutfitStyle;
        [SerializeField] private Button m_tabHouseButton;
        [SerializeField] private NeonButtonStyle m_tabHouseStyle;

        [Header("House Part Sub-Tabs")]
        [SerializeField] private GameObject m_houseSubTabBar;
        [SerializeField] private Button m_subTabAwningButton;
        [SerializeField] private NeonButtonStyle m_subTabAwningStyle;
        [SerializeField] private Button m_subTabDoorButton;
        [SerializeField] private NeonButtonStyle m_subTabDoorStyle;
        [SerializeField] private Button m_subTabWallButton;
        [SerializeField] private NeonButtonStyle m_subTabWallStyle;

        [Header("Grid Content")]
        [SerializeField] private ScrollRect m_scrollRect;
        [SerializeField] private RectTransform m_gridContent;
        [SerializeField] private CustomizeItemCardView m_cardPrefab;

        [Header("Toast Feedback")]
        [SerializeField] private CanvasGroup m_toastCanvasGroup;
        [SerializeField] private TextMeshProUGUI m_toastText;

        [Header("Ad Fallback (Editor / No Fill)")]
        [SerializeField] private bool m_simulateAdWhenUnavailable = true;

        private readonly List<CustomizeItemCardView> m_spawnedCards = new List<CustomizeItemCardView>();
        private HomeController m_homeController;
        private HomeCustomizeTab m_currentTab = HomeCustomizeTab.Outfit;
        private HomeHouseTheme.Part m_currentHousePart = HomeHouseTheme.Part.Awning;
        private bool m_isClosing;
        private Sequence m_toastSequence;

        // Tab Neon 90s palettes
        private static readonly Color ColTabActiveFill = new Color(0.06f, 0.20f, 0.24f, 0.96f);
        private static readonly Color ColTabActiveOuter = new Color(0.16f, 0.92f, 0.88f, 1f);
        private static readonly Color ColTabActiveInner = new Color(1.00f, 0.78f, 0.30f, 1f);
        private static readonly Color ColTabActiveIcon = new Color(1.00f, 0.88f, 0.45f, 1f);
        private static readonly Color ColTabActiveLabel = new Color(1.00f, 0.96f, 0.86f, 1f);

        private static readonly Color ColTabInactiveFill = new Color(0.05f, 0.07f, 0.10f, 0.86f);
        private static readonly Color ColTabInactiveOuter = new Color(0.24f, 0.38f, 0.44f, 0.75f);
        private static readonly Color ColTabInactiveInner = new Color(0.45f, 0.38f, 0.24f, 0.40f);
        private static readonly Color ColTabInactiveIcon = new Color(0.55f, 0.65f, 0.70f, 0.85f);
        private static readonly Color ColTabInactiveLabel = new Color(0.62f, 0.70f, 0.76f, 0.90f);

        private void Awake()
        {
            if (m_closeButton != null) m_closeButton.onClick.AddListener(OnClickClose);
            if (m_tabOutfitButton != null) m_tabOutfitButton.onClick.AddListener(() => SelectTab(HomeCustomizeTab.Outfit));
            if (m_tabHouseButton != null) m_tabHouseButton.onClick.AddListener(() => SelectTab(HomeCustomizeTab.House));
            if (m_subTabAwningButton != null) m_subTabAwningButton.onClick.AddListener(() => SelectHousePart(HomeHouseTheme.Part.Awning));
            if (m_subTabDoorButton != null) m_subTabDoorButton.onClick.AddListener(() => SelectHousePart(HomeHouseTheme.Part.Door));
            if (m_subTabWallButton != null) m_subTabWallButton.onClick.AddListener(() => SelectHousePart(HomeHouseTheme.Part.Wall));

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
        }

        private void OnDisable()
        {
            User.OnGoldChanged -= HandleGoldChanged;
        }

        private void OnDestroy()
        {
            m_toastSequence?.Kill();
            m_toastSequence = null;

            if (m_closeButton != null) m_closeButton.onClick.RemoveListener(OnClickClose);
        }

        public void Setup(HomeController homeController, HomeCustomizeTab initialTab = HomeCustomizeTab.Outfit)
        {
            m_homeController = homeController != null ? homeController : HomeController.instance;
            m_isClosing = false;

            if (m_currencyDisplay != null)
                m_currencyDisplay.RefreshFromUserData();

            SelectTab(initialTab);
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
            Core.Close(this);
        }

        public void SelectTab(HomeCustomizeTab tab)
        {
            m_currentTab = tab;
            UpdateTabVisuals();
            RebuildGrid();

            if (m_scrollRect != null)
                m_scrollRect.verticalNormalizedPosition = 1f;
        }

        public void SelectHousePart(HomeHouseTheme.Part part)
        {
            m_currentHousePart = part;
            UpdateHouseSubTabVisuals();
            if (m_currentTab == HomeCustomizeTab.House)
            {
                RebuildGrid();
                if (m_scrollRect != null)
                    m_scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void HandleGoldChanged(int _)
        {
            if (gameObject.activeInHierarchy)
            {
                RebuildGrid();
            }
        }

        private void UpdateTabVisuals()
        {
            bool isOutfit = m_currentTab == HomeCustomizeTab.Outfit;
            ApplyTabStyle(m_tabOutfitStyle, isOutfit);
            ApplyTabStyle(m_tabHouseStyle, !isOutfit);

            if (m_houseSubTabBar != null)
                m_houseSubTabBar.SetActive(!isOutfit);

            if (m_titleText != null)
                m_titleText.text = isOutfit ? "CHỌN TRANG PHỤC" : "NÂNG CẤP NHÀ CỬA";

            UpdateHouseSubTabVisuals();
        }

        private void UpdateHouseSubTabVisuals()
        {
            ApplyTabStyle(m_subTabAwningStyle, m_currentHousePart == HomeHouseTheme.Part.Awning);
            ApplyTabStyle(m_subTabDoorStyle, m_currentHousePart == HomeHouseTheme.Part.Door);
            ApplyTabStyle(m_subTabWallStyle, m_currentHousePart == HomeHouseTheme.Part.Wall);
        }

        private static void ApplyTabStyle(NeonButtonStyle style, bool isActive)
        {
            if (style == null) return;

            if (isActive)
            {
                style.SetPalette(
                    ColTabActiveFill,
                    ColTabActiveOuter,
                    ColTabActiveInner,
                    ColTabActiveIcon,
                    ColTabActiveLabel);
            }
            else
            {
                style.SetPalette(
                    ColTabInactiveFill,
                    ColTabInactiveOuter,
                    ColTabInactiveInner,
                    ColTabInactiveIcon,
                    ColTabInactiveLabel);
            }
        }

        private void RebuildGrid()
        {
            if (m_gridContent == null || m_cardPrefab == null)
                return;

            if (m_homeController == null)
                m_homeController = HomeController.instance;

            User.EnsureLoaded();

            if (m_currentTab == HomeCustomizeTab.Outfit)
            {
                PopulateOutfitCards();
            }
            else
            {
                PopulateHouseCards();
            }
        }

        private void PopulateOutfitCards()
        {
            OutfitProgression progression = m_homeController != null ? m_homeController.Outfits : null;
            if (progression == null || progression.Outfits == null)
            {
                SetActiveCardCount(0);
                return;
            }

            IReadOnlyList<OutfitData> outfits = progression.Outfits;
            SetActiveCardCount(outfits.Count);

            int equippedIndex = progression.CurrentOutfitIndex;
            int playerGold = User.data.gold;

            for (int i = 0; i < outfits.Count; i++)
            {
                OutfitData outfit = outfits[i];
                CustomizeItemCardView card = m_spawnedCards[i];
                if (outfit == null)
                {
                    card.gameObject.SetActive(false);
                    continue;
                }

                bool isUnlocked = progression.IsUnlocked(i);
                bool isEquipped = isUnlocked && i == equippedIndex;
                int adsWatched = progression.GetAdProgress(outfit);
                int adsRequired = outfit.AdsRequired;

                CustomizeCardViewModel vm = new CustomizeCardViewModel
                {
                    Title = outfit.DisplayName,
                    Subtitle = $"{outfit.RankTitle} • +{outfit.BonusHealth} HP",
                    PreviewSprite = ResolveOutfitPreview(outfit),
                    AdsWatched = adsWatched,
                    AdsRequired = adsRequired,
                    CanAffordGold = playerGold >= outfit.PriceGold
                };

                if (isEquipped)
                {
                    vm.State = CustomizeItemVisualState.Equipped;
                    vm.ActionText = "ĐANG MẶC";
                }
                else if (isUnlocked)
                {
                    vm.State = CustomizeItemVisualState.Unlocked;
                    vm.ActionText = "MẶC";
                }
                else if (outfit.UnlockMethod == CustomizeUnlockMethod.WatchAd)
                {
                    vm.State = CustomizeItemVisualState.LockedWatchAd;
                    vm.ActionText = adsRequired > 1 ? $"QC {adsWatched}/{adsRequired}" : "XEM QC";
                }
                else
                {
                    vm.State = CustomizeItemVisualState.LockedGold;
                    vm.ActionText = outfit.PriceGold.ToString("N0");
                }

                int capturedIndex = i;
                card.Bind(vm, () => HandleOutfitCardClicked(capturedIndex, card));
            }
        }

        private void PopulateHouseCards()
        {
            HomeHouseTheme houseTheme = m_homeController != null ? m_homeController.HouseTheme : null;
            if (houseTheme == null)
            {
                SetActiveCardCount(0);
                return;
            }

            HomeHouseTheme.Level[] levels =
            {
                HomeHouseTheme.Level.Dilapidated,
                HomeHouseTheme.Level.Patched,
                HomeHouseTheme.Level.Renovated
            };

            SetActiveCardCount(levels.Length);
            HomeHouseTheme.Level equippedLevel = houseTheme.GetPartLevel(m_currentHousePart);
            int playerGold = User.data.gold;

            for (int i = 0; i < levels.Length; i++)
            {
                HomeHouseTheme.Level level = levels[i];
                HomeHouseTheme.HouseItemSpec spec = HomeHouseTheme.GetItemSpec(m_currentHousePart, level);
                CustomizeItemCardView card = m_spawnedCards[i];

                bool isUnlocked = houseTheme.IsPartLevelUnlocked(m_currentHousePart, level);
                bool isEquipped = isUnlocked && level == equippedLevel;
                int adsWatched = houseTheme.GetAdProgress(m_currentHousePart, level);

                CustomizeCardViewModel vm = new CustomizeCardViewModel
                {
                    Title = spec.DisplayName,
                    Subtitle = spec.TierLabel,
                    PreviewSprite = houseTheme.GetPartSprite(m_currentHousePart, level),
                    AdsWatched = adsWatched,
                    AdsRequired = spec.AdsRequired,
                    CanAffordGold = playerGold >= spec.PriceGold
                };

                if (isEquipped)
                {
                    vm.State = CustomizeItemVisualState.Equipped;
                    vm.ActionText = "ĐANG DÙNG";
                }
                else if (isUnlocked)
                {
                    vm.State = CustomizeItemVisualState.Unlocked;
                    vm.ActionText = "CHỌN";
                }
                else if (spec.UnlockMethod == CustomizeUnlockMethod.WatchAd)
                {
                    vm.State = CustomizeItemVisualState.LockedWatchAd;
                    vm.ActionText = spec.AdsRequired > 1 ? $"QC {adsWatched}/{spec.AdsRequired}" : "XEM QC";
                }
                else
                {
                    vm.State = CustomizeItemVisualState.LockedGold;
                    vm.ActionText = spec.PriceGold.ToString("N0");
                }

                HomeHouseTheme.Part capturedPart = m_currentHousePart;
                HomeHouseTheme.Level capturedLevel = level;
                card.Bind(vm, () => HandleHouseCardClicked(capturedPart, capturedLevel, card));
            }
        }

        private void HandleOutfitCardClicked(int outfitIndex, CustomizeItemCardView card)
        {
            OutfitProgression progression = m_homeController != null ? m_homeController.Outfits : null;
            if (progression == null || outfitIndex < 0 || outfitIndex >= progression.Outfits.Count)
                return;

            OutfitData outfit = progression.Outfits[outfitIndex];
            if (outfit == null)
                return;

            if (progression.IsUnlocked(outfitIndex))
            {
                if (progression.EquipOutfitByIndex(outfitIndex))
                {
                    card.PlayUnlockPunch();
                    ShowToast($"Đã mặc {outfit.DisplayName}");
                    RebuildGrid();
                }
                return;
            }

            if (outfit.UnlockMethod == CustomizeUnlockMethod.WatchAd)
            {
                WatchRewardedAd(() =>
                {
                    if (progression.TryRecordAdWatch(outfitIndex, out bool unlockedNow, autoEquip: true))
                    {
                        m_homeController?.SetData();
                        card.PlayUnlockPunch();
                        if (unlockedNow)
                        {
                            ShowToast($"Đã mở khóa & mặc {outfit.DisplayName}!");
                        }
                        else
                        {
                            int progress = progression.GetAdProgress(outfit);
                            ShowToast($"Đã xem quảng cáo ({progress}/{outfit.AdsRequired})");
                        }
                        RebuildGrid();
                    }
                });
                return;
            }

            if (progression.TryUnlockWithGold(outfitIndex, autoEquip: true))
            {
                m_homeController?.SetData();
                card.PlayUnlockPunch();
                ShowToast($"Đã mua & mặc {outfit.DisplayName}!");
                RebuildGrid();
            }
            else
            {
                card.PlayErrorShake();
                ShowToast("Không đủ tiền để mở khóa!");
            }
        }

        private void HandleHouseCardClicked(HomeHouseTheme.Part part, HomeHouseTheme.Level level, CustomizeItemCardView card)
        {
            HomeHouseTheme houseTheme = m_homeController != null ? m_homeController.HouseTheme : null;
            if (houseTheme == null)
                return;

            HomeHouseTheme.HouseItemSpec spec = HomeHouseTheme.GetItemSpec(part, level);

            if (houseTheme.IsPartLevelUnlocked(part, level))
            {
                houseTheme.SetPartLevel(part, level);
                card.PlayUnlockPunch();
                ShowToast($"Đã chọn {spec.DisplayName}");
                RebuildGrid();
                return;
            }

            if (spec.UnlockMethod == CustomizeUnlockMethod.WatchAd)
            {
                WatchRewardedAd(() =>
                {
                    if (houseTheme.TryRecordAdWatch(part, level, out bool unlockedNow, autoEquip: true))
                    {
                        card.PlayUnlockPunch();
                        if (unlockedNow)
                        {
                            ShowToast($"Đã mở khóa {spec.DisplayName}!");
                        }
                        else
                        {
                            int progress = houseTheme.GetAdProgress(part, level);
                            ShowToast($"Đã xem quảng cáo ({progress}/{spec.AdsRequired})");
                        }
                        RebuildGrid();
                    }
                });
                return;
            }

            if (houseTheme.TryUnlockWithGold(part, level, autoEquip: true))
            {
                m_homeController?.SetData();
                card.PlayUnlockPunch();
                ShowToast($"Đã nâng cấp {spec.DisplayName}!");
                RebuildGrid();
            }
            else
            {
                card.PlayErrorShake();
                ShowToast("Không đủ tiền để nâng cấp!");
            }
        }

        private void WatchRewardedAd(System.Action onEarnedReward)
        {
            if (AdsController.instance != null && AdsController.instance.checkReward())
            {
                AdsController.instance.UserChoseToWatchAd(() => onEarnedReward?.Invoke());
                return;
            }

            if (m_simulateAdWhenUnavailable)
            {
                Debug.Log("[HomeCustomizePopup] Rewarded ad not loaded; granting simulated ad reward.");
                onEarnedReward?.Invoke();
                return;
            }

            ShowToast("Quảng cáo chưa sẵn sàng, thử lại sau!");
        }

        private static Sprite ResolveOutfitPreview(OutfitData outfit)
        {
            if (outfit == null) return null;
            if (outfit.PreviewSprite != null) return outfit.PreviewSprite;
            if (outfit.SpriteLibraryAsset != null)
                return outfit.SpriteLibraryAsset.GetSprite("Torso", "Default");
            return null;
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

        private void ShowToast(string message)
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

        public void ConfigureReferences(
            TextMeshProUGUI titleText,
            CurrencyDisplayView currencyDisplay,
            Button closeButton,
            Button tabOutfitButton,
            NeonButtonStyle tabOutfitStyle,
            Button tabHouseButton,
            NeonButtonStyle tabHouseStyle,
            GameObject houseSubTabBar,
            Button subTabAwningButton,
            NeonButtonStyle subTabAwningStyle,
            Button subTabDoorButton,
            NeonButtonStyle subTabDoorStyle,
            Button subTabWallButton,
            NeonButtonStyle subTabWallStyle,
            ScrollRect scrollRect,
            RectTransform gridContent,
            CustomizeItemCardView cardPrefab,
            CanvasGroup toastCanvasGroup,
            TextMeshProUGUI toastText)
        {
            m_titleText = titleText;
            m_currencyDisplay = currencyDisplay;
            m_closeButton = closeButton;
            m_tabOutfitButton = tabOutfitButton;
            m_tabOutfitStyle = tabOutfitStyle;
            m_tabHouseButton = tabHouseButton;
            m_tabHouseStyle = tabHouseStyle;
            m_houseSubTabBar = houseSubTabBar;
            m_subTabAwningButton = subTabAwningButton;
            m_subTabAwningStyle = subTabAwningStyle;
            m_subTabDoorButton = subTabDoorButton;
            m_subTabDoorStyle = subTabDoorStyle;
            m_subTabWallButton = subTabWallButton;
            m_subTabWallStyle = subTabWallStyle;
            m_scrollRect = scrollRect;
            m_gridContent = gridContent;
            m_cardPrefab = cardPrefab;
            m_toastCanvasGroup = toastCanvasGroup;
            m_toastText = toastText;
        }
    }
}
