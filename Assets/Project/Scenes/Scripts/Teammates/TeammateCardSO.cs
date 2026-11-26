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

        [Header("Detail Info")]
        [SerializeField] private string m_title;
        [SerializeField] private string m_faction;
        [SerializeField, TextArea(3, 6)] private string m_detail;
        [SerializeField, TextArea(2, 4)] private string m_skillDetail;

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
        public string Title => m_title;
        public string Faction => m_faction;
        public string Detail => m_detail;
        public string SkillDetail => m_skillDetail;
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

        public string GetCardTypeDisplayName()
        {
            return m_cardType == TeammateCardType.Attack
                ? "TẤN CÔNG"
                : "HỖ TRỢ / PHÒNG THỦ";
        }

        public string GetDetailText()
        {
            if (!string.IsNullOrWhiteSpace(m_detail))
            {
                return m_detail;
            }
            return !string.IsNullOrWhiteSpace(m_description)
                ? m_description
                : "Chưa có thông tin chi tiết về đồng đội này.";
        }

        public string GetSkillDetailText()
        {
            if (!string.IsNullOrWhiteSpace(m_skillDetail))
            {
                return m_skillDetail;
            }

            return m_cardType == TeammateCardType.Attack
                ? $"Khi ra trận ở hàng công, mỗi đòn tấn công của bạn được cộng thêm +{EffectValue} sát thương trực tiếp lên đối thủ."
                : $"Khi ra trận ở hàng thủ, tự động che chắn và giảm {EffectValue} sát thương từ mỗi đòn tấn công của kẻ địch.";
        }

        public string GetUnlockSourceText()
        {
            if (m_unlockedByDefault)
            {
                return "Mở khóa mặc định ngay từ đầu";
            }
            if (m_storyOnly)
            {
                return "Thẻ độc quyền cốt truyện (nhận miễn phí khi tiến triển kịch bản)";
            }
            if (PriceGold <= 0)
            {
                return "Chiêu mộ miễn phí tại màn hình Đội Hình";
            }
            return $"Chiêu mộ tại màn hình Đội Hình với giá {PriceGold:N0} $";
        }

        public void Configure(
            string id,
            string displayName,
            TeammateCardType cardType,
            Sprite avatar,
            int effectValue,
            int priceGold,
            bool unlockedByDefault,
            string description,
            string title = null,
            string faction = null,
            string detail = null,
            string skillDetail = null)
        {
            m_id = id;
            m_displayName = displayName;
            m_cardType = cardType;
            m_avatar = avatar;
            m_effectValue = Mathf.Max(0, effectValue);
            m_priceGold = Mathf.Max(0, priceGold);
            m_unlockedByDefault = unlockedByDefault;
            m_description = description;
            if (title != null) m_title = title;
            if (faction != null) m_faction = faction;
            if (detail != null) m_detail = detail;
            if (skillDetail != null) m_skillDetail = skillDetail;
        }
    }
}
