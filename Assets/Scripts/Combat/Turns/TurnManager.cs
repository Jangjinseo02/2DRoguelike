using System;
using System.Collections.Generic;
using System.Linq;
using Roguelike.Data;
using Roguelike.Data.Cards;
using Roguelike.Combat;

namespace Roguelike.Combat.Turns
{
    /// <summary>
    /// Drives a single battle's turn loop. Plain C# (not a ScriptableObject or MonoBehaviour):
    /// it holds mutable, battle-scoped state that must not persist as a serialized asset and
    /// must not survive past the battle it was created for.
    ///
    /// The player's side is synchronous - the player's input is what paces it. The enemy turn is
    /// continuation-driven: at the start of the enemy turn and around each enemy action it hands
    /// control to an IBattlePresenter and only moves on when the presenter calls back. With no
    /// presenter the continuations run immediately, so the same code path works headless.
    /// </summary>
    public class TurnManager
    {
        public TurnPhase CurrentPhase { get; private set; }
        public int TurnNumber { get; private set; }
        public CharacterInstance Player => player;
        public IReadOnlyList<EnemyInstance> Enemies => enemies;
        public BattleRoster Roster => roster;

        /// <summary>Same as the event channel, for listeners that already hold this TurnManager.</summary>
        public event Action<TurnPhase> PhaseChanged;

        private readonly CharacterInstance player;
        private readonly List<EnemyInstance> enemies;
        private readonly BattleRoster roster;
        private readonly TurnEventChannelSO eventChannel;
        private readonly Random rng;

        private IBattlePresenter presenter;

        // Snapshot of who acts this enemy turn, walked one action at a time across presenter callbacks.
        private readonly List<EnemyInstance> actingEnemies = new List<EnemyInstance>();
        private int nextActingEnemy;

        public TurnManager(CharacterInstance player, List<EnemyInstance> enemies, TurnEventChannelSO eventChannel = null, int? seed = null)
        {
            this.player = player;
            this.enemies = enemies;
            this.eventChannel = eventChannel;
            rng = seed.HasValue ? new Random(seed.Value) : new Random();

            roster = new BattleRoster();
            roster.Allies.Add(player);
            roster.Enemies.AddRange(enemies.Cast<ICombatant>());
        }

        /// <summary>Set before StartBattle(). Null = no waiting (headless / tests).</summary>
        public void SetPresenter(IBattlePresenter battlePresenter) => presenter = battlePresenter;

        public void StartBattle()
        {
            SetPhase(TurnPhase.BattleStart);
            TriggerTraits(TriggerType.OnBattleStart);
            UnityEngine.Debug.Log($"Battle Started: Player {player.Definition.displayName} vs {enemies.Count} enemies.");
            BeginPlayerTurn();
        }

        /// <summary>
        /// The only way presentation should play a card: rejects plays outside the player's main phase
        /// (e.g. mid enemy turn), and ends the battle right away if the card killed the last enemy -
        /// instead of waiting for the player to press end turn on an already-won fight.
        /// </summary>
        public bool TryPlayCard(CardDefinition card, ICombatant target)
        {
            if (CurrentPhase != TurnPhase.PlayerMain) return false;
            if (!player.TryPlayCard(card, roster, target)) return false;

            if (IsBattleOver())
                EndBattle();
            return true;
        }

        /// <summary>Call once the player has finished playing cards for the turn. Ignored outside
        /// PlayerMain, so a stray click while the enemy turn is still playing out can't re-enter it.</summary>
        public void EndPlayerTurn()
        {
            if (CurrentPhase != TurnPhase.PlayerMain) return;

            SetPhase(TurnPhase.PlayerTurnEnd);
            TriggerTraits(TriggerType.OnTurnEnd);
            player.OnTurnEnd(roster, rng);
            UnityEngine.Debug.Log($"Player Turn Ended: {player.Definition.displayName}");

            if (IsBattleOver())
            {
                EndBattle();
                return;
            }

            SetPhase(TurnPhase.EnemyTurnStart);
            if (presenter == null)
                BeginEnemyTurn();
            else
                presenter.PresentEnemyTurnStart(Once(BeginEnemyTurn));
        }

        private void BeginPlayerTurn()
        {
            TurnNumber++;
            SetPhase(TurnPhase.PlayerTurnStart);
            player.OnTurnStart(roster, rng);
            TriggerTraits(TriggerType.OnTurnStart);

            if (IsBattleOver()) // a start-of-turn status tick can be lethal
            {
                EndBattle();
                return;
            }

            SetPhase(TurnPhase.PlayerMain);
            UnityEngine.Debug.Log($"Player Turn Started: {player.Definition.displayName}");
        }

        private void BeginEnemyTurn()
        {
            UnityEngine.Debug.Log($"Enemy Turn Started: {string.Join(", ", AliveEnemies().Select(e => e.DisplayName))}");
            foreach (var enemy in AliveEnemies())
                enemy.OnTurnStart(roster, rng);

            if (IsBattleOver())
            {
                EndBattle();
                return;
            }

            SetPhase(TurnPhase.EnemyMain);
            actingEnemies.Clear();
            actingEnemies.AddRange(AliveEnemies());
            nextActingEnemy = 0;
            RunNextEnemyAction();
        }

        /// <summary>One enemy action per call; re-entered from the presenter's onComplete.</summary>
        private void RunNextEnemyAction()
        {
            // Skip anyone who died since the snapshot was taken (e.g. to a status tick or reflected damage).
            while (nextActingEnemy < actingEnemies.Count && !actingEnemies[nextActingEnemy].IsAlive)
                nextActingEnemy++;

            if (!player.IsAlive || nextActingEnemy >= actingEnemies.Count)
            {
                FinishEnemyTurn();
                return;
            }

            var enemy = actingEnemies[nextActingEnemy++];
            Action resolve = Once(() => enemy.ExecuteTurnAction(roster, rng));
            Action complete = Once(() =>
            {
                resolve(); // no-op if the presenter already resolved at impact
                RunNextEnemyAction();
            });

            if (presenter == null)
                complete();
            else
                presenter.PresentEnemyAction(enemy, resolve, complete);
        }

        private void FinishEnemyTurn()
        {
            SetPhase(TurnPhase.EnemyTurnEnd);
            foreach (var enemy in AliveEnemies())
                enemy.OnTurnEnd(roster, rng);

            if (IsBattleOver())
                EndBattle();
            else
                BeginPlayerTurn();
        }

        private void EndBattle()
        {
            if (CurrentPhase == TurnPhase.BattleEnd) return;

            UnityEngine.Debug.Log($"Battle Ended: {(player.IsAlive ? "Victory" : "Defeat")}");
            TriggerTraits(TriggerType.OnBattleEnd);
            SetPhase(TurnPhase.BattleEnd);
        }

        private bool IsBattleOver() => !player.IsAlive || enemies.TrueForAll(e => !e.IsAlive);

        private IEnumerable<EnemyInstance> AliveEnemies() => enemies.Where(e => e.IsAlive);

        private void TriggerTraits(TriggerType trigger)
        {
            foreach (var trait in player.Traits)
            {
                foreach (var entry in trait.triggeredEffects)
                {
                    if (entry.trigger == trigger)
                        EffectExecutor.Execute(entry.effects, player, roster, null, rng);
                }
            }
        }

        private void SetPhase(TurnPhase phase)
        {
            CurrentPhase = phase;
            eventChannel?.Raise(phase);
            PhaseChanged?.Invoke(phase);
        }

        /// <summary>Wraps a continuation so a presenter calling back twice can't advance the battle twice.</summary>
        private static Action Once(Action action)
        {
            bool invoked = false;
            return () =>
            {
                if (invoked) return;
                invoked = true;
                action();
            };
        }
    }
}
