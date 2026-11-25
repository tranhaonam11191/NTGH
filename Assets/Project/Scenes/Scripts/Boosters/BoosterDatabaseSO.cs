using System;
using System.Collections.Generic;
using UnityEngine;

namespace NTGH.Boosters
{
    [Serializable]
    public class BoosterConfig
    {
        public BoosterType type = BoosterType.Hammer;
        public string id = "booster_hammer";
        public string displayName = "Búa Phá Ô";
        [TextArea(2, 4)]
        public string description = "Phá hủy ngay lập tức 1 ô bất kỳ trên bàn cờ.";
        public string targetingHint = "Chọn 1 ô bất kỳ để phá hủy";
        public Sprite icon;

        [Header("Unlock & Starter")]
        [Min(1)] public int unlockLevel = 1;
        [Min(0)] public int initialFreeCount = 2;

        [Header("Shop Pricing (Gold)")]
        [Min(1)] public int pricePack1 = 150;
        [Min(1)] public int pricePack3 = 400;

        public bool RequiresTarget => type != BoosterType.Shuffle;

        public bool IsUnlockedAtLevel(int currentLevelNumber)
        {
            return Mathf.Max(1, currentLevelNumber) >= Mathf.Max(1, unlockLevel);
        }

        public int GetPriceForPack(int packSize)
        {
            return packSize >= 3 ? Mathf.Max(1, pricePack3) : Mathf.Max(1, pricePack1);
        }
    }

    [CreateAssetMenu(fileName = "BoosterDatabase", menuName = "NTGH/Boosters/Booster Database")]
    public class BoosterDatabaseSO : ScriptableObject
    {
        public const string DefaultResourcesPath = "Data/BoosterDatabase";

        [SerializeField] private List<BoosterConfig> m_boosters = new List<BoosterConfig>();

        public IReadOnlyList<BoosterConfig> Boosters => m_boosters;

        public BoosterConfig GetConfig(BoosterType type)
        {
            if (m_boosters == null) return null;
            for (int i = 0; i < m_boosters.Count; i++)
            {
                if (m_boosters[i] != null && m_boosters[i].type == type)
                {
                    return m_boosters[i];
                }
            }
            return null;
        }

        public static BoosterDatabaseSO LoadDefault()
        {
            if (SOManager.instance != null && SOManager.instance.BoosterDatabase != null)
            {
                return SOManager.instance.BoosterDatabase;
            }

            return Resources.Load<BoosterDatabaseSO>(DefaultResourcesPath);
        }

        public void SetBoosters(List<BoosterConfig> configs)
        {
            m_boosters = configs ?? new List<BoosterConfig>();
        }

        public void EnsureDefaultEntries()
        {
            if (m_boosters != null && m_boosters.Count >= 4)
            {
                return;
            }

            m_boosters = new List<BoosterConfig>
            {
                new BoosterConfig
                {
                    type = BoosterType.Hammer,
                    id = "booster_hammer",
                    displayName = "Búa Phá Ô",
                    description = "Phá hủy ngay lập tức 1 ô bất kỳ trên bàn cờ và gây sát thương.",
                    targetingHint = "Chọn 1 ô bất kỳ để phá hủy",
                    unlockLevel = 1,
                    initialFreeCount = 2,
                    pricePack1 = 150,
                    pricePack3 = 400
                },
                new BoosterConfig
                {
                    type = BoosterType.FreeSwap,
                    id = "booster_free_swap",
                    displayName = "Đổi Chỗ Tự Do",
                    description = "Hoán đổi 2 ô kề nhau bất kỳ mà không cần tạo match-3.",
                    targetingHint = "Chọn 2 ô kề nhau để đổi chỗ tự do",
                    unlockLevel = 2,
                    initialFreeCount = 2,
                    pricePack1 = 200,
                    pricePack3 = 520
                },
                new BoosterConfig
                {
                    type = BoosterType.LineBlast,
                    id = "booster_line_blast",
                    displayName = "Phá Hàng & Cột",
                    description = "Quét sạch toàn bộ hàng ngang và cột dọc đi qua ô được chọn.",
                    targetingHint = "Chọn 1 ô để phá sạch cả hàng và cột",
                    unlockLevel = 3,
                    initialFreeCount = 2,
                    pricePack1 = 300,
                    pricePack3 = 780
                },
                new BoosterConfig
                {
                    type = BoosterType.Shuffle,
                    id = "booster_shuffle",
                    displayName = "Xáo Trộn Bàn",
                    description = "Xáo trộn toàn bộ các ô trên bàn cờ để mở ra nước đi mới.",
                    targetingHint = string.Empty,
                    unlockLevel = 4,
                    initialFreeCount = 2,
                    pricePack1 = 150,
                    pricePack3 = 400
                }
            };
        }
    }
}
