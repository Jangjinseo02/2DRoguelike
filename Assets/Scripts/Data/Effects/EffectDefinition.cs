using UnityEngine;
using Roguelike.Data;

namespace Roguelike.Data.Effects
{
    /// <summary>
    /// Base type for a single, reusable piece of combat behaviour (deal damage, gain block,
    /// draw cards, apply a status...). Concrete subclasses are ScriptableObject assets that
    /// designers drop into a card/trait/status's effect list and configure per use via
    /// EffectInstance.magnitude / stacks.
    /// </summary>
    public abstract class EffectDefinition : ScriptableObject
    {
        public abstract void Execute(EffectContext context, EffectInstance instance);
    }
}
