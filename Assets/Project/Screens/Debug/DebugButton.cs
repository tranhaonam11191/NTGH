using UnityEngine;

/// <summary>
/// Editor-only Debug entry button (modeled after CoffeeMania's DebugButton).
/// Visible exclusively in the Unity Editor; hidden in all player builds.
/// </summary>
public class DebugButton : MonoBehaviour
{
    [SerializeField] private GameObject m_DebugButton;

    private void Awake()
    {
        GameObject target = m_DebugButton != null ? m_DebugButton : gameObject;
        if (target != null)
        {
#if UNITY_EDITOR
            target.SetActive(true);
#else
            target.SetActive(false);
#endif
        }
    }

    public void OnClickDebugButton()
    {
#if UNITY_EDITOR
        if (DebugController.IsDebugOpened)
            return;

        if (SOManager.instance != null)
            SOManager.instance.PlayAudio("Click2");

        GameController gc = GameController.instance;
        Canvas callerCanvas = gc != null ? gc.BattleCanvas : GetComponentInParent<Canvas>();

        if (gc != null)
        {
            gc.PushDebugPopupBlock();
        }

        DebugController.Open(
            onClosed: () =>
            {
                if (GameController.instance != null)
                {
                    GameController.instance.PopDebugPopupBlock();
                }
            },
            callerCanvas: callerCanvas);
#endif
    }

    public void SetEditorReferences(GameObject debugButtonObject)
    {
        m_DebugButton = debugButtonObject;
    }
}
