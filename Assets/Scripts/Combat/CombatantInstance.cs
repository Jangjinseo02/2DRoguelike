using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Roguelike.Data;
using Roguelike.Data.Equipment;
using Roguelike.Data.Traits;
using Roguelike.Data.Status;

namespace Roguelike.Combat
{
    /// <summary>
    /// Shared runtime state and behaviour for anything that fights: health/block, stat
    /// modifiers, and status effects. CharacterInstance and EnemyInstance both build on this.
    /// </summary>
    public abstract class CombatantInstance : ICombatant
    {
        public string DisplayName { get; protected set; }
        public int MaxHealth { get; protected set; }
        public int CurrentHealth { get; protected set; }
        public int Block { get; protected set; }
        public bool IsAlive => CurrentHealth > 0;

        public AttackType CurrentAttackType => EquippedItems.TryGetValue(EquipmentSlot.Weapon, out var weapon) && weapon != null ? weapon.attackType : AttackType.None;
        public ArmorType CurrentArmorType => EquippedItems.TryGetValue(EquipmentSlot.Armor, out var armor) && armor != null ? armor.armorType : ArmorType.None;

        public Dictionary<EquipmentSlot, EquipmentDefinition> EquippedItems { get; } = new Dictionary<EquipmentSlot, EquipmentDefinition>();
        //public List<TraitDefinition> Traits { get; } = new List<TraitDefinition>();
        private readonly List<ActiveTrait> traitGrants = new List<ActiveTrait>();
        public IEnumerable<TraitDefinition> Traits => traitGrants.Select(g => g.definition).Distinct(); // 체크된 특성들 중 중복만 제거해서 traits list 반환

        protected readonly AttackArmorMatchTable matchTable;
        protected readonly List<ActiveStatModifier> statModifiers = new List<ActiveStatModifier>();
        protected readonly List<ActiveStatusEffect> statusEffects = new List<ActiveStatusEffect>();
        public IReadOnlyList<ActiveStatusEffect> StatusEffects => statusEffects;

        /// <summary>Any displayed number (HP, max HP, block, and subclass values like energy) changed.
        /// Views re-read the properties rather than being told the delta.</summary>
        public event Action Changed;

        /// <summary>A hit landed: (health lost, damage absorbed by block). Raised even when fully blocked,
        /// so presentation can still react to the hit. Raised before Changed.</summary>
        public event Action<int, int> Damaged;

        /// <summary>Health just reached 0. Raised once, after Damaged/Changed for the killing hit.</summary>
        public event Action Died;

        protected CombatantInstance(AttackArmorMatchTable matchTable)
        {
            this.matchTable = matchTable;
        }

        public void AddTrait(TraitDefinition trait, object source)
        {
            bool ready = traitGrants.Exists(g => g.definition == trait);
            //Traits.Add(trait);
            traitGrants.Add(new ActiveTrait() {definition = trait, source = source} ); // 누가 특성을 주는지 체크
            if(!ready) // 리스트 내부에 활성화된 중복 특성이 없다면.
            {
                foreach (var modifier in trait.passiveStatModifiers)
                    ApplyStatModifier(modifier, trait); // 특성 효과를 추가
            }
        }

        public void RemoveTrait(TraitDefinition trait, object source)
        {
            int index = traitGrants.FindIndex(g => g.definition == trait && g.source == source);
            if(index == -1) return; // 출처가 없으면 지울 특성이 없음.
            traitGrants.RemoveAt(index); // source 정보에 대한 특성이 있다면 지운다.
            
            if(!traitGrants.Exists(g => g.definition == trait)) // 리스트 내부에 특성 정보가 없다면.
                RemoveStatModifiersFromSource(trait); // 특성 효과를 지움
        }

        protected void RaiseChanged() => Changed?.Invoke();

        public float GetStat(StatType type)
        {
            float sum = 0f;
            float multiplier = 1f;
            bool hasOverride = false;
            float overrideValue = 0f;

            foreach (var modifier in statModifiers)
            {
                if (modifier.data.statType != type) continue;
                switch (modifier.data.operation)
                {
                    case ModifierOperation.Add:
                        sum += modifier.data.value;
                        break;
                    case ModifierOperation.Multiply:
                        multiplier *= modifier.data.value;
                        break;
                    case ModifierOperation.Override:
                        hasOverride = true;
                        overrideValue = modifier.data.value;
                        break;
                }
            }

            return hasOverride ? overrideValue : sum * multiplier;
        }

        public bool TryConsumePenetration()
        {
            var status = statusEffects.Find(g => g.definition != null && g.definition.grantsPenetration);
            if(status == null) return false;
            
            status.stacks--;
            if(status.stacks == 0) statusEffects.Remove(status);
            return true;
        } 
        

        public virtual void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive) return;
            int blocked = Mathf.Min(Block, amount);
            int healthBefore = CurrentHealth;
            Block -= blocked;
            CurrentHealth = Mathf.Max(0, CurrentHealth - (amount - blocked));

            Damaged?.Invoke(healthBefore - CurrentHealth, blocked);
            RaiseChanged();
            if (!IsAlive)
                Died?.Invoke();
        }

        public virtual void TakeDamage(int amount, AttackType attackType, bool penetrates)
        {
            float result = amount;
            
            //상성 배율 계산
            float multiplier = matchTable != null ? matchTable.GetMultiplier(attackType, CurrentArmorType) : 1f;
            // 관통 시 유리한 배율은 그대로, 불리한 배율은 1배로
            if(penetrates) multiplier = Mathf.Max(multiplier, 1f); 
            
            // 데미지 계산(데미지 * 배율)
            amount = Mathf.RoundToInt(result * multiplier);

            // 데미지 할당(방어도와 hp 처리는 기본형에게)
            TakeDamage(amount);
        }

        public virtual void GainBlock(int amount)
        {
            Block += Mathf.Max(0, amount);
            RaiseChanged();
        }

        public virtual void Heal(int amount)
        {
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + Mathf.Max(0, amount));
            RaiseChanged();
        }

        public virtual void ModifyMaxHealth(int amount)
        {
            MaxHealth = Mathf.Max(1, MaxHealth + amount);
            if (amount > 0) CurrentHealth += amount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
            RaiseChanged();
        }

        public void ApplyStatModifier(StatModifier modifier, object source) =>
            statModifiers.Add(new ActiveStatModifier { data = modifier, source = source });

        public void RemoveStatModifiersFromSource(object source) =>
            statModifiers.RemoveAll(m => m.source == source);

        public void ApplyStatus(StatusEffectDefinition status, int stacks)
        {
            if (status == null || stacks <= 0) return;
            var existing = statusEffects.Find(s => s.definition == status);
            if (existing != null)
            {
                if (status.stackBehavior == StatusStackBehavior.Intensity)
                    existing.stacks += stacks;
                else
                    existing.stacks = Mathf.Max(existing.stacks, stacks);
            }
            else
            {
                statusEffects.Add(new ActiveStatusEffect { definition = status, stacks = stacks });
            }
        }

        public virtual void Equip(EquipmentDefinition equipment)
        {
            if (EquippedItems.TryGetValue(equipment.slot, out var previous))
                Unequip(previous);

            EquippedItems[equipment.slot] = equipment;
            foreach (var modifier in equipment.statModifiers)
                ApplyStatModifier(modifier, equipment);
            foreach (var trait in equipment.grantedTraits)
                AddTrait(trait, equipment);
        }

        public virtual void Unequip(EquipmentDefinition equipment)
        {
            RemoveStatModifiersFromSource(equipment);
            foreach (var trait in equipment.grantedTraits)
                RemoveTrait(trait, equipment);

            EquippedItems.Remove(equipment.slot);
        }

        /// <summary>Called by TurnManager at the start of this combatant's turn.</summary>
        public virtual void OnTurnStart(BattleRoster roster, System.Random rng)
        {
            Block = 0; // classic "block decays on your own turn" rule - adjust to change the convention
            RaiseChanged();
            foreach (var status in statusEffects.ToArray())
                EffectExecutor.Execute(status.definition.onTurnStartEffects, this, roster, null, rng, status.stacks);
        }

        /// <summary>Called by TurnManager at the end of this combatant's turn.</summary>
        public virtual void OnTurnEnd(BattleRoster roster, System.Random rng)
        {
            foreach (var status in statusEffects.ToArray())
                EffectExecutor.Execute(status.definition.onTurnEndEffects, this, roster, null, rng, status.stacks);

            for (int i = statusEffects.Count - 1; i >= 0; i--)
            {
                var status = statusEffects[i];
                if (status.definition.decayPerTurn > 0)
                    status.stacks -= status.definition.decayPerTurn;
                if (status.stacks <= 0)
                    statusEffects.RemoveAt(i);
            }
        }
    }
}
