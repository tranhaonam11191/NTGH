using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace NTGH.Demo.OutfitSystem
{
    [DefaultExecutionOrder(-20)]
    public class CharacterOutfitController : MonoBehaviour
    {
        public const string DefaultLabel = "Default";

        [Header("2D Animation Components")]
        [SerializeField] private SpriteLibrary m_spriteLibrary;
        [SerializeField] private List<SpriteResolver> m_resolvers = new List<SpriteResolver>();
        [SerializeField] private List<SpriteSkin> m_spriteSkins = new List<SpriteSkin>();

        [Header("Rigid Attachment Slot (Mounted on Bone)")]
        [SerializeField] private SpriteRenderer m_weaponRenderer;

        [Header("Available Outfits")]
        [SerializeField] private List<OutfitData> m_outfits = new List<OutfitData>();
        [SerializeField] private SpriteLibraryAsset m_mixAndMatchLibrary;

        private int m_currentOutfitIndex;
        private readonly Dictionary<string, SpriteResolver> m_resolverByCategory = new Dictionary<string, SpriteResolver>();

        public OutfitData CurrentOutfit =>
            m_outfits != null && m_currentOutfitIndex >= 0 && m_currentOutfitIndex < m_outfits.Count
                ? m_outfits[m_currentOutfitIndex]
                : null;

        public int CurrentOutfitIndex => m_currentOutfitIndex;
        public IReadOnlyList<OutfitData> Outfits => m_outfits;
        public SpriteLibraryAsset MixAndMatchLibrary => m_mixAndMatchLibrary;

        private void Awake()
        {
            CacheComponents();
            EnableAutoRebind();
        }

        public void CacheComponents()
        {
            if (m_spriteLibrary == null)
            {
                m_spriteLibrary = GetComponent<SpriteLibrary>();
            }

            if (m_resolvers == null || m_resolvers.Count == 0)
            {
                m_resolvers = new List<SpriteResolver>(GetComponentsInChildren<SpriteResolver>(true));
            }

            if (m_spriteSkins == null || m_spriteSkins.Count == 0)
            {
                m_spriteSkins = new List<SpriteSkin>(GetComponentsInChildren<SpriteSkin>(true));
            }

            m_resolverByCategory.Clear();
            foreach (SpriteResolver resolver in m_resolvers)
            {
                if (resolver == null)
                {
                    continue;
                }

                string category = resolver.GetCategory();
                if (!string.IsNullOrEmpty(category) && !m_resolverByCategory.ContainsKey(category))
                {
                    m_resolverByCategory.Add(category, resolver);
                }
            }
        }

        private void EnableAutoRebind()
        {
            if (m_spriteSkins == null)
            {
                return;
            }

            foreach (SpriteSkin skin in m_spriteSkins)
            {
                if (skin != null)
                {
                    skin.autoRebind = true;
                }
            }
        }

        public void EquipOutfitByIndex(int index)
        {
            if (m_outfits == null || index < 0 || index >= m_outfits.Count)
            {
                return;
            }

            m_currentOutfitIndex = index;
            EquipOutfit(m_outfits[index]);
        }

        /// <summary>
        /// Method 1: Swap entire outfit by assigning a new SpriteLibraryAsset.
        /// All SpriteResolvers update automatically and SpriteSkins auto-rebind to the shared skeleton.
        /// </summary>
        public void EquipOutfit(OutfitData outfit)
        {
            if (outfit == null || m_spriteLibrary == null)
            {
                return;
            }

            if (outfit.SpriteLibraryAsset != null)
            {
                m_spriteLibrary.spriteLibraryAsset = outfit.SpriteLibraryAsset;

                foreach (var part in m_resolverByCategory)
                {
                    part.Value.SetCategoryAndLabel(part.Key, DefaultLabel);
                }

                m_spriteLibrary.RefreshSpriteResolvers();
            }

            EquipWeapon(outfit.WeaponSprite);

        }

        /// <summary>
        /// Method 2: Mix-and-match a single body part using a shared SpriteLibraryAsset with multiple labels.
        /// </summary>
        public void EquipPart(string category, string label)
        {
            if (m_spriteLibrary == null || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(label))
            {
                return;
            }

            if (m_mixAndMatchLibrary != null && m_spriteLibrary.spriteLibraryAsset != m_mixAndMatchLibrary)
            {
                m_spriteLibrary.spriteLibraryAsset = m_mixAndMatchLibrary;
                string currentLabel = $"Lv{m_currentOutfitIndex + 1}";
                foreach (var part in m_resolverByCategory)
                {
                    part.Value.SetCategoryAndLabel(part.Key, currentLabel);
                }
            }

            if (m_resolverByCategory.Count == 0)
            {
                CacheComponents();
            }

            if (m_resolverByCategory.TryGetValue(category, out SpriteResolver resolver) && resolver != null)
            {
                resolver.SetCategoryAndLabel(category, label);
                resolver.ResolveSpriteToSpriteRenderer();
            }
        }

        /// <summary>
        /// Method 3: Rigid attachment mounted directly under a bone (e.g., RightArm_Bone -> WeaponSlot).
        /// </summary>
        public void EquipWeapon(Sprite weaponSprite)
        {
            if (m_weaponRenderer == null)
            {
                return;
            }

            m_weaponRenderer.sprite = weaponSprite;
            m_weaponRenderer.enabled = weaponSprite != null;
        }

        public void SetupReferences(
            SpriteLibrary spriteLibrary,
            List<SpriteResolver> resolvers,
            List<SpriteSkin> spriteSkins,
            SpriteRenderer weaponRenderer,
            List<OutfitData> outfits,
            SpriteLibraryAsset mixAndMatchLibrary)
        {
            m_spriteLibrary = spriteLibrary;
            m_resolvers = resolvers;
            m_spriteSkins = spriteSkins;
            m_weaponRenderer = weaponRenderer;
            m_outfits = outfits;
            m_mixAndMatchLibrary = mixAndMatchLibrary;
        }
    }
}
