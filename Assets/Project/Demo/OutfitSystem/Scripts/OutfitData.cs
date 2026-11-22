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
        [SerializeField] private CustomizeUnlockMethod m_unlockMethod = CustomizeUnlockMethod.Gold;
        [SerializeField] private int m_adsRequired = 1;

        [Header("Bonus Stats")]
        [SerializeField] private int m_bonusHealth;
        [SerializeField] private int m_bonusDefence;

        [Header("Visuals")]
        [SerializeField] private SpriteLibraryAsset m_spriteLibraryAsset;
        [SerializeField] private Sprite m_weaponSprite;
        [SerializeField] private Sprite m_previewSprite;

        public string OutfitId => m_outfitId;
        public string DisplayName => m_displayName;
        public string RankTitle => m_rankTitle;
        public int PriceGold => m_priceGold;
        public CustomizeUnlockMethod UnlockMethod => m_unlockMethod;
        public int AdsRequired => Mathf.Max(1, m_adsRequired);
        public int BonusHealth => m_bonusHealth;
        public int BonusDefence => m_bonusDefence;
        public SpriteLibraryAsset SpriteLibraryAsset => m_spriteLibraryAsset;
        public Sprite WeaponSprite => m_weaponSprite;
        public Sprite PreviewSprite => m_previewSprite;

        public void ConfigureUnlock(CustomizeUnlockMethod unlockMethod, int priceGold, int adsRequired, Sprite previewSprite)
        {
            m_unlockMethod = unlockMethod;
            m_priceGold = Mathf.Max(0, priceGold);
            m_adsRequired = Mathf.Max(1, adsRequired);
            if (previewSprite != null)
            {
                m_previewSprite = previewSprite;
            }
        }

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
