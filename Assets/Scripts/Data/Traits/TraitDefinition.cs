using System;
using System.Collections.Generic;
using UnityEngine;
using Roguelike.Data;

namespace Roguelike.Data.Traits
{
    [Serializable]
    public struct TraitTriggerEntry
    {
        public TriggerType trigger;
        public List<EffectInstance> effects;
    }

    // 이점 특성과 불이점 특성을 나눈다.
    public enum TraitCategory
    {
        Advantage,
        Disadvantage
    }

    /// <summary>
    /// A passive characteristic (background trait, class talent, or relic-like passive) held
    /// by a character. Combines always-on stat modifiers with reactive effects fired by
    /// battle-wide triggers (see TriggerType).
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Traits/Trait Definition", fileName = "Trait_")]
    public class TraitDefinition : ScriptableObject
    {
        public string traitId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public Rarity rarity;

        [Header("Point Buy")]
        public TraitCategory category;
        [Min(0)]
        public int pointCost;

        [Header("Bonus Advantage Slots"), Min(0), Tooltip("추가되는 이점 특성 슬롯 수, +3 > 3칸 추가")]
        public int bonusAdvantageSlots;

        [Tooltip("Applied once while this trait is held, removed the moment it is lost.")]
        public List<StatModifier> passiveStatModifiers;

        [Tooltip("Effects run when the matching TriggerType fires while this trait is held.")]
        public List<TraitTriggerEntry> triggeredEffects;
    }
}
