using System;
using System.Collections.Generic;
using Roguelike.Data;

namespace Roguelike.Combat
{
    /// <summary>
    /// Turns an effect's TargetType into concrete combatants. Every "Enemy"/"Ally" is relative to the
    /// effect's source: an enemy's SingleEnemy is the player, a player card's AllAllies is the player's
    /// side. That's what lets the same effect assets be shared between cards and enemy actions.
    /// </summary>
    public static class TargetResolver
    {
        private static readonly List<ICombatant> Empty = new List<ICombatant>();

        public static IReadOnlyList<ICombatant> Resolve(TargetType type, ICombatant source, ICombatant explicitTarget, BattleRoster roster, Random rng)
        {
            switch (type)
            {
                case TargetType.Self:
                    return source != null ? new List<ICombatant> { source } : Empty;

                case TargetType.SingleEnemy:
                    return Single(explicitTarget, Opponents(source, roster));

                case TargetType.SingleAlly:
                    return Single(explicitTarget, Side(source, roster), source);

                case TargetType.AllEnemies:
                    return FilterAlive(Opponents(source, roster));

                case TargetType.AllAllies:
                    return FilterAlive(Side(source, roster));

                case TargetType.RandomEnemy:
                    var aliveEnemies = FilterAlive(Opponents(source, roster));
                    if (aliveEnemies.Count == 0) return Empty;
                    var picked = aliveEnemies[(rng ?? new Random()).Next(aliveEnemies.Count)];
                    return new List<ICombatant> { picked };

                case TargetType.None:
                default:
                    return Empty;
            }
        }

        /// <summary>
        /// The explicit target if it's alive and on the right side (the player's chosen target). Otherwise
        /// - no choice was made, as with enemy actions and trait/status triggers - the preferred fallback
        /// if given and alive, else the first living combatant on that side.
        /// </summary>
        private static IReadOnlyList<ICombatant> Single(ICombatant explicitTarget, List<ICombatant> side, ICombatant preferred = null)
        {
            if (side == null) return Empty;

            if (explicitTarget != null && explicitTarget.IsAlive && side.Contains(explicitTarget))
                return new List<ICombatant> { explicitTarget };

            if (preferred != null && preferred.IsAlive && side.Contains(preferred))
                return new List<ICombatant> { preferred };

            foreach (var combatant in side)
                if (combatant.IsAlive) return new List<ICombatant> { combatant };

            return Empty;
        }

        private static List<ICombatant> Opponents(ICombatant source, BattleRoster roster) =>
            roster == null ? null : source != null ? roster.OpponentsOf(source) : roster.Enemies;

        private static List<ICombatant> Side(ICombatant source, BattleRoster roster) =>
            roster == null ? null : source != null ? roster.SideOf(source) : roster.Allies;

        private static List<ICombatant> FilterAlive(List<ICombatant> source)
        {
            var result = new List<ICombatant>();
            if (source == null) return result;
            foreach (var combatant in source)
                if (combatant.IsAlive) result.Add(combatant);
            return result;
        }
    }
}
