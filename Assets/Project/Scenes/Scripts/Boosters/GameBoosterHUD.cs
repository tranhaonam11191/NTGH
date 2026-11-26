using System.Collections;
using System.Collections.Generic;
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

        private readonly List<GameObject> m_flyingIcons = new List<GameObject>();
        private readonly List<Tween> m_activeIntroTweens = new List<Tween>();
        private Sequence m_toastSequence;
        private bool m_lastBoardBusy;
        private bool m_hasIntroBoardBlock;
        private bool m_introSequenceRunning;

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
            CleanupIntroAnimations();
            ReleaseIntroBoardBlock();

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

            if (m_boosters != null)
            {
                for (int i = 0; i < m_boosters.Length; i++)
                {
                    if (m_boosters[i] == null) continue;
                    m_boosters[i].Bind(HandleBoosterClicked);
                }
            }

            RefreshAllBoosters();
            StartCoroutine(CheckAndShowBoosterIntroCoroutine());
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

        private IEnumerator CheckAndShowBoosterIntroCoroutine()
        {
            // Wait 1 frame so GameController.Setup runs, then wait until opening story dialogue finishes.
            yield return null;
            while (GameController.instance != null && GameController.instance.BlocksBoard)
            {
                yield return null;
            }

            if (m_database == null)
            {
                m_database = BoosterDatabaseSO.LoadDefault();
            }

            User.EnsureLoaded();
            if (User.data == null || m_database == null)
            {
                yield break;
            }

            int levelNum = GetCurrentLevelNumber();
            List<BoosterConfig> pending = User.data.GetPendingUnlockBoosters(m_database, levelNum);
            if (pending == null || pending.Count == 0)
            {
                yield break;
            }

            m_introSequenceRunning = true;
            AcquireIntroBoardBlock();

            Canvas callerCanvas = GetComponentInParent<Canvas>();
            BoosterIntroScreenController.Open(
                pending,
                HandleBoosterIntroClaimed,
                callerCanvas: callerCanvas);
        }

        private void HandleBoosterIntroClaimed(
            IReadOnlyList<BoosterConfig> configs,
            IReadOnlyList<Vector3> startScreenPositions)
        {
            if (configs == null || configs.Count == 0)
            {
                FinishIntroSequence();
                return;
            }

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
            RectTransform canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
            Camera uiCam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            int remaining = configs.Count;

            for (int i = 0; i < configs.Count; i++)
            {
                BoosterConfig cfg = configs[i];
                BoosterBase targetSlot = FindBoosterSlot(cfg.type);

                if (targetSlot == null || canvasRect == null)
                {
                    User.EnsureLoaded();
                    User.data?.ClaimBoosterUnlock(cfg);
                    User.Save();
                    remaining--;
                    if (remaining <= 0) FinishIntroSequence();
                    continue;
                }

                Vector2 startScreen = i < startScreenPositions.Count
                    ? new Vector2(startScreenPositions[i].x, startScreenPositions[i].y)
                    : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

                RectTransform targetIconRt = targetSlot.IconRectTransform;
                Vector3 targetWorldCenter = targetIconRt.TransformPoint(targetIconRt.rect.center);
                Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(uiCam, targetWorldCenter);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreen, uiCam, out Vector2 startLocal);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreen, uiCam, out Vector2 endLocal);

                // Spawn flying booster icon with the EXACT size of the in-game booster UI icon
                Vector2 inGameIconSize = targetIconRt.rect.size;
                if (inGameIconSize.x <= 1f || inGameIconSize.y <= 1f)
                {
                    inGameIconSize = new Vector2(96f, 96f);
                }

                GameObject flyGo = new GameObject($"FlyingBooster_{cfg.type}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                flyGo.layer = canvasRect.gameObject.layer;
                RectTransform flyRt = flyGo.GetComponent<RectTransform>();
                flyRt.SetParent(canvasRect, false);
                flyRt.SetAsLastSibling();
                flyRt.anchorMin = new Vector2(0.5f, 0.5f);
                flyRt.anchorMax = new Vector2(0.5f, 0.5f);
                flyRt.pivot = new Vector2(0.5f, 0.5f);
                flyRt.sizeDelta = inGameIconSize;
                flyRt.anchoredPosition = startLocal;
                flyRt.localScale = Vector3.one * 0.85f;

                Image flyImg = flyGo.GetComponent<Image>();
                flyImg.sprite = cfg.icon;
                flyImg.preserveAspect = true;
                flyImg.raycastTarget = false;

                m_flyingIcons.Add(flyGo);

                // Curved Bezier arc toward the target slot
                Vector2 midLocal = (startLocal + endLocal) * 0.5f + new Vector2(0f, 110f);
                float delay = 0.08f * i;
                const float flyDuration = 0.52f;

                Sequence flySeq = DOTween.Sequence().SetUpdate(true).SetLink(flyGo);
                if (delay > 0f)
                {
                    flySeq.AppendInterval(delay);
                }

                flySeq.Append(flyRt.DOScale(Vector3.one, 0.12f).SetEase(Ease.OutBack));
                flySeq.Join(DOVirtual.Float(0f, 1f, flyDuration, t =>
                {
                    if (flyRt == null) return;
                    float omt = 1f - t;
                    Vector2 pos = (omt * omt * startLocal) + (2f * omt * t * midLocal) + (t * t * endLocal);
                    flyRt.anchoredPosition = pos;
                }).SetEase(Ease.InOutCubic));

                flySeq.OnComplete(() =>
                {
                    if (flyGo != null)
                    {
                        m_flyingIcons.Remove(flyGo);
                        Destroy(flyGo);
                    }

                    User.EnsureLoaded();
                    if (User.data != null)
                    {
                        User.data.ClaimBoosterUnlock(cfg);
                        User.Save();
                    }

                    int newCount = User.data != null ? User.data.GetBoosterCount(cfg.type) : cfg.initialFreeCount;
                    targetSlot.PlayUnlockAnimation(cfg, newCount, () =>
                    {
                        remaining--;
                        if (remaining <= 0)
                        {
                            FinishIntroSequence();
                        }
                    });
                });

                m_activeIntroTweens.Add(flySeq);
            }
        }

        private void FinishIntroSequence()
        {
            m_introSequenceRunning = false;
            m_activeIntroTweens.Clear();
            ReleaseIntroBoardBlock();
            RefreshAllBoosters();
        }

        private BoosterBase FindBoosterSlot(BoosterType type)
        {
            if (m_boosters == null) return null;
            for (int i = 0; i < m_boosters.Length; i++)
            {
                if (m_boosters[i] != null && m_boosters[i].BoosterType == type)
                {
                    return m_boosters[i];
                }
            }
            return null;
        }

        private void AcquireIntroBoardBlock()
        {
            if (m_hasIntroBoardBlock || GameController.instance == null) return;
            GameController.instance.PushBoosterPopupBlock();
            m_hasIntroBoardBlock = true;
        }

        private void ReleaseIntroBoardBlock()
        {
            if (!m_hasIntroBoardBlock) return;
            m_hasIntroBoardBlock = false;
            GameController.instance?.PopBoosterPopupBlock();
        }

        private void CleanupIntroAnimations()
        {
            for (int i = 0; i < m_activeIntroTweens.Count; i++)
            {
                m_activeIntroTweens[i]?.Kill();
            }
            m_activeIntroTweens.Clear();

            for (int i = 0; i < m_flyingIcons.Count; i++)
            {
                if (m_flyingIcons[i] != null)
                {
                    Destroy(m_flyingIcons[i]);
                }
            }
            m_flyingIcons.Clear();
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
            if (!m_introSequenceRunning)
            {
                RefreshAllBoosters();
            }
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
            if (booster == null || m_introSequenceRunning || (GameController.instance != null && GameController.instance.BlocksBoard))
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

            User.EnsureLoaded();
            int levelNum = GetCurrentLevelNumber();
            bool isUnlocked = config.IsUnlockedAtLevel(levelNum) &&
                (User.data == null || User.data.HasClaimedBoosterUnlock(type));
            if (!isUnlocked)
            {
                ShowToast($"Mở khóa ở Màn {config.unlockLevel}!");
                return;
            }

            if (m_shapesManager != null && m_shapesManager.IsBoardBusy)
            {
                ShowToast("Đợi các ô rơi xong nhé!");
                return;
            }

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
                bool isUnlocked = config != null &&
                    config.IsUnlockedAtLevel(levelNum) &&
                    (User.data == null || User.data.HasClaimedBoosterUnlock(booster.BoosterType));
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
