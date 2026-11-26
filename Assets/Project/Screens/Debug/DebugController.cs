using System;
using NTGH.UI;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug screen controller (modeled after CoffeeMania's DebugController, adapted for NTGH SS.UI).
/// Provides quick actions for +Gold, +Heart (max 5), and jumping to a level resolved from ArcsData.
/// </summary>
[DisallowMultipleComponent]
public class DebugController : MonoBehaviour, IKeyBack
{
    public const string NAME = "Debug";

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI m_TitleText;
    [SerializeField] private Button m_CloseButton;

    [Header("Currency (+ Gold)")]
    [SerializeField] private TextMeshProUGUI m_GoldText;
    [SerializeField] private Button m_AddGoldButton;
    [SerializeField] private int m_AddGoldAmount = 1000;

    [Header("Hearts (+ Heart, max 5)")]
    [SerializeField] private TextMeshProUGUI m_HeartText;
    [SerializeField] private Button m_AddHeartButton;

    [Header("Level Selector (ArcsData)")]
    [SerializeField] private TextMeshProUGUI m_CurrentLevelText;
    [SerializeField] private TMP_InputField m_LevelInputField;
    [SerializeField] private Button m_GoToLevelButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI m_StatusText;

    private string m_TestLevelInput;
    private Action m_onClosed;
    private bool m_isClosing;
    private bool m_hasNotifiedClosed;

    public static bool IsDebugOpened { get; private set; }

    public static void Open(Action onClosed = null, Canvas callerCanvas = null)
    {
        NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

        Core.Add<DebugController>(
            NAME,
            showAnimation: "ScaleShow",
            hideAnimation: "ScaleHide",
            useExistingScreen: true,
            onScreenLoad: popup => popup.Setup(onClosed),
            hasShield: true,
            shieldAlpha: 0.76f,
            hideTopScreen: false);
    }

    private void Awake()
    {
        if (m_CloseButton != null)
            m_CloseButton.onClick.AddListener(OnClickClose);

        if (m_AddGoldButton != null)
            m_AddGoldButton.onClick.AddListener(OnAddGoldButtonTap);

        if (m_AddHeartButton != null)
            m_AddHeartButton.onClick.AddListener(OnAddHeartButtonTap);

        if (m_GoToLevelButton != null)
            m_GoToLevelButton.onClick.AddListener(OnGoToLevelButtonTap);

        if (m_LevelInputField != null)
            m_LevelInputField.onValueChanged.AddListener(OnInputLevel);
    }

    private void OnEnable()
    {
        IsDebugOpened = true;
        m_isClosing = false;
        User.OnGoldChanged += HandleGoldChanged;
        User.OnHeartsChanged += HandleHeartsChanged;
    }

    private void OnDisable()
    {
        User.OnGoldChanged -= HandleGoldChanged;
        User.OnHeartsChanged -= HandleHeartsChanged;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDestroy()
    {
        IsDebugOpened = false;

        if (m_CloseButton != null)
            m_CloseButton.onClick.RemoveListener(OnClickClose);

        if (m_AddGoldButton != null)
            m_AddGoldButton.onClick.RemoveListener(OnAddGoldButtonTap);

        if (m_AddHeartButton != null)
            m_AddHeartButton.onClick.RemoveListener(OnAddHeartButtonTap);

        if (m_GoToLevelButton != null)
            m_GoToLevelButton.onClick.RemoveListener(OnGoToLevelButtonTap);

        if (m_LevelInputField != null)
            m_LevelInputField.onValueChanged.RemoveListener(OnInputLevel);

        NotifyClosedOnce();
    }

    public void Setup(Action onClosed = null)
    {
        m_onClosed = onClosed;
        m_isClosing = false;
        m_hasNotifiedClosed = false;
        IsDebugOpened = true;

        if (m_TitleText != null)
            m_TitleText.text = "DEBUG";

        SetStatus(string.Empty);
        Refresh();
    }

    public void Refresh()
    {
        User.EnsureLoaded();
        if (User.data != null)
        {
            User.data.RefreshHearts();

            if (m_GoldText != null)
                m_GoldText.text = $"{User.data.gold:N0} Gold";

            if (m_HeartText != null)
                m_HeartText.text = $"{User.data.hearts} / {UserData.MAX_HEARTS} Tim";

            if (m_AddHeartButton != null)
                m_AddHeartButton.interactable = User.data.hearts < UserData.MAX_HEARTS;
        }

        RefreshLevelInfo();
    }

    private void RefreshLevelInfo()
    {
        if (m_CurrentLevelText == null)
            return;

        ArcsData arcsData = SOManager.instance != null ? SOManager.instance.ArcsData : null;
        if (arcsData == null)
        {
            m_CurrentLevelText.text = "ArcsData chưa được gán";
            return;
        }

        int totalLevels = arcsData.GetTotalLevelCount();
        int currentIndex = SOManager.instance.CurrentLevelIndex;

        if (arcsData.TryGetArcAndLevelIndex(currentIndex, out int arcIndex, out int levelInArc, out LevelData levelData))
        {
            string displayName = !string.IsNullOrEmpty(levelData.displayName) ? $" - {levelData.displayName}" : string.Empty;
            m_CurrentLevelText.text = $"Hiện tại: Lv {currentIndex + 1}/{totalLevels} (Arc {arcIndex + 1} - Lv {levelInArc + 1}){displayName}";
        }
        else
        {
            m_CurrentLevelText.text = $"Tổng số level: {totalLevels}";
        }
    }

    private void HandleGoldChanged(int _)
    {
        Refresh();
    }

    private void HandleHeartsChanged(int _)
    {
        Refresh();
    }

    public void OnAddGoldButtonTap()
    {
        PlayClickSfx();
        User.EnsureLoaded();
        if (User.data == null)
            return;

        int amount = Mathf.Max(1, m_AddGoldAmount);
        User.data.AddGold(amount);
        User.Save();

        if (HomeController.instance != null)
            HomeController.instance.SetData();

        SetStatus($"+{amount:N0} Gold (Hiện có: {User.data.gold:N0})");
        Refresh();
    }

    public void OnAddHeartButtonTap()
    {
        PlayClickSfx();
        User.EnsureLoaded();
        if (User.data == null)
            return;

        User.data.RefreshHearts();
        if (User.data.hearts >= UserData.MAX_HEARTS)
        {
            SetStatus($"Tim đã đạt tối đa ({UserData.MAX_HEARTS}/{UserData.MAX_HEARTS})");
            Refresh();
            return;
        }

        User.data.AddHearts(1);
        User.Save();

        if (HomeController.instance != null)
            HomeController.instance.SetData();

        SetStatus($"+1 Tim ({User.data.hearts}/{UserData.MAX_HEARTS})");
        Refresh();
    }

    public void OnInputLevel(string str)
    {
        m_TestLevelInput = str;
    }

    public void OnGoToLevelButtonTap()
    {
        if (m_isClosing)
            return;

        PlayClickSfx();

        string rawInput = m_LevelInputField != null ? m_LevelInputField.text : m_TestLevelInput;
        ArcsData arcsData = SOManager.instance != null ? SOManager.instance.ArcsData : null;

        if (SOManager.instance == null || arcsData == null)
        {
            SetStatus("Không tìm thấy SOManager / ArcsData!", isError: true);
            return;
        }

        int totalLevels = arcsData.GetTotalLevelCount();
        if (!arcsData.TryParseLevelInput(rawInput, out int flatIndex, out int arcIndex, out int levelInArc, out LevelData targetLevel))
        {
            SetStatus($"Level không hợp lệ! Nhập 1..{totalLevels} hoặc Arc-Lv (VD: 1-3)", isError: true);
            return;
        }

        if (targetLevel == null || !targetLevel.HasCombatants)
        {
            SetStatus($"Lv {flatIndex + 1} (Arc {arcIndex + 1} - Lv {levelInArc + 1}) thiếu dữ liệu nhân vật!", isError: true);
            return;
        }

        SetStatus($"Đang tải Lv {flatIndex + 1} (Arc {arcIndex + 1} - Lv {levelInArc + 1})...");
        m_isClosing = true;
        IsDebugOpened = false;
        NotifyClosedOnce();

        SOManager.instance.ForceLoadLevel(flatIndex);
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
        IsDebugOpened = false;
        PlayClickSfx();
        Core.Close(this, NotifyClosedOnce);
    }

    private void SetStatus(string message, bool isError = false)
    {
        if (m_StatusText == null)
            return;

        m_StatusText.text = message ?? string.Empty;
        m_StatusText.color = isError
            ? new Color(1f, 0.36f, 0.46f, 1f)
            : new Color(0.16f, 0.92f, 0.88f, 1f);
    }

    private void NotifyClosedOnce()
    {
        if (m_hasNotifiedClosed)
            return;

        m_hasNotifiedClosed = true;
        Action callback = m_onClosed;
        m_onClosed = null;
        callback?.Invoke();
    }

    private static void PlayClickSfx()
    {
        if (SOManager.instance != null)
            SOManager.instance.PlayAudio("Click2");
    }

    public void SetEditorReferences(
        TextMeshProUGUI titleText,
        Button closeButton,
        TextMeshProUGUI goldText,
        Button addGoldButton,
        TextMeshProUGUI heartText,
        Button addHeartButton,
        TextMeshProUGUI currentLevelText,
        TMP_InputField levelInputField,
        Button goToLevelButton,
        TextMeshProUGUI statusText)
    {
        m_TitleText = titleText;
        m_CloseButton = closeButton;
        m_GoldText = goldText;
        m_AddGoldButton = addGoldButton;
        m_HeartText = heartText;
        m_AddHeartButton = addHeartButton;
        m_CurrentLevelText = currentLevelText;
        m_LevelInputField = levelInputField;
        m_GoToLevelButton = goToLevelButton;
        m_StatusText = statusText;
    }
}
