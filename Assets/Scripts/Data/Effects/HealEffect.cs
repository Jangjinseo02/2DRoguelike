using UnityEngine;
using Roguelike.Data;

namespace Roguelike.Data.Effects
{
    [CreateAssetMenu(menuName = "Roguelike/Effects/Heal", fileName = "Effect_Heal")]
    public class HealEffect : EffectDefinition
    {
        public override void Execute(EffectContext context, EffectInstance instance)
        {
            int amount = Mathf.Max(0, Mathf.RoundToInt(instance.magnitude));
            foreach (var target in context.Targets)
                target.Heal(amount);
        }
    }
}
