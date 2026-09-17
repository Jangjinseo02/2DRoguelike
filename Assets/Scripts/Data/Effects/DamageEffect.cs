using UnityEngine;
using Roguelike.Data;

namespace Roguelike.Data.Effects
{
    [CreateAssetMenu(menuName = "Roguelike/Effects/Damage", fileName = "Effect_Damage")]
    public class DamageEffect : EffectDefinition
    {
        [Tooltip("Added to instance.magnitude before dealing damage.")]
        public bool scalesWithStrength = true;
        public int hitCount = 1;

        public override void Execute(EffectContext context, EffectInstance instance)
        {
            float amount = instance.magnitude;
            if (scalesWithStrength && context.Source != null)
                amount += context.Source.GetStat(StatType.Strength);

            int damage = Mathf.Max(0, Mathf.RoundToInt(amount));
            for (int hit = 0; hit < hitCount; hit++)
            {
                foreach (var target in context.Targets)
                    target.TakeDamage(damage);
            }
        }
    }
}
