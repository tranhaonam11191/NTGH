using System;
using System.Collections.Generic;
using DG.Tweening;
using NTGH.Boosters;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// Full-screen dark-shield intro shown when the player reaches a level that unlocks one or more new boosters.
    /// Displays the booster icon(s) in the center with a "Nhận" button. Clicking "Nhận" closes this screen
    /// and reports each booster's screen position so GameBoosterHUD can fly exact-size icons into their HUD slots.
    /// </summary>
    [DisallowMultipleComponent]
    public class BoosterIntroScreenController : MonoBehaviour, IKeyBack
    {
        public const string ScreenResourceName = "BoosterIntroScreen";

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI m_titleText;

        [Header("Center Icon Showcase")]
        [SerializeField] private RectTransform m_iconsContainer;
        [SerializeField] private GameObject m_iconItemTemplate;
        [SerializeField] private TextMeshProUGUI m_descriptionText;

        [Header("Claim CTA")]
        [SerializeField] private Button m_claimButton;
        [SerializeField] private TextMeshProUGUI m_claimButtonLabel;

        private readonly List<BoosterConfig> m_configs = new List<BoosterConfig>();
        private readonly List<GameObject> m_spawnedItems = new List<GameObject>();
        private readonly List<RectTransform> m_spawnedIconRects = new List<RectTransform>();
        private Action<IReadOnlyList<BoosterConfig>, IReadOnlyList<Vector3>> m_onClaim;
        private bool m_isClaiming;
        private Sequence m_introSequence;

        public static void Open(
            IReadOnlyList<BoosterConfig> unlockedBoosters,
            Action<IReadOnlyList<BoosterConfig>, IReadOnlyList<Vector3>> onClaim,
            Canvas callerCanvas = null)
        {
            if (unlockedBoosters == null || unlockedBoosters.Count == 0)
            {
                onClaim?.Invoke(Array.Empty<BoosterConfig>(), Array.Empty<Vector3>());
                return;
            }

            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<BoosterIntroScreenController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: true,
                onScreenLoad: screen => screen.Setup(unlockedBoosters, onClaim),
                hasShield: true,
                shieldAlpha: 0.86f,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_claimButton != null)
            {
                m_claimButton.onClick.AddListener(OnClickClaim);
            }

            if (m_iconItemTemplate != null)
            {
                m_iconItemTemplate.SetActive(false);
            }
        }

        private void OnDisable()
        {
            m_introSequence?.Kill();
            m_introSequence = null;
        }

        private void OnDestroy()
        {
            m_introSequence?.Kill();
            m_introSequence = null;

            if (m_claimButton != null)
            {
                m_claimButton.onClick.RemoveListener(OnClickClaim);
            }
        }

        public void Setup(
            IReadOnlyList<BoosterConfig> unlockedBoosters,
            Action<IReadOnlyList<BoosterConfig>, IReadOnlyList<Vector3>> onClaim)
        {
            m_isClaiming = false;
            m_onClaim = onClaim;
            m_configs.Clear();

            if (unlockedBoosters != null)
            {
                for (int i = 0; i < unlockedBoosters.Count; i++)
                {
                    if (unlockedBoosters[i] != null)
                    {
                        m_configs.Add(unlockedBoosters[i]);
                    }
                }
            }

            if (m_claimButton != null)
            {
                m_claimButton.interactable = true;
            }

            if (m_claimButtonLabel != null)
            {
                m_claimButtonLabel.text = "NHẬN";
            }

            RebuildShowcase();
            PlayIntroPop();
        }

        private void RebuildShowcase()
        {
            for (int i = 0; i < m_spawnedItems.Count; i++)
            {
                if (m_spawnedItems[i] != null)
                {
                    Destroy(m_spawnedItems[i]);
                }
            }
            m_spawnedItems.Clear();
            m_spawnedIconRects.Clear();

            int count = m_configs.Count;
            if (m_titleText != null)
            {
                m_titleText.text = count > 1
                    ? $"MỞ KHÓA {count} BOOSTER MỚI!"
                    : "MỞ KHÓA BOOSTER MỚI!";
            }

            if (m_descriptionText != null)
            {
                if (count == 1)
                {
                    BoosterConfig single = m_configs[0];
                    m_descriptionText.text = $"{single.description}\n<color=#29EBE0>Tặng +{Mathf.Max(0, single.initialFreeCount)} lượt dùng miễn phí!</color>";
                }
                else
                {
                    m_descriptionText.text = "Tất cả booster mới sẽ được mở khóa ngay vào thanh chiến đấu!";
                }
            }

            if (m_iconsContainer == null || m_iconItemTemplate == null)
            {
                return;
            }

            float iconDisplaySize = count == 1 ? 186f : (count == 2 ? 156f : 124f);

            for (int i = 0; i < count; i++)
            {
                BoosterConfig cfg = m_configs[i];
                GameObject itemGo = Instantiate(m_iconItemTemplate, m_iconsContainer);
                itemGo.name = $"IntroItem_{cfg.type}";
                itemGo.SetActive(true);

                RectTransform itemRt = itemGo.GetComponent<RectTransform>();
                if (itemRt != null)
                {
                    itemRt.sizeDelta = new Vector2(iconDisplaySize + 24f, iconDisplaySize + 64f);
                }

                Transform iconTr = itemGo.transform.Find("ImgIcon");
                RectTransform iconRt = iconTr as RectTransform;
                if (iconRt != null)
                {
                    iconRt.sizeDelta = new Vector2(iconDisplaySize, iconDisplaySize);
                    Image img = iconRt.GetComponent<Image>();
                    if (img != null && cfg.icon != null)
                    {
                        img.sprite = cfg.icon;
                    }
                    m_spawnedIconRects.Add(iconRt);
                }
                else
                {
                    m_spawnedIconRects.Add(itemRt);
                }

                Transform nameTr = itemGo.transform.Find("TxtName");
                if (nameTr != null)
                {
                    TextMeshProUGUI nameTmp = nameTr.GetComponent<TextMeshProUGUI>();
                    if (nameTmp != null)
                    {
                        nameTmp.text = cfg.displayName;
                    }
                }

                m_spawnedItems.Add(itemGo);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(m_iconsContainer);
        }

        private void PlayIntroPop()
        {
            m_introSequence?.Kill();
            m_introSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            for (int i = 0; i < m_spawnedItems.Count; i++)
            {
                Transform tr = m_spawnedItems[i].transform;
                tr.localScale = Vector3.zero;
                m_introSequence.Insert(0.06f * i, tr.DOScale(Vector3.one, 0.34f).SetEase(Ease.OutBack));
            }
        }

        public void OnKeyBack()
        {
            OnClickClaim();
        }

        private void OnClickClaim()
        {
            if (m_isClaiming) return;
            m_isClaiming = true;

            if (m_claimButton != null)
            {
                m_claimButton.interactable = false;
            }

            if (SOManager.instance != null)
            {
                SOManager.instance.PlayAudio("Click2");
            }

            // Capture screen-space centers of each displayed booster icon before closing
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
            Camera uiCam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            var startScreenPositions = new List<Vector3>(m_configs.Count);
            for (int i = 0; i < m_configs.Count; i++)
            {
                RectTransform iconRt = i < m_spawnedIconRects.Count ? m_spawnedIconRects[i] : null;
                if (iconRt != null)
                {
                    Vector3 worldCenter = iconRt.TransformPoint(iconRt.rect.center);
                    Vector2 sp = RectTransformUtility.WorldToScreenPoint(uiCam, worldCenter);
                    startScreenPositions.Add(new Vector3(sp.x, sp.y, 0f));
                }
                else
                {
                    startScreenPositions.Add(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
                }
            }

            var configsSnapshot = new List<BoosterConfig>(m_configs);
            var callback = m_onClaim;
            m_onClaim = null;

            m_introSequence?.Kill();
            m_introSequence = null;

            // Close immediately so the intro screen vanishes and the flying icons take over smoothly
            Core.Destroy(this);
            callback?.Invoke(configsSnapshot, startScreenPositions);
        }

        public void SetEditorReferences(
            TextMeshProUGUI titleText,
            RectTransform iconsContainer,
            GameObject iconItemTemplate,
            TextMeshProUGUI descriptionText,
            Button claimButton,
            TextMeshProUGUI claimButtonLabel)
        {
            m_titleText = titleText;
            m_iconsContainer = iconsContainer;
            m_iconItemTemplate = iconItemTemplate;
            m_descriptionText = descriptionText;
            m_claimButton = claimButton;
            m_claimButtonLabel = claimButtonLabel;
        }
    }
}
