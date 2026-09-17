using System.Collections.Generic;
using UnityEngine;
using Roguelike.Data.Equipment;
using Roguelike.Data.Traits;

namespace Roguelike.Data.Enemies
{
    /// <summary>
    /// Minimal enemy template so the turn system has an opposing side to run against.
    /// turnActionEffects is a stub "always do this" action - swap it for a real intent/AI
    /// system (e.g. a weighted list of possible actions) once combat design firms up.
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Enemies/Enemy Definition", fileName = "Enemy_")]
    public class EnemyDefinition : ScriptableObject
    {
        public string enemyId;
        public string displayName;
        public Sprite portrait;
        public int maxHealth = 20;
        public List<EffectInstance> turnActionEffects;
        public List<TraitDefinition> startingTraits;
        public List<EquipmentDefinition> startingEquipment;
    }
}
