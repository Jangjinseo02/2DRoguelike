namespace Roguelike.Data
{
    /// <summary>
    /// Numeric combat stats that can be modified by equipment, traits, and status effects.
    /// Max health is intentionally excluded - it is tracked directly on CombatantInstance
    /// because changing it also has to touch current health (see CombatantInstance.ModifyMaxHealth).
    /// </summary>
    public enum StatType
    {
        Strength,
        Dexterity,
        Energy,
        CardDraw,
        Speed,
        CritChance,
        CritDamage
    }
}
