using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelike.Data.Traits;
using Roguelike.Data.Equipment;
using Roguelike.Data.Cards;

namespace Roguelike.Data.Characters
{
    [Serializable]
    public struct DeckEntry
    {
        public CardDefinition card;
        public int count;
    }

    /// <summary>
    /// Design-time template for a playable character/class: base stats and starting loadout.
    /// A run's mutable state (current HP, hand, equipped items, etc.) lives on CharacterInstance,
    /// which is built from one of these at the start of a run - never mutate this asset at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Characters/Character Definition", fileName = "Character_")]
    public class CharacterDefinition : ScriptableObject
    {
        public string characterId;
        public string displayName;
        [TextArea] public string description;
        public Sprite portrait;

        [Header("Base Stats")]
        public int maxHealth = 50;
        public int baseEnergyPerTurn = 3;
        public int baseCardDrawPerTurn = 5;

        [Header("Starting Loadout")]
        public List<DeckEntry> startingDeck;
        public List<TraitDefinition> startingTraits;
        public List<EquipmentDefinition> startingEquipment;
    }
}
