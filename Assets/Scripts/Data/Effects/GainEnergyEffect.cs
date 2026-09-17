using UnityEngine;
using Roguelike.Data;
using Roguelike.Combat;

namespace Roguelike.Data.Effects
{
    [CreateAssetMenu(menuName = "Roguelike/Effects/Gain Energy", fileName = "Effect_GainEnergy")]
    public class GainEnergyEffect : EffectDefinition
    {
        public override void Execute(EffectContext context, EffectInstance instance)
        {
            int amount = Mathf.Max(0, Mathf.RoundToInt(instance.magnitude));
            foreach (var target in context.Targets)
            {
                if (target is IDeckOwner deckOwner)
                    deckOwner.GainEnergy(amount);
            }
        }
    }
}
