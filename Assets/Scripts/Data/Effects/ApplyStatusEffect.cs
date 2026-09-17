using UnityEngine;
using Roguelike.Data;
using Roguelike.Data.Status;

namespace Roguelike.Data.Effects
{
    [CreateAssetMenu(menuName = "Roguelike/Effects/Apply Status", fileName = "Effect_ApplyStatus")]
    public class ApplyStatusEffect : EffectDefinition
    {
        public StatusEffectDefinition status;

        public override void Execute(EffectContext context, EffectInstance instance)
        {
            if (status == null) return;
            int stacks = Mathf.Max(1, instance.stacks);
            foreach (var target in context.Targets)
                target.ApplyStatus(status, stacks);
        }
    }
}
