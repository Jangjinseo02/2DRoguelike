using Roguelike.Data;
using Roguelike.Data.Status;

namespace Roguelike.Combat
{
    /// <summary>Anything that can take part in combat: a player character or an enemy.</summary>
    public interface ICombatant
    {
        string DisplayName { get; }
        int MaxHealth { get; }
        int CurrentHealth { get; }
        int Block { get; }
        bool IsAlive { get; }

        float GetStat(StatType type);
        void TakeDamage(int amount);
        void GainBlock(int amount);
        void Heal(int amount);
        void ModifyMaxHealth(int amount);
        void ApplyStatModifier(StatModifier modifier, object source);
        void RemoveStatModifiersFromSource(object source);
        void ApplyStatus(StatusEffectDefinition status, int stacks);
    }
}
