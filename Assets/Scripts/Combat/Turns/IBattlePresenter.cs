using System;

namespace Roguelike.Combat.Turns
{
    /// <summary>
    /// What TurnManager waits on so the enemy turn can play out over time instead of in one call.
    /// Declared in Combat and implemented by presentation (UIManager), so Combat never references UI.
    ///
    /// Every method hands over a continuation; the battle does not advance until it's invoked. Invoking
    /// it more than once is harmless (TurnManager ignores repeats), but never invoking it stalls the battle.
    /// With no presenter set, TurnManager continues immediately - headless tests run the same logic.
    /// </summary>
    public interface IBattlePresenter
    {
        /// <summary>The player's turn just ended (hand already discarded). Call onReady once the screen
        /// is ready for enemies to act - e.g. after the hand's discard animation has settled.</summary>
        void PresentEnemyTurnStart(Action onReady);

        /// <summary>
        /// One enemy is about to act. Play its action and call resolve at the moment of impact - that's
        /// when the action's effects are actually applied, so combatant events (Damaged, Changed, Died)
        /// fire in sync with the animation. Call onComplete when the whole action has finished playing.
        /// If resolve was never called, onComplete resolves the action first.
        /// </summary>
        void PresentEnemyAction(EnemyInstance enemy, Action resolve, Action onComplete);
    }
}
