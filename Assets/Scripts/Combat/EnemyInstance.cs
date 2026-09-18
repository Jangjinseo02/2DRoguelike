using System;
using Roguelike.Data.Enemies;
using Roguelike.Data.Equipment;

namespace Roguelike.Combat
{
    /// <summary>
    /// Minimal runtime enemy: has health/block/status like any CombatantInstance, and one
    /// stub action it repeats every turn. Replace ExecuteTurnAction with a real intent
    /// system (telegraphed next move, weighted action pool, phases...) as combat design grows.
    /// </summary>
    public class EnemyInstance : CombatantInstance
    {
        public EnemyDefinition Definition { get; }

        public EnemyInstance(EnemyDefinition definition, AttackArmorMatchTable matchTable) 
        : base(matchTable)
        {
            Definition = definition;
            DisplayName = definition.displayName;
            MaxHealth = definition.maxHealth;
            CurrentHealth = MaxHealth;

            foreach(var trait in definition.startingTraits)
                AddTrait(trait);
            foreach(var eqipment in definition.startingEquipment)
                Equip(eqipment);
        }

        public void ExecuteTurnAction(BattleRoster roster, Random rng)
        {
            EffectExecutor.Execute(Definition.turnActionEffects, this, roster, null, rng);
        }
    }
}
