using System;
using System.Collections.Generic;
using DG.Tweening;
using NTGH.Teammates;
using SS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NTGH.UI
{
    /// <summary>
    /// SS.UI screen displayed when the player receives 1-3 free Teammate Cards during a story conversation.
    /// Reuses TeammateCardView and adapts layout for 1, 2, or 3 cards.
    /// </summary>
    [DisallowMultipleComponent]
    public class TeammateCardRewardController : MonoBehaviour, IKeyBack
    {
        public const string ScreenResourceName = "TeammateCardRewardScreen";
        public const int MaxCardsPerReveal = ConversationData.MaxRewardCardsPerLine;

        [Header("Header")]
        [SerializeField] private RectTransform m_panelRoot;
        [SerializeField] private TextMeshProUGUI m_txtTitle;
        [SerializeField] private TextMeshProUGUI m_txtSubtitle;

        [Header("Cards (Max 3)")]
        [SerializeField] private HorizontalLayoutGroup m_cardsContainer;
        [SerializeField] private RectTransform[] m_cardSlots = new RectTransform[MaxCardsPerReveal];
        [SerializeField] private TeammateCardView[] m_cardViews = new TeammateCardView[MaxCardsPerReveal];
        [SerializeField] private TextMeshProUGUI[] m_cardEffectTexts = new TextMeshProUGUI[MaxCardsPerReveal];
        [SerializeField] private TextMeshProUGUI m_txtSingleDescription;

        [Header("Claim Action")]
        [SerializeField] private Button m_btnClaim;
        [SerializeField] private TextMeshProUGUI m_txtClaimLabel;
        [SerializeField] private CanvasGroup m_claimButtonGroup;

        private Action m_onClaimed;
        private Sequence m_revealSequence;
        private bool m_canClaim;
        private bool m_isClosing;

        public static void Open(
            TeammateCardDatabaseSO database,
            IReadOnlyList<TeammateCardSO> cards,
            Action onClaimed = null,
            Canvas callerCanvas = null)
        {
            if (cards == null || cards.Count == 0)
            {
                onClaimed?.Invoke();
                return;
            }

            NeonPopupBase.EnsureSSCanvasAbove(callerCanvas);

            Core.Add<TeammateCardRewardController>(
                ScreenResourceName,
                showAnimation: "ScaleShow",
                hideAnimation: "ScaleHide",
                useExistingScreen: false,
                onScreenLoad: screen => screen.Setup(database, cards, onClaimed),
                hasShield: true,
                shieldAlpha: 0.78f,
                hideTopScreen: false);
        }

        private void Awake()
        {
            if (m_btnClaim != null)
            {
                m_btnClaim.onClick.AddListener(OnClickClaim);
            }
        }

        private void OnDestroy()
        {
            m_revealSequence?.Kill();
            m_revealSequence = null;

            if (m_btnClaim != null)
            {
                m_btnClaim.onClick.RemoveListener(OnClickClaim);
            }
        }

        public void Setup(
            TeammateCardDatabaseSO database,
            IReadOnlyList<TeammateCardSO> cards,
            Action onClaimed = null)
        {
            m_onClaimed = onClaimed;
            m_isClosing = false;
            m_canClaim = false;

            int count = cards != null ? Mathf.Min(cards.Count, MaxCardsPerReveal) : 0;
            if (count <= 0)
            {
                CloseAndNotify();
                return;
            }

            if (m_txtTitle != null)
            {
                m_txtTitle.text = count == 1 ? "CHIÊU MỘ ĐỒNG ĐỘI MỚI!" : "ĐỒNG ĐỘI MỚI GIA NHẬP!";
            }

            if (m_txtSubtitle != null)
            {
                m_txtSubtitle.text = count == 1
                    ? "Nhận miễn phí theo cốt truyện"
                    : $"Nhận miễn phí {count} thẻ đồng đội theo cốt truyện";
            }

            if (m_txtClaimLabel != null)
            {
                m_txtClaimLabel.text = "NHẬN THẺ";
            }

            ApplyAdaptiveLayout(count);

            for (int i = 0; i < m_cardSlots.Length; i++)
            {
                bool active = i < count && cards[i] != null;
                if (m_cardSlots[i] != null)
                {
                    m_cardSlots[i].gameObject.SetActive(active);
                }

                if (!active) continue;

                var card = cards[i];
                var theme = database != null ? database.GetTheme(card.CardType) : null;

                if (m_cardViews != null && i < m_cardViews.Length && m_cardViews[i] != null)
                {
                    m_cardViews[i].Bind(card, theme);
                }

                if (m_cardEffectTexts != null && i < m_cardEffectTexts.Length && m_cardEffectTexts[i] != null)
                {
                    m_cardEffectTexts[i].text = card.GetEffectSummary();
                }
            }

            if (m_txtSingleDescription != null)
            {
                bool showDesc = count == 1 && cards[0] != null && !string.IsNullOrWhiteSpace(cards[0].Description);
                m_txtSingleDescription.gameObject.SetActive(showDesc);
                if (showDesc)
                {
                    m_txtSingleDescription.text = cards[0].Description;
                }
            }

            PlayRevealAnimation(count);
        }

        private void ApplyAdaptiveLayout(int count)
        {
            float slotScale;
            float spacing;

            if (count == 1)
            {
                slotScale = 1.18f;
                spacing = 0f;
            }
            else if (count == 2)
            {
                slotScale = 1.0f;
                spacing = 32f;
            }
            else
            {
                slotScale = 0.88f;
                spacing = 12f;
            }

            if (m_cardsContainer != null)
            {
                m_cardsContainer.spacing = spacing;
            }

            for (int i = 0; i < m_cardSlots.Length; i++)
            {
                if (m_cardSlots[i] != null)
                {
                    m_cardSlots[i].localScale = Vector3.one * slotScale;
                }
            }
        }

        private void PlayRevealAnimation(int count)
        {
            m_revealSequence?.Kill();

            if (m_btnClaim != null) m_btnClaim.interactable = false;
            if (m_claimButtonGroup != null) m_claimButtonGroup.alpha = 0f;

            float targetScale = count == 1 ? 1.18f : (count == 2 ? 1.0f : 0.88f);

            m_revealSequence = DOTween.Sequence().SetUpdate(true);

            for (int i = 0; i < count; i++)
            {
                if (m_cardSlots == null || i >= m_cardSlots.Length || m_cardSlots[i] == null) continue;

                var slot = m_cardSlots[i];
                slot.localScale = Vector3.zero;

                var cg = slot.GetComponent<CanvasGroup>();
                if (cg == null) cg = slot.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;

                float startTime = 0.12f + i * 0.14f;
                m_revealSequence.Insert(startTime, slot.DOScale(targetScale, 0.28f).SetEase(Ease.OutBack));
                m_revealSequence.Insert(startTime, cg.DOFade(1f, 0.20f));
                m_revealSequence.InsertCallback(startTime, () =>
                {
                    if (SOManager.instance != null)
                    {
                        SOManager.instance.PlayAudio("Click2");
                    }
                });
            }

            if (m_claimButtonGroup != null)
            {
                m_revealSequence.Append(m_claimButtonGroup.DOFade(1f, 0.20f));
            }

            m_revealSequence.OnComplete(() =>
            {
                m_canClaim = true;
                if (m_btnClaim != null) m_btnClaim.interactable = true;
            });
        }

        private void SkipRevealToEnd()
        {
            if (m_revealSequence != null && m_revealSequence.IsActive() && !m_revealSequence.IsComplete())
            {
                m_revealSequence.Complete(withCallbacks: true);
            }
            m_canClaim = true;
            if (m_btnClaim != null) m_btnClaim.interactable = true;
            if (m_claimButtonGroup != null) m_claimButtonGroup.alpha = 1f;
        }

        private void Update()
        {
            if (m_isClosing || m_canClaim) return;

            if (Input.GetMouseButtonDown(0))
            {
                SkipRevealToEnd();
            }
        }

        public void OnKeyBack()
        {
            if (m_isClosing) return;

            if (!m_canClaim)
            {
                SkipRevealToEnd();
                return;
            }

            OnClickClaim();
        }

        public void OnClickClaim()
        {
            if (m_isClosing || !m_canClaim) return;

            if (SOManager.instance != null)
            {
                SOManager.instance.PlayAudio("Click2");
            }

            CloseAndNotify();
        }

        private void CloseAndNotify()
        {
            if (m_isClosing) return;
            m_isClosing = true;

            var callback = m_onClaimed;
            m_onClaimed = null;
            Core.Close(this, () => callback?.Invoke());
        }

        public void SetEditorReferences(
            RectTransform panelRoot,
            TextMeshProUGUI txtTitle,
            TextMeshProUGUI txtSubtitle,
            HorizontalLayoutGroup cardsContainer,
            RectTransform[] cardSlots,
            TeammateCardView[] cardViews,
            TextMeshProUGUI[] cardEffectTexts,
            TextMeshProUGUI txtSingleDescription,
            Button btnClaim,
            TextMeshProUGUI txtClaimLabel,
            CanvasGroup claimButtonGroup)
        {
            m_panelRoot = panelRoot;
            m_txtTitle = txtTitle;
            m_txtSubtitle = txtSubtitle;
            m_cardsContainer = cardsContainer;
            m_cardSlots = cardSlots;
            m_cardViews = cardViews;
            m_cardEffectTexts = cardEffectTexts;
            m_txtSingleDescription = txtSingleDescription;
            m_btnClaim = btnClaim;
            m_txtClaimLabel = txtClaimLabel;
            m_claimButtonGroup = claimButtonGroup;
        }
    }
}
