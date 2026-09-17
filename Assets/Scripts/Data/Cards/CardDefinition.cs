using System.Collections.Generic;
using UnityEngine;
using Roguelike.Data;

namespace Roguelike.Data.Cards
{
    public enum CardType
    {
        Attack,
        Skill,
        Power,
        Status,
        Curse
    }

    [CreateAssetMenu(menuName = "Roguelike/Cards/Card Definition", fileName = "Card_")]
    public class CardDefinition : ScriptableObject
    {
        public string cardId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public CardType cardType;
        public Rarity rarity;
        public int energyCost = 1;

        [Tooltip("Removed from the run entirely after being played, instead of going to the discard pile.")]
        public bool exhausts;

        [Tooltip("Can be played even before it would normally be drawn (STS-style 'Innate').")]
        public bool innate;

        [Tooltip("Each effect carries its own target, so one card can e.g. damage an enemy and block for self.")]
        public List<EffectInstance> effects;

        [Tooltip("Optional upgraded variant for a 'Card+' style upgrade system.")]
        public CardDefinition upgradedVersion;

        /// <summary>True if the player must pick a specific target before this card can resolve.</summary>
        public bool RequiresManualTarget =>
            effects != null && effects.Exists(e => e.targetType == TargetType.SingleEnemy || e.targetType == TargetType.SingleAlly);
    }
}
