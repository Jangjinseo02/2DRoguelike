using System.Collections.Generic;
using Roguelike.Combat;

namespace Roguelike.Data.Effects
{
    /// <summary>Runtime information handed to an EffectDefinition when it executes.</summary>
    public class EffectContext
    {
        public ICombatant Source;
        public IReadOnlyList<ICombatant> Targets;
    }
}
