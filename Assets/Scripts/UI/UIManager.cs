using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Roguelike.Combat;
using Roguelike.Combat.Turns;

/// <summary>
/// Battle HUD entry point and the battle's IBattlePresenter: binds the character view, spawns one
/// EnemyView per enemy, initializes the hand, and paces the enemy turn - TurnManager waits on the
/// callbacks this hands back before advancing to the next enemy action or the next turn.
/// </summary>
public class UIManager : MonoBehaviour, IBattlePresenter
{
    [SerializeField] private Button turnEndButton;
    [SerializeField] private HandView handView;
    [SerializeField] private CharacterView characterView;

    [Header("Enemies")]
    [SerializeField] private EnemyView enemyViewPrefab;
    [Tooltip("Parent enemy views are spawned under, laid out in a row around its center.")]
    [SerializeField] private RectTransform enemyContainer;
    [SerializeField] private float enemySpacing = 260f;

    [Header("Pacing")]
    [Tooltip("Wait after the player ends their turn before the first enemy acts - lets the hand's " +
             "discard animation play out.")]
    [SerializeField] private float enemyTurnStartDelay = 0.5f;

    private readonly Dictionary<EnemyInstance, EnemyView> enemyViews = new Dictionary<EnemyInstance, EnemyView>();

    /// <summary>Called once by Bootstraps right after the battle's TurnManager is built and before
    /// StartBattle(), so every view is already listening when the opening hand is drawn.</summary>
    public void Initialize(TurnManager manager)
    {
        manager.SetPresenter(this);
        manager.PhaseChanged += HandlePhaseChanged;
        turnEndButton.onClick.AddListener(manager.EndPlayerTurn);

        characterView.Bind(manager.Player);
        SpawnEnemyViews(manager.Enemies);
        handView.Initialize(manager);
    }

    private void HandlePhaseChanged(TurnPhase phase)
    {
        // Greyed out for the whole enemy turn - TurnManager ignores the click then anyway, this just shows it.
        turnEndButton.interactable = phase == TurnPhase.PlayerMain;
    }

    private void SpawnEnemyViews(IReadOnlyList<EnemyInstance> enemies)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            var view = Instantiate(enemyViewPrefab, enemyContainer);
            var rect = (RectTransform)view.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((i - (enemies.Count - 1) * 0.5f) * enemySpacing, 0f);

            view.Bind(enemies[i]);
            enemyViews[enemies[i]] = view;
        }
    }

    // ---- IBattlePresenter ------------------------------------------------------------------------

    public void PresentEnemyTurnStart(Action onReady)
    {
        StartCoroutine(DelayRoutine(enemyTurnStartDelay, onReady));
    }

    public void PresentEnemyAction(EnemyInstance enemy, Action resolve, Action onComplete)
    {
        if (!enemyViews.TryGetValue(enemy, out var view))
        {
            onComplete(); // no view to animate - TurnManager resolves the action itself
            return;
        }

        view.PlayAction(characterView.Body.position, resolve, onComplete);
    }

    private static IEnumerator DelayRoutine(float seconds, Action then)
    {
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
        then();
    }
}
