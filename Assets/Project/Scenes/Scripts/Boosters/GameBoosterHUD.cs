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

        [Header("Booster Prefab Instances (Concrete BoosterBase Subclasses)")]
        [SerializeField] private BoosterBase[] m_boosters;

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

            if (m_boosters == null || m_boosters.Length == 0)
            {
                m_boosters = GetComponentsInChildren<BoosterBase>(true);
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

            if (m_boosters != null)
            {
                for (int i = 0; i < m_boosters.Length; i++)
                {
                    if (m_boosters[i] == null) continue;
                    m_boosters[i].Bind(HandleBoosterClicked);
                }
            }

            RefreshAllBoosters();
        }

        private void Update()
        {
            if (m_shapesManager == null) return;
            bool busy = m_shapesManager.IsBoardBusy;
            if (busy != m_lastBoardBusy)
            {
                m_lastBoardBusy = busy;
                RefreshAllBoosters();
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
            RefreshAllBoosters();
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

            RefreshAllBoosters();
        }

        private void HandleBoosterExecuted(BoosterType _)
        {
            RefreshAllBoosters();
        }

        private void OnClickCancelTargeting()
        {
            PlayClickSfx();
            m_shapesManager?.CancelBoosterMode();
        }

        private void HandleBoosterClicked(BoosterBase booster)
        {
            if (booster == null || (GameController.instance != null && GameController.instance.BlocksBoard))
            {
                return;
            }

            PlayClickSfx();

            if (m_database == null)
            {
                m_database = BoosterDatabaseSO.LoadDefault();
            }

            BoosterType type = booster.BoosterType;
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
                    onPurchased: RefreshAllBoosters,
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

            bool activated = m_shapesManager.TryActivateBooster(booster, ConsumeBoosterCharge);
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

        public void RefreshAllBoosters()
        {
            if (m_boosters == null) return;

            User.EnsureLoaded();
            int levelNum = GetCurrentLevelNumber();
            BoosterBase activeBooster = m_shapesManager != null ? m_shapesManager.ActiveBooster : null;
            bool isBusy = m_shapesManager != null && m_shapesManager.IsBoardBusy;

            for (int i = 0; i < m_boosters.Length; i++)
            {
                BoosterBase booster = m_boosters[i];
                if (booster == null) continue;

                BoosterConfig config = m_database != null ? m_database.GetConfig(booster.BoosterType) : null;
                bool isUnlocked = config != null && config.IsUnlockedAtLevel(levelNum);
                int count = User.data != null ? User.data.GetBoosterCount(booster.BoosterType) : 0;
                bool isTargeting = activeBooster == booster;

                booster.RefreshState(config, isUnlocked, count, isTargeting, isBusy);
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
            BoosterBase[] boosters,
            GameObject targetingBanner,
            TextMeshProUGUI targetingHintText,
            Button cancelTargetingButton,
            CanvasGroup toastCanvasGroup,
            TextMeshProUGUI toastText)
        {
            m_database = database;
            m_shapesManager = shapesManager;
            m_boosters = boosters;
            m_targetingBanner = targetingBanner;
            m_targetingHintText = targetingHintText;
            m_cancelTargetingButton = cancelTargetingButton;
            m_toastCanvasGroup = toastCanvasGroup;
            m_toastText = toastText;
        }
    }
}
