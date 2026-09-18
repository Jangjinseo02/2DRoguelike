using UnityEngine;
using Roguelike.Data;
using Roguelike.Combat;

namespace Roguelike.Data.Effects
{
    [CreateAssetMenu(menuName = "Roguelike/Effects/Draw Cards", fileName = "Effect_DrawCards")]
    public class DrawCardsEffect : EffectDefinition
    {
        public override void Execute(EffectContext context, EffectInstance instance)
        {
            int count = Mathf.Max(0, Mathf.RoundToInt(instance.magnitude));
            foreach (var target in context.Targets)
            {
                if (target is IDeckOwner deckOwner)
                    deckOwner.DrawCards(count);
            }
        }
    }
}
