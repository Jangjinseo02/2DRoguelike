using System.Collections.Generic;
using UnityEngine;
using Roguelike.Data;

namespace Roguelike.Data.Status
{
    public enum StatusStackBehavior
    {
        /// <summary>New applications add to the stack count (e.g. Strength).</summary>
        Intensity,
        /// <summary>New applications refresh/extend the remaining duration instead of stacking (e.g. Vulnerable).</summary>
        Duration
    }

    [CreateAssetMenu(menuName = "Roguelike/Status Effects/Status Effect Definition", fileName = "Status_")]
    public class StatusEffectDefinition : ScriptableObject
    {
        public string statusId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public bool isDebuff;
        [Tooltip("이 상태이상을 가진 전투원의 단일 공격이 관통이 됩니다.(스택 1 소모)")]
        public bool grantsPenetration;
        public StatusStackBehavior stackBehavior;

        [Tooltip("Stacks removed at the end of the owner's turn. 0 = never decays on its own (e.g. Strength).")]
        public int decayPerTurn;

        public List<EffectInstance> onTurnStartEffects;
        public List<EffectInstance> onTurnEndEffects;
    }
}
