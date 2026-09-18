using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Data.Equipment
{

    [System.Serializable]
    public struct MatchEntry
    {
        public AttackType attackType;
        public ArmorType armorType;
        public float damageMultiplier;
    }

    [CreateAssetMenu(menuName = "Roguelike/Equipment/Attack Armor Match Table", fileName = "AttackArmorMatchTable_")]
    public class AttackArmorMatchTable : ScriptableObject
    {
        public List<MatchEntry> matchEntries;


        public float GetMultiplier(AttackType attackType, ArmorType armorType)
        {
            foreach (var entry in matchEntries)
            {
                if (entry.attackType == attackType && entry.armorType == armorType)
                {
                    return entry.damageMultiplier;
                }
            }
            return 1f; // 기본 배율
        }

        private void OnValidate()
        {
            HashSet<(AttackType, ArmorType)> hash = new HashSet<(AttackType, ArmorType)>();

            foreach(var entry in matchEntries)
            {
                // 중복 확인
                if(!hash.Add((entry.attackType, entry.armorType))) Debug.LogWarning($"{entry.attackType} vs {entry.armorType} 에 관한 정보가 중복입니다.");
            }

            // 빠진 조합 확인
            foreach(AttackType attackType in System.Enum.GetValues(typeof(AttackType)))
            {
                foreach(ArmorType armorType in System.Enum.GetValues(typeof(ArmorType)))
                {
                    if(!hash.Contains((attackType, armorType)))
                        Debug.LogWarning($"{attackType} vs {armorType} 에 관한 매치 정보가 존재하지 않습니다.");
                }
            }
        }
    }
    
}