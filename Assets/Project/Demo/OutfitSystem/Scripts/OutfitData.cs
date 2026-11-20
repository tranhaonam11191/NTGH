using UnityEngine;
using UnityEngine.U2D.Animation;

namespace NTGH.Demo.OutfitSystem
{
    [CreateAssetMenu(fileName = "OutfitData", menuName = "NTGH/Demo/Outfit Data")]
    public class OutfitData : ScriptableObject
    {
        [Header("Info")]
        [SerializeField] private string m_outfitId;
        [SerializeField] private string m_displayName;
        [SerializeField] private string m_rankTitle;
        [SerializeField] private int m_priceGold;

        [Header("Bonus Stats")]
        [SerializeField] private int m_bonusHealth;
        [SerializeField] private int m_bonusDefence;

        [Header("Visuals")]
        [SerializeField] private SpriteLibraryAsset m_spriteLibraryAsset;
        [SerializeField] private Sprite m_weaponSprite;

        public string OutfitId => m_outfitId;
        public string DisplayName => m_displayName;
        public string RankTitle => m_rankTitle;
        public int PriceGold => m_priceGold;
        public int BonusHealth => m_bonusHealth;
        public int BonusDefence => m_bonusDefence;
        public SpriteLibraryAsset SpriteLibraryAsset => m_spriteLibraryAsset;
        public Sprite WeaponSprite => m_weaponSprite;

        public void Configure(
            string outfitId,
            string displayName,
            string rankTitle,
            int priceGold,
            int bonusHealth,
            int bonusDefence,
            SpriteLibraryAsset spriteLibraryAsset,
            Sprite weaponSprite)
        {
            m_outfitId = outfitId;
            m_displayName = displayName;
            m_rankTitle = rankTitle;
            m_priceGold = priceGold;
            m_bonusHealth = bonusHealth;
            m_bonusDefence = bonusDefence;
            m_spriteLibraryAsset = spriteLibraryAsset;
            m_weaponSprite = weaponSprite;
        }
    }
}
