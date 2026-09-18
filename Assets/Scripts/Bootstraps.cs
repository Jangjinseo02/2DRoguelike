using System.Collections.Generic;
using UnityEngine;
using Roguelike.Combat.Turns;
using Roguelike.Data.Characters;
using Roguelike.Data.Enemies;
using Roguelike.Combat;
using Roguelike.Data.Equipment;

public class Bootstraps : MonoBehaviour
{
    [SerializeField] private TurnEventChannelSO turnEventChannel;
    [SerializeField] private AttackArmorMatchTable matchTable;
    [SerializeField] private CharacterDefinition characterDefinition;
    [SerializeField] private EnemyDefinition enemyDefinition;
    [SerializeField] private UIManager uiManager;

    private TurnManager turnManager;

    private void Awake()
    {
        CharacterInstance characterInstance = new CharacterInstance(characterDefinition, matchTable);
        EnemyInstance enemyInstance = new EnemyInstance(enemyDefinition, matchTable);

        List<EnemyInstance> enemies = new List<EnemyInstance> { enemyInstance };
        turnManager = new TurnManager(characterInstance, enemies, turnEventChannel);
    }

    private void Start()
    {
        uiManager.Initialize(turnManager);
        turnManager.StartBattle();
    }
}