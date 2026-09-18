using System;
using Roguelike.Data.Effects;

namespace Roguelike.Data
{
    /// <summary>
    /// One configured use of an EffectDefinition: which effect asset, how strong, how many
    /// stacks, and who it targets. Cards, traits, and status effects all use lists of these
    /// instead of hard-coding behaviour, so designers compose new content from existing
    /// effect assets in the inspector.
    /// </summary>
    [Serializable]
    public struct EffectInstance
    {
        public EffectDefinition effect;
        public TargetType targetType;
        public float magnitude;
        public int stacks;
    }
}
