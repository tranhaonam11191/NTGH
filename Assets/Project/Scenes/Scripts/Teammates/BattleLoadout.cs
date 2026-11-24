using System.Collections.Generic;
using NTGH.UI;
using UnityEngine;

namespace NTGH.Teammates
{
    /// <summary>
    /// Snapshot of a combatant's equipped support items (Weapon, Outfit, and up to 4 Teammate Cards)
    /// passed into GameController when starting a battle.
    /// </summary>
    public class BattleLoadout
    {
        public string WeaponId { get; set; }
        public string WeaponName { get; set; }
        public Sprite WeaponIcon { get; set; }
        public int WeaponBonusAttack { get; set; }

        public string OutfitId { get; set; }
        public string OutfitName { get; set; }
        public Sprite OutfitIcon { get; set; }
        public int OutfitBonusHealth { get; set; }

        public TeammateCardSO[] Teammates { get; } = new TeammateCardSO[TeammateCardDatabaseSO.MaxSquadSlots];

        public int GetTotalAttackBonus()
        {
            int bonus = Mathf.Max(0, WeaponBonusAttack);
            for (int i = 0; i < Teammates.Length; i++)
            {
                var card = Teammates[i];
                if (card != null && card.CardType == TeammateCardType.Attack)
                {
                    bonus += card.EffectValue;
                }
            }
            return bonus;
        }

        public int GetTotalDamageReduction()
        {
            int reduction = 0;
            for (int i = 0; i < Teammates.Length; i++)
            {
                var card = Teammates[i];
                if (card != null && card.CardType == TeammateCardType.Support)
                {
                    reduction += card.EffectValue;
                }
            }
            return reduction;
        }

        public static BattleLoadout FromUserData(
            UserData userData,
            ItemCatalogSO weaponCatalog,
            ItemCatalogSO outfitCatalog,
            TeammateCardDatabaseSO teammateDatabase,
            CreateItemReward fallbackWeapon = null)
        {
            var loadout = new BattleLoadout();
            if (userData == null) return loadout;

            userData.EnsureTeammateDefaults(teammateDatabase);

            // 1. Weapon
            string weaponId = userData.current_weapon_id;
            SelectableItemEntry weaponEntry = FindEntry(weaponCatalog, weaponId);
            if (weaponEntry != null)
            {
                loadout.WeaponId = weaponEntry.Id;
                loadout.WeaponName = weaponEntry.DisplayName;
                loadout.WeaponIcon = weaponEntry.PreviewSprite;
                loadout.WeaponBonusAttack = ResolveWeaponBonusAttack(weaponEntry.Id, weaponEntry.PriceGold);
            }
            else if (fallbackWeapon != null && fallbackWeapon.ItemReward != null)
            {
                loadout.WeaponId = fallbackWeapon.ItemReward.ID;
                loadout.WeaponName = fallbackWeapon.ItemReward.Name;
                loadout.WeaponIcon = fallbackWeapon.ItemReward.Sprite;
                loadout.WeaponBonusAttack = ResolveWeaponBonusAttack(fallbackWeapon.ItemReward.ID, fallbackWeapon.ItemReward.Price);
            }

            // 2. Outfit
            string outfitId = userData.current_outfit_id;
            SelectableItemEntry outfitEntry = FindEntry(outfitCatalog, outfitId);
            if (outfitEntry == null && outfitCatalog != null && outfitCatalog.Items != null && outfitCatalog.Items.Count > 0)
            {
                outfitEntry = outfitCatalog.Items[0];
            }
            if (outfitEntry != null)
            {
                loadout.OutfitId = outfitEntry.Id;
                loadout.OutfitName = outfitEntry.DisplayName;
                loadout.OutfitIcon = outfitEntry.PreviewSprite;
                loadout.OutfitBonusHealth = ResolveOutfitBonusHealth(outfitEntry.Id, outfitEntry.Subtitle);
            }

            // 3. Teammates (4 slots)
            if (teammateDatabase != null)
            {
                for (int i = 0; i < TeammateCardDatabaseSO.MaxSquadSlots; i++)
                {
                    string cardId = userData.GetEquippedTeammateAt(i);
                    loadout.Teammates[i] = teammateDatabase.GetCardById(cardId);
                }
            }

            return loadout;
        }

        public static BattleLoadout FromCharacterData(
            CharacterData characterData,
            ItemCatalogSO weaponCatalog,
            ItemCatalogSO outfitCatalog)
        {
            var loadout = new BattleLoadout();
            if (characterData == null) return loadout;

            // Enemy Weapon
            if (!string.IsNullOrEmpty(characterData.equippedWeaponId))
            {
                var entry = FindEntry(weaponCatalog, characterData.equippedWeaponId);
                if (entry != null)
                {
                    loadout.WeaponId = entry.Id;
                    loadout.WeaponName = entry.DisplayName;
                    loadout.WeaponIcon = entry.PreviewSprite;
                    loadout.WeaponBonusAttack = characterData.weaponBonusAttack > 0
                        ? characterData.weaponBonusAttack
                        : ResolveWeaponBonusAttack(entry.Id, entry.PriceGold);
                }
            }
            if (loadout.WeaponIcon == null && characterData.weaponIcon != null)
            {
                loadout.WeaponIcon = characterData.weaponIcon;
                loadout.WeaponBonusAttack = Mathf.Max(0, characterData.weaponBonusAttack);
            }

            // Enemy Outfit
            if (!string.IsNullOrEmpty(characterData.equippedOutfitId))
            {
                var entry = FindEntry(outfitCatalog, characterData.equippedOutfitId);
                if (entry != null)
                {
                    loadout.OutfitId = entry.Id;
                    loadout.OutfitName = entry.DisplayName;
                    loadout.OutfitIcon = entry.PreviewSprite;
                    loadout.OutfitBonusHealth = characterData.outfitBonusHealth > 0
                        ? characterData.outfitBonusHealth
                        : ResolveOutfitBonusHealth(entry.Id, entry.Subtitle);
                }
            }
            if (loadout.OutfitIcon == null && characterData.outfitIcon != null)
            {
                loadout.OutfitIcon = characterData.outfitIcon;
                loadout.OutfitBonusHealth = Mathf.Max(0, characterData.outfitBonusHealth);
            }

            // Enemy Teammates (up to 4)
            if (characterData.equippedTeammates != null)
            {
                int count = Mathf.Min(TeammateCardDatabaseSO.MaxSquadSlots, characterData.equippedTeammates.Count);
                for (int i = 0; i < count; i++)
                {
                    loadout.Teammates[i] = characterData.equippedTeammates[i];
                }
            }

            return loadout;
        }

        private static SelectableItemEntry FindEntry(ItemCatalogSO catalog, string id)
        {
            if (catalog == null || catalog.Items == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < catalog.Items.Count; i++)
            {
                var item = catalog.Items[i];
                if (item != null && item.Id == id) return item;
            }
            return null;
        }

        private static int ResolveWeaponBonusAttack(string weaponId, int priceGold)
        {
            if (string.IsNullOrEmpty(weaponId)) return 0;
            string lower = weaponId.ToLowerInvariant();
            if (lower.Contains("katana")) return 15;
            if (lower.Contains("maloan")) return 30;
            return Mathf.Max(10, priceGold / 20);
        }

        private static int ResolveOutfitBonusHealth(string outfitId, string subtitle)
        {
            if (!string.IsNullOrEmpty(subtitle))
            {
                int plusIdx = subtitle.IndexOf('+');
                if (plusIdx >= 0)
                {
                    int num = 0;
                    bool found = false;
                    for (int i = plusIdx + 1; i < subtitle.Length; i++)
                    {
                        char c = subtitle[i];
                        if (char.IsDigit(c))
                        {
                            num = num * 10 + (c - '0');
                            found = true;
                        }
                        else if (found)
                        {
                            break;
                        }
                    }
                    if (found) return num;
                }
            }

            if (string.IsNullOrEmpty(outfitId)) return 0;
            if (outfitId.Contains("lv5")) return 1200;
            if (outfitId.Contains("lv4")) return 600;
            if (outfitId.Contains("lv3")) return 320;
            if (outfitId.Contains("lv2")) return 150;
            if (outfitId.Contains("lv1")) return 50;
            return 0;
        }
    }
}
