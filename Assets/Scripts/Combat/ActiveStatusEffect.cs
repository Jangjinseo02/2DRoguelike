using Roguelike.Data.Status;

namespace Roguelike.Combat
{
    /// <summary>Runtime-only: how many stacks of a StatusEffectDefinition a combatant currently holds.</summary>
    public class ActiveStatusEffect
    {
        public StatusEffectDefinition definition;
        public int stacks;
    }
}
