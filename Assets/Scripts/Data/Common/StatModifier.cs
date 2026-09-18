using System;

namespace Roguelike.Data
{
    public enum ModifierOperation
    {
        Add,
        Multiply,
        Override
    }

    /// <summary>
    /// Design-time description of a stat change (e.g. "+2 Strength", "x1.5 CritDamage").
    /// Applying one at runtime wraps it in an ActiveStatModifier together with the object
    /// that granted it, so it can be removed again when that trait/equipment/status goes away.
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public StatType statType;
        public ModifierOperation operation;
        public float value;
    }
}
