using UnityEngine;

namespace NTGH.Teammates
{
    [CreateAssetMenu(fileName = "Card_", menuName = "NTGH/Teammates/Teammate Card")]
    public class TeammateCardSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string m_id;
        [SerializeField] private string m_displayName;
        [SerializeField] private TeammateCardType m_cardType = TeammateCardType.Attack;
        [SerializeField, TextArea(2, 3)] private string m_description;

        [Header("Character Portrait (Separate from Card Frame)")]
        [SerializeField] private Sprite m_avatar;

        [Header("Combat Effect")]
        [Tooltip("Attack: +X bonus damage when attacking. Support: -Y incoming enemy damage.")]
        [SerializeField, Min(0)] private int m_effectValue = 10;

        [Header("Shop / Unlock")]
        [SerializeField, Min(0)] private int m_priceGold = 300;
        [SerializeField] private bool m_unlockedByDefault;
        [Tooltip("Thẻ độc quyền cốt truyện: ẩn khỏi shop khi chưa mở khóa, chỉ nhận qua kịch bản thoại.")]
        [SerializeField] private bool m_storyOnly;

        public string Id => m_id;
        public string DisplayName => m_displayName;
        public TeammateCardType CardType => m_cardType;
        public string Description => m_description;
        public Sprite Avatar => m_avatar;
        public int EffectValue => Mathf.Max(0, m_effectValue);
        public int PriceGold => Mathf.Max(0, m_priceGold);
        public bool UnlockedByDefault => m_unlockedByDefault;
        public bool StoryOnly => m_storyOnly;

        public string GetStatShortText()
        {
            return m_cardType == TeammateCardType.Attack
                ? $"+{EffectValue} ST"
                : $"-{EffectValue} ST";
        }

        public string GetEffectSummary()
        {
            return m_cardType == TeammateCardType.Attack
                ? $"Tấn công +{EffectValue} sát thương"
                : $"Giảm {EffectValue} sát thương địch";
        }

        public void Configure(
            string id,
            string displayName,
            TeammateCardType cardType,
            Sprite avatar,
            int effectValue,
            int priceGold,
            bool unlockedByDefault,
            string description)
        {
            m_id = id;
            m_displayName = displayName;
            m_cardType = cardType;
            m_avatar = avatar;
            m_effectValue = Mathf.Max(0, effectValue);
            m_priceGold = Mathf.Max(0, priceGold);
            m_unlockedByDefault = unlockedByDefault;
            m_description = description;
        }
    }
}
