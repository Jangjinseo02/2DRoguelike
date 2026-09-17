using System.Collections.Generic;

namespace Roguelike.Combat
{
    /// <summary>The two sides of an ongoing battle, used to resolve AoE/random targeting.</summary>
    public class BattleRoster
    {
        /// <summary>The player's side.</summary>
        public List<ICombatant> Allies { get; } = new List<ICombatant>();
        /// <summary>The opposing side.</summary>
        public List<ICombatant> Enemies { get; } = new List<ICombatant>();

        /// <summary>The side <paramref name="combatant"/> fights on. "Allies"/"Enemies" above are named
        /// from the player's point of view; targeting must use these relative lookups instead, or an
        /// enemy's "all enemies" effect would hit its own side.</summary>
        public List<ICombatant> SideOf(ICombatant combatant) => Enemies.Contains(combatant) ? Enemies : Allies;

        /// <summary>The side opposing <paramref name="combatant"/>.</summary>
        public List<ICombatant> OpponentsOf(ICombatant combatant) => Enemies.Contains(combatant) ? Allies : Enemies;
    }
}
