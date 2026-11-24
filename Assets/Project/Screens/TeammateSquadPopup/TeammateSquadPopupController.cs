using System;
using System.Collections.Generic;
using NTGH.Teammates;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// SS UI Screen for managing the player's 4-slot Teammate Squad and purchasing new Teammate Cards on Home.
    /// Reuses TeammateCardView (separated character portrait + Attack/Support card frame).
    /// </summary>
    [DisallowMultipleComponent]
    public class TeammateSquadPopupController : MonoBehaviour, IKeyBack
    {
        public const string ScreenResourceName = "TeammateSquadPopup";

        private enum FilterMode { All, Attack, Support }

        [Header("Header & Currency")]
        [SerializeField] private TextMeshProUGUI m_txtTitle;
        [SerializeField] private TextMeshProUGUI m_txtGold;
        [SerializeField] private TextMeshProUGUI m_txtSquadHeader;
        [SerializeField] private TextMeshProUGUI m_txtStatusNotice;
        [SerializeField] private Button m_btnClose;

        [Header("4 Equipped Squad Slots")]
        [SerializeField] private TeammateCardView[] m_squadSlotViews = new TeammateCardView[4];
        [SerializeField] private Button[] m_squadSlotButtons = new Button[4];

        [Header("Filter Tabs")]
        [SerializeField] private Button m_btnFilterAll;
        [SerializeField] private Button m_btnFilterAttack;
        [SerializeField] private Button m_btnFilterSupport;

        [Header("Shop / Collection Grid")]
        [SerializeField] private ScrollRect m_scrollRect;
        [SerializeField] private RectTransform m_gridContent;
        [SerializeField] private GameObject m_shopItemTemplate;

        [Header("Pre-Battle Action")]
        [SerializeField] private Button m_btnStartBattle;
        [SerializeField] private TextMeshProUGUI m_txtStartBattleLabel;

        private TeammateCardDatabaseSO m_database;
        private Action m_onChanged;
        private Action m_onClosed;
        private bool m_isPreBattle;
        private bool m_isClosing;
        private FilterMode m_filterMode = FilterMode.All;
        private readonly HashSet<string> m_sessionNewCardIds = new HashSet<string>();
        private readonly List<GameObject> m_spawnedItems = new List<GameObject>();

        public static void Open(
            TeammateCardDatabaseSO database,
            Action onChanged = null,
            Canvas callerCanvas = null,
            IReadOnlyList<string> highlightedNewCardIds = null,
            Action onClosed = null)
        {
            if (database == null)
            {
                Debug.LogWarning("[TeammateSquadPopupController] Cannot open popup: database is null.");
                onClosed?.Invoke();
                return;
            }

            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<TeammateSquadPopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: popup => popup.Setup(
                    database,
                    onChanged: onChanged,
                    isPreBattle: false,
                    highlightedNewCardIds: highlightedNewCardIds,
                    onClosed: onClosed),
                hasShield: true,
                hideTopScreen: false);
        }

        public static void OpenForPreBattle(
            TeammateCardDatabaseSO database,
            IReadOnlyList<string> highlightedNewCardIds,
            Action onConfirmBattle,
            Canvas callerCanvas = null)
        {
            if (database == null)
            {
                Debug.LogWarning("[TeammateSquadPopupController] Cannot open pre-battle popup: database is null.");
                onConfirmBattle?.Invoke();
                return;
            }

            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<TeammateSquadPopupController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: false,
                onScreenLoad: popup => popup.Setup(
                    database,
                    onChanged: null,
                    isPreBattle: true,
                    highlightedNewCardIds: highlightedNewCardIds,
                    onClosed: onConfirmBattle),
                hasShield: true,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_btnClose != null) m_btnClose.onClick.AddListener(ClosePopup);
            if (m_btnStartBattle != null) m_btnStartBattle.onClick.AddListener(ClosePopup);
            if (m_btnFilterAll != null) m_btnFilterAll.onClick.AddListener(() => SetFilter(FilterMode.All));
            if (m_btnFilterAttack != null) m_btnFilterAttack.onClick.AddListener(() => SetFilter(FilterMode.Attack));
            if (m_btnFilterSupport != null) m_btnFilterSupport.onClick.AddListener(() => SetFilter(FilterMode.Support));

            if (m_squadSlotButtons != null)
            {
                for (int i = 0; i < m_squadSlotButtons.Length; i++)
                {
                    int slotIndex = i;
                    if (m_squadSlotButtons[i] != null)
                    {
                        m_squadSlotButtons[i].onClick.AddListener(() => OnClickSquadSlot(slotIndex));
                    }
                }
            }

            if (m_shopItemTemplate != null)
            {
                m_shopItemTemplate.SetActive(false);
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

        public void OnKeyBack()
        {
            ClosePopup();
        }

        public void ClosePopup()
        {
            if (m_isClosing) return;
            m_isClosing = true;

            User.EnsureLoaded();
            if (User.data != null && User.data.ClearAllNewTeammates())
            {
                User.Save();
            }

            var onClosed = m_onClosed;
            m_onClosed = null;
            Core.Close(this, () => onClosed?.Invoke());
        }

        public void Setup(
            TeammateCardDatabaseSO database,
            Action onChanged = null,
            bool isPreBattle = false,
            IReadOnlyList<string> highlightedNewCardIds = null,
            Action onClosed = null)
        {
            m_database = database;
            m_onChanged = onChanged;
            m_onClosed = onClosed;
            m_isPreBattle = isPreBattle;
            m_isClosing = false;
            m_filterMode = FilterMode.All;

            User.EnsureLoaded();
            if (User.data != null && User.data.EnsureTeammateDefaults(m_database))
            {
                User.Save();
            }

            m_sessionNewCardIds.Clear();
            if (User.data?.unseen_teammate_ids != null)
            {
                for (int i = 0; i < User.data.unseen_teammate_ids.Count; i++)
                {
                    if (!string.IsNullOrEmpty(User.data.unseen_teammate_ids[i]))
                    {
                        m_sessionNewCardIds.Add(User.data.unseen_teammate_ids[i]);
                    }
                }
            }
            if (highlightedNewCardIds != null)
            {
                for (int i = 0; i < highlightedNewCardIds.Count; i++)
                {
                    if (!string.IsNullOrEmpty(highlightedNewCardIds[i]))
                    {
                        m_sessionNewCardIds.Add(highlightedNewCardIds[i]);
                    }
                }
            }

            ApplyPreBattleLayout();
            SetNotice(m_isPreBattle
                ? "Đồng đội mới đã gia nhập! Chạm XUẤT TRẬN để xếp vào đội hình."
                : string.Empty);
            RefreshAll();

            if (m_scrollRect != null)
            {
                m_scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void ApplyPreBattleLayout()
        {
            if (m_btnStartBattle != null)
            {
                m_btnStartBattle.gameObject.SetActive(m_isPreBattle);
            }

            if (m_txtStartBattleLabel != null)
            {
                m_txtStartBattleLabel.text = "VÀO TRẬN";
            }

            if (m_scrollRect != null)
            {
                var rt = m_scrollRect.GetComponent<RectTransform>();
                if (rt != null)
                {
                    var offsetMin = rt.offsetMin;
                    offsetMin.y = m_isPreBattle ? 88f : 20f;
                    rt.offsetMin = offsetMin;
                }
            }
        }

        private void HandleGoldChanged(int gold)
        {
            if (m_txtGold != null)
            {
                m_txtGold.text = $"{gold:N0} $";
            }
        }

        private void SetFilter(FilterMode mode)
        {
            m_filterMode = mode;
            RefreshGrid();
            RefreshFilterVisuals();
        }

        private void RefreshAll()
        {
            User.EnsureLoaded();
            if (m_txtTitle != null) m_txtTitle.text = m_isPreBattle ? "SẮP XẾP ĐỘI HÌNH" : "ĐỘI HÌNH ĐỒNG ĐỘI";
            if (m_txtGold != null && User.data != null) m_txtGold.text = $"{User.data.gold:N0} $";

            RefreshSquadSlots();
            RefreshFilterVisuals();
            RefreshGrid();
        }

        private void RefreshSquadSlots()
        {
            if (User.data == null || m_database == null) return;

            int equippedCount = User.data.GetEquippedTeammateCount();
            if (m_txtSquadHeader != null)
            {
                m_txtSquadHeader.text = $"ĐỘI HÌNH XUẤT TRẬN ({equippedCount}/{UserData.MaxTeammateSlots})  •  Chạm ô để tháo";
            }

            for (int i = 0; i < m_squadSlotViews.Length; i++)
            {
                var view = m_squadSlotViews[i];
                if (view == null) continue;

                string cardId = User.data.GetEquippedTeammateAt(i);
                var card = m_database.GetCardById(cardId);
                var theme = card != null ? m_database.GetTheme(card.CardType) : null;
                view.Bind(card, theme, $"Ô {i + 1}");
            }
        }

        private void RefreshFilterVisuals()
        {
            SetTabStyle(m_btnFilterAll, m_filterMode == FilterMode.All);
            SetTabStyle(m_btnFilterAttack, m_filterMode == FilterMode.Attack);
            SetTabStyle(m_btnFilterSupport, m_filterMode == FilterMode.Support);
        }

        private static void SetTabStyle(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = active
                    ? new Color(0.12f, 0.52f, 0.58f, 0.96f)
                    : new Color(0.08f, 0.11f, 0.16f, 0.88f);
            }
        }

        private void RefreshGrid()
        {
            for (int i = 0; i < m_spawnedItems.Count; i++)
            {
                if (m_spawnedItems[i] != null) Destroy(m_spawnedItems[i]);
            }
            m_spawnedItems.Clear();

            if (m_database == null || m_shopItemTemplate == null || m_gridContent == null) return;

            var cards = m_database.Cards;
            var newCards = new List<TeammateCardSO>();
            var otherCards = new List<TeammateCardSO>();

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null) continue;

                bool unlocked = User.data != null && User.data.IsTeammateUnlocked(card.Id);
                if (card.StoryOnly && !unlocked) continue;

                if (m_filterMode == FilterMode.Attack && card.CardType != TeammateCardType.Attack) continue;
                if (m_filterMode == FilterMode.Support && card.CardType != TeammateCardType.Support) continue;

                if (m_sessionNewCardIds.Contains(card.Id))
                {
                    newCards.Add(card);
                }
                else
                {
                    otherCards.Add(card);
                }
            }

            newCards.AddRange(otherCards);

            for (int i = 0; i < newCards.Count; i++)
            {
                var card = newCards[i];
                var itemGo = Instantiate(m_shopItemTemplate, m_gridContent);
                itemGo.name = $"ShopCard_{card.Id}";
                itemGo.SetActive(true);
                m_spawnedItems.Add(itemGo);

                BindShopItem(itemGo, card);
            }
        }

        private void BindShopItem(GameObject itemGo, TeammateCardSO card)
        {
            var cardView = itemGo.GetComponentInChildren<TeammateCardView>(true);
            if (cardView != null)
            {
                cardView.Bind(card, m_database.GetTheme(card.CardType));
            }

            var badgeNew = itemGo.transform.Find("BadgeNew");
            if (badgeNew != null)
            {
                badgeNew.gameObject.SetActive(m_sessionNewCardIds.Contains(card.Id));
            }

            var effectTmp = itemGo.transform.Find("TxtEffect")?.GetComponent<TextMeshProUGUI>();
            if (effectTmp != null)
            {
                effectTmp.text = card.GetEffectSummary();
            }

            var actionBtn = itemGo.transform.Find("BtnAction")?.GetComponent<Button>();
            var actionBg = actionBtn != null ? actionBtn.GetComponent<Image>() : null;
            var actionLabel = actionBtn != null ? actionBtn.GetComponentInChildren<TextMeshProUGUI>() : null;

            bool unlocked = User.data != null && User.data.IsTeammateUnlocked(card.Id);
            bool equipped = User.data != null && User.data.IsTeammateEquipped(card.Id);

            if (actionLabel != null)
            {
                if (!unlocked)
                {
                    actionLabel.text = card.PriceGold > 0 ? $"MUA {card.PriceGold:N0}$" : "NHẬN MIỄN PHÍ";
                }
                else if (equipped)
                {
                    actionLabel.text = "THÁO RA";
                }
                else
                {
                    actionLabel.text = "XUẤT TRẬN";
                }
            }

            if (actionBg != null)
            {
                if (!unlocked)
                {
                    actionBg.color = new Color(0.82f, 0.56f, 0.12f, 1f);
                }
                else if (equipped)
                {
                    actionBg.color = new Color(0.62f, 0.18f, 0.22f, 1f);
                }
                else
                {
                    actionBg.color = new Color(0.12f, 0.62f, 0.58f, 1f);
                }
            }

            if (actionBtn != null)
            {
                actionBtn.onClick.RemoveAllListeners();
                actionBtn.onClick.AddListener(() => OnClickCardAction(card));
            }
        }

        private void OnClickSquadSlot(int slotIndex)
        {
            if (User.data == null) return;
            if (User.data.UnequipTeammateAt(slotIndex))
            {
                User.Save();
                SetNotice($"Đã tháo đồng đội khỏi ô {slotIndex + 1}.");
                RefreshAll();
                m_onChanged?.Invoke();
            }
        }

        private void OnClickCardAction(TeammateCardSO card)
        {
            if (card == null || User.data == null) return;

            bool unlocked = User.data.IsTeammateUnlocked(card.Id);
            if (!unlocked)
            {
                if (card.PriceGold > 0 && !User.data.TrySpendGold(card.PriceGold))
                {
                    NotEnoughGoldPopupController.Open(
                        card.PriceGold,
                        onRewardClaimed: RefreshAll,
                        callerCanvas: GetComponentInParent<Canvas>());
                    return;
                }

                User.data.UnlockTeammate(card.Id);
                User.data.TryEquipTeammate(card.Id);
                User.Save();
                SetNotice($"Đã chiêu mộ {card.DisplayName}!");
                RefreshAll();
                m_onChanged?.Invoke();
                return;
            }

            if (User.data.IsTeammateEquipped(card.Id))
            {
                User.data.UnequipTeammate(card.Id);
                User.Save();
                SetNotice($"Đã tháo {card.DisplayName} khỏi đội hình.");
            }
            else if (User.data.TryEquipTeammate(card.Id))
            {
                User.Save();
                SetNotice($"Đã đưa {card.DisplayName} vào đội hình!");
            }
            else
            {
                SetNotice("Đội hình đã đủ 4/4 đồng đội! Chạm vào 1 ô phía trên để tháo bớt.");
            }

            RefreshAll();
            m_onChanged?.Invoke();
        }

        private void SetNotice(string message)
        {
            if (m_txtStatusNotice == null) return;
            m_txtStatusNotice.text = message ?? string.Empty;
            m_txtStatusNotice.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        public void SetEditorReferences(
            TextMeshProUGUI txtTitle,
            TextMeshProUGUI txtGold,
            TextMeshProUGUI txtSquadHeader,
            TextMeshProUGUI txtStatusNotice,
            Button btnClose,
            TeammateCardView[] squadSlotViews,
            Button[] squadSlotButtons,
            Button btnFilterAll,
            Button btnFilterAttack,
            Button btnFilterSupport,
            ScrollRect scrollRect,
            RectTransform gridContent,
            GameObject shopItemTemplate,
            Button btnStartBattle,
            TextMeshProUGUI txtStartBattleLabel)
        {
            m_txtTitle = txtTitle;
            m_txtGold = txtGold;
            m_txtSquadHeader = txtSquadHeader;
            m_txtStatusNotice = txtStatusNotice;
            m_btnClose = btnClose;
            m_squadSlotViews = squadSlotViews;
            m_squadSlotButtons = squadSlotButtons;
            m_btnFilterAll = btnFilterAll;
            m_btnFilterAttack = btnFilterAttack;
            m_btnFilterSupport = btnFilterSupport;
            m_scrollRect = scrollRect;
            m_gridContent = gridContent;
            m_shopItemTemplate = shopItemTemplate;
            m_btnStartBattle = btnStartBattle;
            m_txtStartBattleLabel = txtStartBattleLabel;
        }
    }
}
