using System.Collections.Generic;
using UnityEngine;
using Roguelike.Data;
using Roguelike.Data.Traits;
using Roguelike.Data.Cards;

namespace Roguelike.Data.Equipment
{
    public enum EquipmentSlot
    {
        Weapon,
        Armor,
        Accessory,
        Trinket
    }

    public enum AttackType
    {
        None,
        Slash, // 롱소드, 도끼
        Blunt, // 망치, 철퇴
        Pierce, // 활, 단검
        Magic, // 스태프
    }

    public enum ArmorType
    {
        None,
        Light, // 천갑
        Medium, // 경갑
        Heavy // 중갑
    }

    [CreateAssetMenu(menuName = "Roguelike/Equipment/Equipment Definition", fileName = "Equipment_")]
    public class EquipmentDefinition : ScriptableObject
    {
        public string equipmentId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public EquipmentSlot slot;
        public AttackType attackType;
        public ArmorType armorType;
        public Rarity rarity;

        public List<StatModifier> statModifiers;

        [Tooltip("Traits granted for as long as this item stays equipped.")]
        public List<TraitDefinition> grantedTraits;

        [Tooltip("Cards added to the deck for as long as this item stays equipped (e.g. a weapon that adds its own attack card).")]
        public List<CardDefinition> grantedCards;

        private void OnValidate()
        {
            if (slot != EquipmentSlot.Weapon && attackType != AttackType.None)
            { attackType = AttackType.None; Debug.Log($"{name}: 슬롯이 Attack이 아니라서 AttackType을 지웁니다."); }
                
            if (slot != EquipmentSlot.Armor && armorType != ArmorType.None)
            { armorType = ArmorType.None; Debug.Log($"{name}: 슬롯이 Armor가 아니라서 ArmorType을 지웁니다."); }
        }
    }
}
