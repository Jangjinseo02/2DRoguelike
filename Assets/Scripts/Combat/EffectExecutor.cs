using System;
using System.Collections.Generic;
using Roguelike.Data;
using Roguelike.Data.Effects;

namespace Roguelike.Combat
{
    public static class EffectExecutor
    {
        public static void Execute(List<EffectInstance> effects, ICombatant source, BattleRoster roster, ICombatant explicitTarget, Random rng, int currentStacks = 0)
        {
            if (effects == null) return;

            foreach (var instance in effects)
            {
                if (instance.effect == null) continue;
                var targets = TargetResolver.Resolve(instance.targetType, source, explicitTarget, roster, rng);
                if (targets.Count == 0) continue;

                instance.effect.Execute(new EffectContext { Source = source, Targets = targets, CurrentStacks = currentStacks }, instance);
            }
        }
    }
}
