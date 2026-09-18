using System;
using System.Collections.Generic;
using Roguelike.Data.Characters;
using Roguelike.Data.Equipment;
using Roguelike.Data.Cards;

namespace Roguelike.Combat
{
    /// <summary>
    /// Runtime state for the player-controlled character during a run: current HP/energy,
    /// deck piles, equipped items, and held traits. Built once from a CharacterDefinition
    /// at the start of a run; the definition asset itself is never mutated.
    /// </summary>
    public class CharacterInstance : CombatantInstance, IDeckOwner
    {
        public CharacterDefinition Definition { get; }
        public int Energy { get; private set; }
        public int MaxEnergyPerTurn { get; private set; }
        public int CardDrawPerTurn { get; private set; }

        public List<CardDefinition> DrawPile { get; } = new List<CardDefinition>();
        public List<CardDefinition> Hand { get; } = new List<CardDefinition>();
        public List<CardDefinition> DiscardPile { get; } = new List<CardDefinition>();
        public List<CardDefinition> ExhaustPile { get; } = new List<CardDefinition>();

        /// <summary>Raised once per card as it lands in Hand, in draw order - turn-start draws and
        /// mid-turn DrawCards effects alike - so presentation can animate each draw individually
        /// instead of diffing Hand on some phase change.</summary>
        public event Action<CardDefinition> CardDrawn;

        /// <summary>Raised at turn end, after every card left in Hand has moved to the discard pile.</summary>
        public event Action HandDiscarded;

        private readonly Random rng;

        public CharacterInstance(CharacterDefinition definition, int? seed = null)
        {
            Definition = definition;
            rng = seed.HasValue ? new Random(seed.Value) : new Random();

            DisplayName = definition.displayName;
            MaxHealth = definition.maxHealth;
            CurrentHealth = MaxHealth;
            MaxEnergyPerTurn = definition.baseEnergyPerTurn;
            CardDrawPerTurn = definition.baseCardDrawPerTurn;

            foreach (var entry in definition.startingDeck)
                for (int i = 0; i < entry.count; i++)
                    DrawPile.Add(entry.card);
            ShuffleDrawPile();

            foreach (var trait in definition.startingTraits)
                AddTrait(trait);

            foreach (var equipment in definition.startingEquipment)
                Equip(equipment);
        }

        public override void Equip(EquipmentDefinition equipment)
        {
            base.Equip(equipment);
            foreach (var card in equipment.grantedCards)
                DiscardPile.Add(card); // enters the run's card pool on the next reshuffle
        }

        public override void Unequip(EquipmentDefinition equipment)
        {
            foreach (var card in equipment.grantedCards)
            {
                DrawPile.Remove(card);
                Hand.Remove(card);
                DiscardPile.Remove(card);
            }
            base.Unequip(equipment);
        }

        public bool TryPlayCard(CardDefinition card, BattleRoster roster, ICombatant explicitTarget)
        {
            if (card == null || !Hand.Contains(card) || Energy < card.energyCost)
                return false;

            Energy -= card.energyCost;
            RaiseChanged();
            Hand.Remove(card);
            EffectExecutor.Execute(card.effects, this, roster, explicitTarget, rng);
            (card.exhausts ? ExhaustPile : DiscardPile).Add(card);
            return true;
        }

        public void DrawCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (DrawPile.Count == 0)
                {
                    if (DiscardPile.Count == 0) return;
                    DrawPile.AddRange(DiscardPile);
                    DiscardPile.Clear();
                    ShuffleDrawPile();
                }

                int last = DrawPile.Count - 1;
                var card = DrawPile[last];
                DrawPile.RemoveAt(last);
                Hand.Add(card);
                CardDrawn?.Invoke(card);
            }
        }

        public void GainEnergy(int amount)
        {
            Energy += amount;
            RaiseChanged();
        }

        public override void OnTurnStart(BattleRoster roster, Random turnRng)
        {
            base.OnTurnStart(roster, turnRng);
            Energy = MaxEnergyPerTurn;
            RaiseChanged();
            DrawCards(CardDrawPerTurn);
        }

        public override void OnTurnEnd(BattleRoster roster, Random turnRng)
        {
            base.OnTurnEnd(roster, turnRng);
            DiscardPile.AddRange(Hand);
            Hand.Clear();
            HandDiscarded?.Invoke();
        }

        private void ShuffleDrawPile()
        {
            for (int i = DrawPile.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (DrawPile[i], DrawPile[j]) = (DrawPile[j], DrawPile[i]);
            }
        }
    }
}
