using UnityEngine;
using Roguelike.Data;

namespace Roguelike.Data.Effects
{
    /// <summary>Permanently raises (or lowers) a target's max health, healing/clamping alongside it.</summary>
    [CreateAssetMenu(menuName = "Roguelike/Effects/Modify Max Health", fileName = "Effect_ModifyMaxHealth")]
    public class ModifyMaxHealthEffect : EffectDefinition
    {
        public override void Execute(EffectContext context, EffectInstance instance)
        {
            int amount = Mathf.RoundToInt(instance.magnitude);
            foreach (var target in context.Targets)
                target.ModifyMaxHealth(amount);
        }
    }
}
