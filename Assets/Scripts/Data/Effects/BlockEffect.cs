using UnityEngine;
using Roguelike.Data;

namespace Roguelike.Data.Effects
{
    [CreateAssetMenu(menuName = "Roguelike/Effects/Block", fileName = "Effect_Block")]
    public class BlockEffect : EffectDefinition
    {
        public bool scalesWithDexterity = true;

        public override void Execute(EffectContext context, EffectInstance instance)
        {
            float amount = instance.magnitude;
            if (scalesWithDexterity && context.Source != null)
                amount += context.Source.GetStat(StatType.Dexterity);

            int block = Mathf.Max(0, Mathf.RoundToInt(amount));
            foreach (var target in context.Targets)
                target.GainBlock(block);
        }
    }
}
