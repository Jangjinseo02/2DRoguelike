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

    public enum WeaponType
    {
        None,
        Sword,
        Axe,
        Bow,
        Staff,
        Dagger
    }

    public enum ArmorType
    {
        None,
        Light,
        Medium,
        Heavy
    }

    [CreateAssetMenu(menuName = "Roguelike/Equipment/Equipment Definition", fileName = "Equipment_")]
    public class EquipmentDefinition : ScriptableObject
    {
        public string equipmentId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public EquipmentSlot slot;
        public WeaponType weaponType;
        public ArmorType armorType;
        public Rarity rarity;

        public List<StatModifier> statModifiers;

        [Tooltip("Traits granted for as long as this item stays equipped.")]
        public List<TraitDefinition> grantedTraits;

        [Tooltip("Cards added to the deck for as long as this item stays equipped (e.g. a weapon that adds its own attack card).")]
        public List<CardDefinition> grantedCards;

        private void OnValidate()
        {
            if (slot != EquipmentSlot.Weapon)
                weaponType = WeaponType.None;
            if (slot != EquipmentSlot.Armor)
                armorType = ArmorType.None;
        }
    }
}
