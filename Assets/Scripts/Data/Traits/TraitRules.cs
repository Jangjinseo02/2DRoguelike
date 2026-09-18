using UnityEngine;

namespace Roguelike.Data.Traits
{
    [CreateAssetMenu(menuName = "Roguelike/Traits/Trait Rules", fileName = "TraitRules_")]
    public class TraitRules : ScriptableObject
    {
        [Min(0), Tooltip("기본 이점 특성 칸 수")]
        public int baseAdvantageSlots = 3;

        [Min(0), Tooltip("시작 시 선택할 수 있는 포인트 수")]
        public int baseSelectionPoint = 5;
    }
}
