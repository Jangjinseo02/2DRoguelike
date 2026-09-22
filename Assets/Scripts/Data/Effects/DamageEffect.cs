using UnityEngine;
using Roguelike.Data;

namespace Roguelike.Data.Effects
{
    [CreateAssetMenu(menuName = "Roguelike/Effects/Damage", fileName = "Effect_Damage")]
    public class DamageEffect : EffectDefinition
    {
        [Tooltip("Added to instance.magnitude before dealing damage.")]
        public bool scalesWithStrength = true;
        [Tooltip("무기 - 방어구 상성을 적용할지 여부. 출혈, 화상과 같은 상태이상 데미지일 경우 꺼주세요.")]
        public bool useWeaponMatchup = true;
        public int hitCount = 1;
        public float damagePerStack = 0;

        public override void Execute(EffectContext context, EffectInstance instance)
        {
            float amount = instance.magnitude;
            if (scalesWithStrength && context.Source != null)
                amount += context.Source.GetStat(StatType.Strength);
            
            amount += damagePerStack * context.CurrentStacks; // 스택당 추가 피해
            bool penetrates = useWeaponMatchup && context.Source != null && instance.targetType == TargetType.SingleEnemy && context.Source.TryConsumePenetration();
            int damage = Mathf.Max(0, Mathf.RoundToInt(amount));
            for (int hit = 0; hit < hitCount; hit++)
            {
                foreach (var target in context.Targets)
                {
                    if(useWeaponMatchup && context.Source != null) 
                        target.TakeDamage(damage, context.Source.CurrentAttackType, penetrates);
                    else 
                        target.TakeDamage(damage); 
                } 
            }
        }
    }
}
