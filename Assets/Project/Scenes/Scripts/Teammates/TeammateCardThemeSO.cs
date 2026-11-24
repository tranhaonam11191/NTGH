using UnityEngine;

namespace NTGH.Teammates
{
    [CreateAssetMenu(fileName = "CardTheme", menuName = "NTGH/Teammates/Card Theme")]
    public class TeammateCardThemeSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private TeammateCardType m_cardType = TeammateCardType.Attack;
        [SerializeField] private string m_typeLabel = "CÔNG";

        [Header("Reusable Card Visuals (Separate from Character Avatar)")]
        [SerializeField] private Sprite m_cardBackgroundSprite;
        [SerializeField] private Sprite m_cardBorderSprite;
        [SerializeField] private Color m_backgroundTint = Color.white;
        [SerializeField] private Color m_borderTint = new Color(0.85f, 0.67f, 0.28f, 1f);
        [SerializeField] private Color m_badgeColor = new Color(0.82f, 0.16f, 0.20f, 1f);
        [SerializeField] private Color m_barFillColor = new Color(0.95f, 0.30f, 0.28f, 1f);

        public TeammateCardType CardType => m_cardType;
        public string TypeLabel => m_typeLabel;
        public Sprite CardBackgroundSprite => m_cardBackgroundSprite;
        public Sprite CardBorderSprite => m_cardBorderSprite;
        public Color BackgroundTint => m_backgroundTint;
        public Color BorderTint => m_borderTint;
        public Color BadgeColor => m_badgeColor;
        public Color BarFillColor => m_barFillColor;

        public void Configure(
            TeammateCardType cardType,
            string typeLabel,
            Sprite backgroundSprite,
            Sprite borderSprite,
            Color backgroundTint,
            Color borderTint,
            Color badgeColor,
            Color barFillColor)
        {
            m_cardType = cardType;
            m_typeLabel = typeLabel;
            m_cardBackgroundSprite = backgroundSprite;
            m_cardBorderSprite = borderSprite;
            m_backgroundTint = backgroundTint;
            m_borderTint = borderTint;
            m_badgeColor = badgeColor;
            m_barFillColor = barFillColor;
        }
    }
}
