using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Roguelike.Combat;
using Roguelike.Combat.Turns;
using Roguelike.Data.Cards;

/// <summary>
/// The player's on-screen hand. Spawns pooled CardView prefab instances as cards are drawn, lays
/// them out as a shallow fan (or a straight row), owns card selection, and plays the draw / play /
/// end-of-turn discard motion.
///
/// Driven by CharacterInstance.CardDrawn / HandDiscarded rather than re-reading Hand on a phase
/// change, so a card drawn mid-turn by an effect shows up immediately. A whole turn's draw arrives
/// in one synchronous burst, so draws go through a small step queue that spaces them out and waits
/// for the turn-end discard to finish.
///
/// All scene references (piles, preview layer, backdrop) live here rather than on the card prefab -
/// a prefab asset can't reference scene objects, so each spawned CardView gets them via Initialize.
/// </summary>
public class HandView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardView cardPrefab;
    [Tooltip("Parent the hand's cards live under. Layout positions are relative to its center.")]
    [SerializeField] private RectTransform handContainer;
    [Tooltip("Where drawn cards fly in from. Leave empty to have them come up from below the hand.")]
    [SerializeField] private RectTransform drawPile;
    [Tooltip("Where played and end-of-turn discarded cards fly to and vanish.")]
    [SerializeField] private RectTransform discardPile;
    [Tooltip("Full-screen RectTransform a selected card is reparented into while previewing, so the " +
             "hand's layout can't pull it back. Leave empty to just scale up in place instead.")]
    [SerializeField] private RectTransform previewLayer;
    [Tooltip("Full-screen transparent button behind the preview layer. Shown only while a card " +
             "is selected; clicking it (i.e. clicking empty space) deselects the current card.")]
    [SerializeField] private Button selectionBackdrop;

    [Header("Layout (set Fan Angle and Arc Height to 0 for a straight row)")]
    [Tooltip("Center-to-center distance between neighbouring cards. Less than the card width overlaps them.")]
    [SerializeField] private float cardSpacing = 140f;
    [Tooltip("The hand never grows wider than this - past it, spacing shrinks and cards overlap more.")]
    [SerializeField] private float maxHandWidth = 900f;
    [Tooltip("Tilt difference (degrees) between neighbouring cards.")]
    [SerializeField] private float fanAngle = 3f;
    [Tooltip("Cap on the tilt difference between the two outermost cards, however many are held.")]
    [SerializeField] private float maxFanAngle = 20f;
    [Tooltip("How far below the center a card sitting at the edge of Max Hand Width drops.")]
    [SerializeField] private float arcHeight = 30f;

    [Header("Sequencing")]
    [Tooltip("Gap between consecutive cards leaving the draw pile.")]
    [SerializeField] private float drawInterval = 0.1f;
    [Tooltip("Gap between consecutive cards flying to the discard pile at turn end.")]
    [SerializeField] private float discardStagger = 0.04f;
    [Tooltip("Pause after the last turn-end discard starts before the next turn's draws begin.")]
    [SerializeField] private float afterDiscardDelay = 0.35f;

    private TurnManager turnManager;
    private CharacterInstance player;
    private CardView selectedCard;

    // Cards currently shown in the hand, in hand order. Excludes cards flying off to the discard pile.
    private readonly List<CardView> handViews = new List<CardView>();
    private readonly Stack<CardView> pool = new Stack<CardView>();

    private readonly Queue<Func<IEnumerator>> pendingSteps = new Queue<Func<IEnumerator>>();
    private bool isRunningSteps;

    /// <summary>Called once (via UIManager) before TurnManager.StartBattle(), so the opening hand's
    /// draws are already being listened to.</summary>
    public void Initialize(TurnManager manager)
    {
        turnManager = manager;
        player = manager.Player;
        player.CardDrawn += HandleCardDrawn;
        player.HandDiscarded += HandleHandDiscarded;

        if (selectionBackdrop != null)
        {
            selectionBackdrop.onClick.AddListener(DeselectCurrentCard);
            selectionBackdrop.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (player == null) return;
        player.CardDrawn -= HandleCardDrawn;
        player.HandDiscarded -= HandleHandDiscarded;
    }

    private void OnDisable()
    {
        // Deactivation killed the step runner mid-step; let the next Enqueue start a fresh one.
        isRunningSteps = false;
    }

    // ---- Combat events -------------------------------------------------------------------------

    private void HandleCardDrawn(CardDefinition card) => Enqueue(() => DrawStep(card));

    private void HandleHandDiscarded()
    {
        // Draws still waiting their turn in the queue were for cards that just went to the discard
        // pile along with the rest of the hand - they never get their own fly-in.
        pendingSteps.Clear();
        ClearSelection();

        for (int i = 0; i < handViews.Count; i++)
        {
            var view = handViews[i];
            view.PlayDiscardAnimation(discardPile, i * discardStagger, () => Release(view));
        }

        float settle = handViews.Count * discardStagger + afterDiscardDelay;
        handViews.Clear();
        Enqueue(() => Wait(settle));
    }

    // ---- Card input ----------------------------------------------------------------------------

    private void HandleCardSelected(CardView view)
    {
        // Only one card previewed/enlarged at a time - collapse whichever one was selected before.
        if (selectedCard != null && selectedCard != view)
        {
            selectedCard.Deselect();
            RefreshSiblingOrder();
        }

        selectedCard = view;
        if (selectionBackdrop != null)
            selectionBackdrop.gameObject.SetActive(true);
    }

    /// <summary>Wired to selectionBackdrop's click - i.e. the player tapped empty space.</summary>
    private void DeselectCurrentCard()
    {
        if (selectedCard == null) return;
        selectedCard.Deselect();
        ClearSelection();
        RefreshSiblingOrder();
    }

    /// <summary>Clears the selection bookkeeping without animating anything back - use when the
    /// selected CardView is already being moved away by other means (played, discarded).</summary>
    private void ClearSelection()
    {
        selectedCard = null;
        if (selectionBackdrop != null)
            selectionBackdrop.gameObject.SetActive(false);
    }

    private void HandleCardClicked(CardView view, CardDefinition card)
    {
        int index = handViews.IndexOf(view);
        if (index < 0) return;

        // Take the view out of the hand before playing: the card's effects can draw (DrawCardsEffect),
        // and that draw's layout pass should already see the hand without the card being played.
        handViews.RemoveAt(index);

        // TODO: explicit target selection - always the first living enemy for now (see CLAUDE.md Known gaps).
        ICombatant target = null;
        foreach (var enemy in turnManager.Enemies)
        {
            if (enemy.IsAlive) { target = enemy; break; }
        }

        // Through TurnManager, not player.TryPlayCard: it rejects plays outside PlayerMain and ends
        // the battle on the spot if this card kills the last enemy.
        if (turnManager.TryPlayCard(card, target))
        {
            if (selectedCard == view) ClearSelection();
            Relayout();
            view.PlayUseAnimation(discardPile, () => Release(view));
        }
        else
        {
            // Rejected (not enough energy, ...) without touching Hand - put it back where it was.
            handViews.Insert(index, view);
            view.PlayInvalidAnimation();
        }
    }

    // ---- Layout ----------------------------------------------------------------------------------

    /// <summary>Assigns every hand card its resting pose for the current hand size.</summary>
    private void Relayout()
    {
        int count = handViews.Count;
        float spacing = count > 1 ? Mathf.Min(cardSpacing, maxHandWidth / (count - 1)) : 0f;
        float anglePerCard = count > 1 ? Mathf.Min(fanAngle, maxFanAngle / (count - 1)) : 0f;
        float halfWidth = maxHandWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float offset = i - (count - 1) * 0.5f; // ..., -1, 0, +1, ... around the middle card
            float x = offset * spacing;
            float edge = halfWidth > 0f ? x / halfWidth : 0f;

            // Left of center tilts counter-clockwise (positive z), right tilts clockwise, and cards
            // drop along a parabola toward the edges - an arc rather than a straight line.
            handViews[i].SetRestingPose(new Vector2(x, -arcHeight * edge * edge), -offset * anglePerCard);
        }

        RefreshSiblingOrder();
    }

    /// <summary>Right-hand cards draw over left-hand ones where they overlap. Skips a card that's
    /// currently reparented into the preview layer.</summary>
    private void RefreshSiblingOrder()
    {
        foreach (var view in handViews)
        {
            if (view.transform.parent == handContainer)
                view.transform.SetAsLastSibling();
        }
    }

    private Vector3 DrawOrigin()
    {
        return drawPile != null
            ? drawPile.TransformPoint(drawPile.rect.center)
            : handContainer.TransformPoint(new Vector3(0f, -400f, 0f));
    }

    // ---- Step queue ------------------------------------------------------------------------------

    private void Enqueue(Func<IEnumerator> step)
    {
        pendingSteps.Enqueue(step);
        if (isRunningSteps) return;

        // Set before StartCoroutine: it runs synchronously up to the first yield, and must not see
        // itself as "not running" and get started twice.
        isRunningSteps = true;
        StartCoroutine(RunSteps());
    }

    private IEnumerator RunSteps()
    {
        while (pendingSteps.Count > 0)
            yield return StartCoroutine(pendingSteps.Dequeue()());
        isRunningSteps = false;
    }

    private IEnumerator DrawStep(CardDefinition card)
    {
        var view = Rent();
        view.Bind(card);
        handViews.Add(view);
        Relayout();
        view.PlayDrawAnimation(DrawOrigin());
        yield return new WaitForSeconds(drawInterval);
    }

    private static IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    // ---- Pool ------------------------------------------------------------------------------------

    private CardView Rent()
    {
        var view = pool.Count > 0 ? pool.Pop() : CreateView();
        view.gameObject.SetActive(true);
        return view;
    }

    private CardView CreateView()
    {
        var view = Instantiate(cardPrefab, handContainer);

        // Layout positions are relative to the container's center, whatever anchors the prefab was saved with.
        var rect = (RectTransform)view.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);

        view.Initialize(previewLayer);
        view.Selected += HandleCardSelected;
        view.Clicked += HandleCardClicked;
        return view;
    }

    private void Release(CardView view)
    {
        view.Hide();
        pool.Push(view);
    }
}
