using System.Collections.Generic;
using NTGH.Teammates;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/CharacterData", order = 1)]
public class CharacterData : ScriptableObject
{
    public Character Character;

    [Header("Battle Support Loadout (Enemy / Preset)")]
    public string equippedWeaponId;
    public Sprite weaponIcon;
    [Min(0)] public int weaponBonusAttack;

    public string equippedOutfitId;
    public Sprite outfitIcon;
    [Min(0)] public int outfitBonusHealth;

    [Tooltip("Up to 4 teammate cards equipped by this character/enemy in battle.")]
    public List<TeammateCardSO> equippedTeammates = new List<TeammateCardSO>();
}
