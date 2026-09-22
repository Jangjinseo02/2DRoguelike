using System.Collections.Generic;
using Roguelike.Combat;

namespace Roguelike.Data.Effects
{
    /// <summary>Runtime information handed to an EffectDefinition when it executes.</summary>
    public class EffectContext
    {
        public ICombatant Source;
        public IReadOnlyList<ICombatant> Targets;
        public int CurrentStacks; //상태이상 틱일 때만 채워짐, 그 외에는 0
    }
}
