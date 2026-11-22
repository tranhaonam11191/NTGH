using System;
using System.Collections.Generic;
using UnityEngine;

namespace NTGH.UI
{
    [Serializable]
    public class SelectableItemEntry
    {
        [SerializeField] private string m_id;
        [SerializeField] private string m_displayName;
        [SerializeField] private string m_subtitle;
        [SerializeField] private string m_groupLabel;
        [SerializeField] private Sprite m_previewSprite;
        [SerializeField] private CustomizeUnlockMethod m_unlockMethod = CustomizeUnlockMethod.Gold;
        [SerializeField] private int m_priceGold;
        [SerializeField] private int m_adsRequired = 1;
        [SerializeField] private string m_payload;

        public string Id => m_id;
        public string DisplayName => m_displayName;
        public string Subtitle => m_subtitle;
        public string GroupLabel => m_groupLabel;
        public Sprite PreviewSprite => m_previewSprite;
        public CustomizeUnlockMethod UnlockMethod => m_unlockMethod;
        public int PriceGold => Mathf.Max(0, m_priceGold);
        public int AdsRequired => Mathf.Max(1, m_adsRequired);
        public string Payload => m_payload;

        public SelectableItemEntry() { }

        public SelectableItemEntry(
            string id,
            string displayName,
            string subtitle,
            Sprite previewSprite,
            CustomizeUnlockMethod unlockMethod,
            int priceGold = 0,
            int adsRequired = 1,
            string groupLabel = "",
            string payload = "")
        {
            m_id = id;
            m_displayName = displayName;
            m_subtitle = subtitle;
            m_previewSprite = previewSprite;
            m_unlockMethod = unlockMethod;
            m_priceGold = Mathf.Max(0, priceGold);
            m_adsRequired = Mathf.Max(1, adsRequired);
            m_groupLabel = groupLabel ?? string.Empty;
            m_payload = payload ?? string.Empty;
        }
    }

    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "NTGH/UI/Item Catalog")]
    public class ItemCatalogSO : ScriptableObject
    {
        [Header("Popup Labels")]
        [SerializeField] private string m_defaultLabel = "CHỌN VẬT PHẨM";
        [SerializeField] private string m_equipActionLabel = "CHỌN";
        [SerializeField] private string m_equippedStatusLabel = "ĐANG DÙNG";

        [Header("Items")]
        [SerializeField] private List<SelectableItemEntry> m_items = new List<SelectableItemEntry>();

        public string DefaultLabel => m_defaultLabel;
        public string EquipActionLabel => m_equipActionLabel;
        public string EquippedStatusLabel => m_equippedStatusLabel;
        public IReadOnlyList<SelectableItemEntry> Items => m_items;

        public void Configure(
            string defaultLabel,
            string equipActionLabel,
            string equippedStatusLabel,
            List<SelectableItemEntry> items)
        {
            m_defaultLabel = defaultLabel;
            m_equipActionLabel = equipActionLabel;
            m_equippedStatusLabel = equippedStatusLabel;
            m_items = items ?? new List<SelectableItemEntry>();
        }
    }
}
