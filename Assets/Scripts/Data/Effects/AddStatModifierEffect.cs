using UnityEngine;
using Roguelike.Data;

namespace Roguelike.Data.Effects
{
    /// <summary>Grants a temporary or permanent stat modifier, e.g. a card that gives +3 Strength this combat.</summary>
    [CreateAssetMenu(menuName = "Roguelike/Effects/Add Stat Modifier", fileName = "Effect_AddStatModifier")]
    public class AddStatModifierEffect : EffectDefinition
    {
        [Tooltip("value is multiplied by instance.magnitude (leave magnitude at 1 to use the value as-is).")]
        public StatModifier modifier;

        public override void Execute(EffectContext context, EffectInstance instance)
        {
            var scaled = modifier;
            scaled.value *= instance.magnitude;
            foreach (var target in context.Targets)
                target.ApplyStatModifier(scaled, this);
        }
    }
}
