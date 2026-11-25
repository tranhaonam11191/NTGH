using DG.Tweening;
using NTGH.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.Boosters
{
    [DisallowMultipleComponent]
    public class GameBoosterHUD : MonoBehaviour
    {
        [Header("Data & Board")]
        [SerializeField] private BoosterDatabaseSO m_database;
        [SerializeField] private ShapesManager m_shapesManager;

        [Header("Slots (Order: Hammer, FreeSwap, LineBlast, Shuffle)")]
        [SerializeField] private BoosterSlotView[] m_slots;

        [Header("Targeting Banner")]
        [SerializeField] private GameObject m_targetingBanner;
        [SerializeField] private TextMeshProUGUI m_targetingHintText;
        [SerializeField] private Button m_cancelTargetingButton;

        [Header("Toast Feedback")]
        [SerializeField] private CanvasGroup m_toastCanvasGroup;
        [SerializeField] private TextMeshProUGUI m_toastText;

        private Sequence m_toastSequence;
        private bool m_lastBoardBusy;

        private void Awake()
        {
            if (m_database == null)
            {
                m_database = BoosterDatabaseSO.LoadDefault();
            }

            if (m_shapesManager == null)
            {
                m_shapesManager = FindFirstObjectByType<ShapesManager>();
            }

            if (m_cancelTargetingButton != null)
            {
                m_cancelTargetingButton.onClick.AddListener(OnClickCancelTargeting);
            }

            if (m_targetingBanner != null)
            {
                m_targetingBanner.SetActive(false);
            }

            if (m_toastCanvasGroup != null)
            {
                m_toastCanvasGroup.alpha = 0f;
                m_toastCanvasGroup.blocksRaycasts = false;
            }
        }

        private void OnEnable()
        {
            User.OnBoosterCountChanged += HandleBoosterCountChanged;
            if (m_shapesManager != null)
            {
                m_shapesManager.OnBoosterModeChanged += HandleBoosterModeChanged;
                m_shapesManager.OnBoosterExecuted += HandleBoosterExecuted;
            }
        }

        private void OnDisable()
        {
            User.OnBoosterCountChanged -= HandleBoosterCountChanged;
            if (m_shapesManager != null)
            {
                m_shapesManager.OnBoosterModeChanged -= HandleBoosterModeChanged;
                m_shapesManager.OnBoosterExecuted -= HandleBoosterExecuted;
            }

            m_toastSequence?.Kill();
            m_toastSequence = null;
        }

        private void OnDestroy()
        {
            m_toastSequence?.Kill();
            m_toastSequence = null;

            if (m_cancelTargetingButton != null)
            {
                m_cancelTargetingButton.onClick.RemoveListener(OnClickCancelTargeting);
            }
        }

        private void Start()
        {
            if (m_database == null)
            {
                m_database = BoosterDatabaseSO.LoadDefault();
            }

            if (m_shapesManager == null)
            {
                m_shapesManager = FindFirstObjectByType<ShapesManager>();
                if (m_shapesManager != null)
                {
                    m_shapesManager.OnBoosterModeChanged += HandleBoosterModeChanged;
                    m_shapesManager.OnBoosterExecuted += HandleBoosterExecuted;
                }
            }

            User.EnsureLoaded();
            if (User.data != null && m_database != null)
            {
                if (User.data.EnsureBoosterDefaults(m_database, GetCurrentLevelNumber()))
                {
                    User.Save();
                }
            }

            if (m_slots != null)
            {
                for (int i = 0; i < m_slots.Length; i++)
                {
                    if (m_slots[i] == null) continue;
                    m_slots[i].Bind(m_slots[i].BoosterType, HandleSlotClicked);
                }
            }

            RefreshAllSlots();
        }

        private void Update()
        {
            if (m_shapesManager == null) return;
            bool busy = m_shapesManager.IsBoardBusy;
            if (busy != m_lastBoardBusy)
            {
                m_lastBoardBusy = busy;
                RefreshAllSlots();
            }
        }

        private int GetCurrentLevelNumber()
        {
            if (SOManager.instance != null)
            {
                return Mathf.Max(1, SOManager.instance.CurrentLevelNumber);
            }

            User.EnsureLoaded();
            return User.data != null ? Mathf.Max(1, User.data.level + 1) : 1;
        }

        private void HandleBoosterCountChanged(BoosterType _, int __)
        {
            RefreshAllSlots();
        }

        private void HandleBoosterModeChanged(BoosterType? activeMode)
        {
            if (m_targetingBanner != null)
            {
                bool show = activeMode.HasValue;
                m_targetingBanner.SetActive(show);
                if (show && m_targetingHintText != null)
                {
                    BoosterConfig cfg = m_database != null ? m_database.GetConfig(activeMode.Value) : null;
                    m_targetingHintText.text = cfg != null && !string.IsNullOrEmpty(cfg.targetingHint)
                        ? cfg.targetingHint
                        : "Chọn mục tiêu trên bàn cờ";
                }
            }

            RefreshAllSlots();
        }

        private void HandleBoosterExecuted(BoosterType _)
        {
            RefreshAllSlots();
        }

        private void OnClickCancelTargeting()
        {
            PlayClickSfx();
            m_shapesManager?.CancelBoosterMode();
        }

        private void HandleSlotClicked(BoosterType type)
        {
            if (GameController.instance != null && GameController.instance.BlocksBoard)
            {
                return;
            }

            PlayClickSfx();

            if (m_database == null)
            {
                m_database = BoosterDatabaseSO.LoadDefault();
            }

            BoosterConfig config = m_database != null ? m_database.GetConfig(type) : null;
            if (config == null)
            {
                Debug.LogWarning($"[GameBoosterHUD] Missing BoosterConfig for {type}.");
                return;
            }

            int levelNum = GetCurrentLevelNumber();
            if (!config.IsUnlockedAtLevel(levelNum))
            {
                ShowToast($"Mở khóa ở Màn {config.unlockLevel}!");
                return;
            }

            if (m_shapesManager != null && m_shapesManager.IsBoardBusy)
            {
                ShowToast("Đợi các ô rơi xong nhé!");
                return;
            }

            User.EnsureLoaded();
            int count = User.data != null ? User.data.GetBoosterCount(type) : 0;
            if (count <= 0)
            {
                m_shapesManager?.CancelBoosterMode();
                Canvas callerCanvas = GetComponentInParent<Canvas>();
                BuyBoosterPopupController.Open(
                    config,
                    onPurchased: RefreshAllSlots,
                    callerCanvas: callerCanvas);
                return;
            }

            if (m_shapesManager == null)
            {
                m_shapesManager = FindFirstObjectByType<ShapesManager>();
            }

            if (m_shapesManager == null)
            {
                return;
            }

            bool activated = m_shapesManager.TryActivateBooster(type, ConsumeBoosterCharge);
            if (!activated)
            {
                ShowToast("Đợi các ô rơi xong nhé!");
            }
        }

        private bool ConsumeBoosterCharge(BoosterType type)
        {
            User.EnsureLoaded();
            if (User.data == null) return false;
            if (!User.data.TryConsumeBooster(type)) return false;

            User.Save();
            return true;
        }

        public void RefreshAllSlots()
        {
            if (m_slots == null) return;

            User.EnsureLoaded();
            int levelNum = GetCurrentLevelNumber();
            BoosterType? activeMode = m_shapesManager != null ? m_shapesManager.ActiveBoosterMode : null;
            bool isBusy = m_shapesManager != null && m_shapesManager.IsBoardBusy;

            for (int i = 0; i < m_slots.Length; i++)
            {
                BoosterSlotView slot = m_slots[i];
                if (slot == null) continue;

                BoosterConfig config = m_database != null ? m_database.GetConfig(slot.BoosterType) : null;
                bool isUnlocked = config != null && config.IsUnlockedAtLevel(levelNum);
                int count = User.data != null ? User.data.GetBoosterCount(slot.BoosterType) : 0;
                bool isTargeting = activeMode.HasValue && activeMode.Value == slot.BoosterType;

                slot.RefreshState(config, isUnlocked, count, isTargeting, isBusy);
            }
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
                .SetLink(gameObject)
                .Append(m_toastCanvasGroup.DOFade(1f, 0.14f))
                .AppendInterval(1.1f)
                .Append(m_toastCanvasGroup.DOFade(0f, 0.25f));
        }

        private static void PlayClickSfx()
        {
            if (SOManager.instance != null)
                SOManager.instance.PlayAudio("Click2");
        }

        public void SetEditorReferences(
            BoosterDatabaseSO database,
            ShapesManager shapesManager,
            BoosterSlotView[] slots,
            GameObject targetingBanner,
            TextMeshProUGUI targetingHintText,
            Button cancelTargetingButton,
            CanvasGroup toastCanvasGroup,
            TextMeshProUGUI toastText)
        {
            m_database = database;
            m_shapesManager = shapesManager;
            m_slots = slots;
            m_targetingBanner = targetingBanner;
            m_targetingHintText = targetingHintText;
            m_cancelTargetingButton = cancelTargetingButton;
            m_toastCanvasGroup = toastCanvasGroup;
            m_toastText = toastText;
        }
    }
}
