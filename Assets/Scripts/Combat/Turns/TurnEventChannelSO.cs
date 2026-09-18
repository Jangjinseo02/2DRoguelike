using System;
using UnityEngine;

namespace Roguelike.Combat.Turns
{
    /// <summary>
    /// ScriptableObject event channel: UI, VFX, and audio systems can subscribe to a shared
    /// asset reference instead of needing a direct reference to whichever TurnManager is
    /// running the current battle (the "ScriptableObject Architecture" observer pattern).
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Events/Turn Event Channel", fileName = "TurnEventChannel")]
    public class TurnEventChannelSO : ScriptableObject
    {
        public event Action<TurnPhase> OnPhaseChanged;

        public void Raise(TurnPhase phase) => OnPhaseChanged?.Invoke(phase);
    }
}
