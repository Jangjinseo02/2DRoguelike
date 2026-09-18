using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Roguelike.Data.Cards;

/// <summary>
/// Visual representation of a single card copy on screen, spawned from a prefab by HandView. A
/// CardDefinition is shared data - the same asset can sit in a hand five times at once - so display
/// state and animation state live here, per on-screen instance, not on the definition itself.
///
/// The card never decides where it sits in the hand: the owner assigns a resting pose (position +
/// tilt) via SetRestingPose and the card animates toward it. Being a prefab, it holds no scene
/// references either - the preview layer is handed in once through Initialize.
///
/// Click flow is two-step: the first click on a resting card only selects it (enlarges it to
/// screen center for preview); a second click while it's already selected confirms the play.
/// Deselecting - by clicking a different card or an empty area - is driven by the owner
/// (HandView): a single CardView deliberately doesn't know about the rest of the hand or the
/// background, so it only exposes Select()/Deselect() for the owner to call.
/// </summary>
[RequireComponent(typeof(Button))]
public class CardView : MonoBehaviour
{
    private enum State
    {
        Resting,  // sitting in (or sliding between) hand slots - clickable
        Arriving, // flying in from the draw pile - clicks ignored until it lands
        Selected, // enlarged at screen center - next click confirms the play
        Leaving,  // played or discarded, flying to the discard pile - clicks ignored for good
    }

    [Header("Display")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image artworkImage;

    [Header("Movement")]
    [SerializeField] private RectTransform rectTransform;
    [Tooltip("Sliding to a new hand slot when the layout changes, and to/from the preview.")]
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float shakeStrength = 12f;
    [SerializeField] private float shakeDuration = 0.25f;

    [Header("Draw (draw pile -> hand)")]
    [SerializeField] private float drawDuration = 0.3f;
    [Tooltip("Ease-out: leaves the pile fast and settles gently into its slot. Default is exactly 1-(1-t)^3.")]
    [SerializeField] private AnimationCurve drawCurve = new AnimationCurve(new Keyframe(0f, 0f, 3f, 3f), new Keyframe(1f, 1f, 0f, 0f));
    [Tooltip("Scale the card starts at on the draw pile, growing to full size as it reaches the hand.")]
    [SerializeField] private float drawStartScale = 0.35f;

    [Header("Selection")]
    [Tooltip("Scale applied while previewing at screen center - big enough to read description text.")]
    [SerializeField] private float selectedScale = 2.4f;
    [SerializeField] private float selectDuration = 0.15f;

    [Header("Use / discard (-> discard pile)")]
    [Tooltip("How far (canvas units) a played card pops up before flying off to the discard pile.")]
    [SerializeField] private float useHopHeight = 60f;
    [SerializeField] private float useHopDuration = 0.12f;
    [Tooltip("Short hang at the top of the hop so it reads as a bounce rather than one straight line.")]
    [SerializeField] private float useHangDuration = 0.05f;
    [SerializeField] private float useFlyDuration = 0.35f;
    [Tooltip("Flight easing. Should start flat and end steep (ease-in) so the card speeds up into the pile. " +
             "Default is exactly t^3.")]
    [SerializeField] private AnimationCurve useFlyCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 3f, 3f));
    [Tooltip("Scale the card shrinks to by the time it reaches the pile - a full-size (or previewed) card " +
             "would otherwise swallow the pile it's landing on.")]
    [SerializeField] private float useEndScale = 0.35f;

    /// <summary>First click on a resting card: it was enlarged for preview, not played.</summary>
    public event Action<CardView> Selected;

    /// <summary>Second click, while already selected: the player is confirming the play.
    /// Carries the bound CardDefinition so a listener never has to ask this view what it's
    /// currently showing.</summary>
    public event Action<CardView, CardDefinition> Clicked;

    public CardDefinition Card { get; private set; }
    public bool IsSelected => state == State.Selected;

    private Button button;
    private RectTransform previewLayer;

    // Clicks are gated on this. Leaving matters most: a CardDefinition is shared, so a stray confirm
    // mid-flight would go through TryPlayCard again and silently play a *different* copy of the
    // same card still in hand. (Not button.interactable - that would tint the card mid-flight.)
    private State state;

    // Assigned by the owner's layout. Arriving/deselect tweens read these live, so a relayout that
    // happens mid-flight (another card drawn or played) just bends the flight to the new slot.
    private Vector2 restingPosition;
    private float restingAngle;

    // Where this card lived before EnterPreview, so ExitPreview can hand it back. Sibling order is
    // not restored here - the owner re-sorts the hand, since it may have changed in the meantime.
    private Transform originalParent;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;

    // Pose (move/tilt/shake) and scale animate independently so confirming a play doesn't get
    // silently cut short by whatever selection tween was still finishing.
    private Coroutine positionAnimation;
    private Coroutine scaleAnimation;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        // Unity forbids SetParent while a GameObject is mid-(de)activation, so this must NOT try to
        // hand the card back from previewLayer - deactivate through Hide() instead, which restores
        // first. Deactivation already stopped the coroutines; just drop the stale handles.
        positionAnimation = null;
        scaleAnimation = null;
    }

    /// <summary>Once, right after the owner instantiates this prefab: scene objects it can't
    /// reference from the prefab asset itself.</summary>
    public void Initialize(RectTransform previewLayer)
    {
        this.previewLayer = previewLayer;
    }

    /// <summary>Returns the card to its owner's container in a clean resting state, then deactivates
    /// it. Always hide a CardView through this rather than SetActive(false) - see OnDisable.</summary>
    public void Hide()
    {
        if (!gameObject.activeSelf) return;

        ResetVisualState();
        gameObject.SetActive(false);
    }

    /// <summary>Fills in this view's visuals from a card's data and resets any leftover animation state.</summary>
    public void Bind(CardDefinition card)
    {
        Card = card;
        ResetVisualState();

        if (nameText != null) nameText.text = card.displayName;
        if (costText != null) costText.text = $"Cost : {card.energyCost}";
        if (descriptionText != null) descriptionText.text = card.description;
        if (artworkImage != null && card.icon != null) artworkImage.sprite = card.icon;
    }

    /// <summary>Where this card belongs in the hand (anchoredPosition in the hand container, z tilt in
    /// degrees). A resting card slides there; an arriving one bends its flight toward it; a selected
    /// one goes there when deselected.</summary>
    public void SetRestingPose(Vector2 position, float angle)
    {
        restingPosition = position;
        restingAngle = angle;

        if (state == State.Resting)
            RestartPosition(MoveRoutine(RestingPosition, RestingAngle, moveDuration, moveCurve, null));
    }

    /// <summary>Card was just drawn: starts small at fromWorld (the draw pile) and flies into its
    /// resting pose, growing to full size. Set the resting pose before calling this.</summary>
    public void PlayDrawAnimation(Vector3 fromWorld)
    {
        state = State.Arriving;
        rectTransform.anchoredPosition = WorldToAnchored(fromWorld);
        SetAngle(0f);
        rectTransform.localScale = Vector3.one * drawStartScale;

        RestartPosition(MoveRoutine(RestingPosition, RestingAngle, drawDuration, drawCurve, () =>
        {
            if (state == State.Arriving) state = State.Resting;
        }));
        RestartScale(ScaleRoutine(Vector3.one, drawDuration, drawCurve));
    }

    /// <summary>
    /// Card was legally played and is leaving the hand: pops up from wherever it is right now, then
    /// accelerates into discardPile and shrinks as it goes. onComplete fires on arrival (the owner
    /// hides/pools it there, which is what makes it vanish at the pile) so the caller never has to
    /// guess a duration. With no discardPile it just flies straight up off its current spot.
    /// </summary>
    public void PlayUseAnimation(RectTransform discardPile, Action onComplete = null)
    {
        // Deliberately stays under previewLayer (if it was previewed) for the whole flight so it
        // draws above the hand and the hand's layout can't tug at it.
        BeginLeaving();
        RestartPosition(FlyToPileRoutine(discardPile, true, 0f, onComplete));
    }

    /// <summary>End-of-turn discard: after delay, flies straight from its hand slot into discardPile
    /// (no hop - it wasn't played). onComplete fires on arrival, as with PlayUseAnimation.</summary>
    public void PlayDiscardAnimation(RectTransform discardPile, float delay, Action onComplete = null)
    {
        BeginLeaving();
        RestartPosition(FlyToPileRoutine(discardPile, false, delay, onComplete));
    }

    /// <summary>Confirm click was rejected (not enough energy, no legal target, ...) - a quick
    /// shake in place. Only moves position, so an enlarged/previewed card keeps its preview size.</summary>
    public void PlayInvalidAnimation()
    {
        RestartPosition(ShakeRoutine());
    }

    /// <summary>Enlarges the card to screen center for preview without playing it. Only from Resting.</summary>
    public void Select()
    {
        if (state != State.Resting) return;
        state = State.Selected;

        EnterPreview();
        RestartPosition(MoveRoutine(() => Vector2.zero, () => 0f, moveDuration, moveCurve, null));
        RestartScale(ScaleRoutine(Vector3.one * selectedScale, selectDuration, moveCurve));
        Selected?.Invoke(this);
    }

    /// <summary>Collapses the preview back into its hand slot without playing the card.</summary>
    public void Deselect()
    {
        if (state != State.Selected) return;
        state = State.Resting;

        ExitPreview();
        RestartPosition(MoveRoutine(RestingPosition, RestingAngle, moveDuration, moveCurve, null));
        RestartScale(ScaleRoutine(Vector3.one, selectDuration, moveCurve));
    }

    private void HandleClick()
    {
        switch (state)
        {
            case State.Selected: Clicked?.Invoke(this, Card); break;
            case State.Resting: Select(); break;
            // Arriving / Leaving: ignore.
        }
    }

    private void BeginLeaving()
    {
        state = State.Leaving;
        if (scaleAnimation != null) { StopCoroutine(scaleAnimation); scaleAnimation = null; }
    }

    private Vector2 RestingPosition() => restingPosition;
    private float RestingAngle() => restingAngle;

    /// <summary>Reparents out of the hand into previewLayer so the hand's own layout can't fight the
    /// preview position, and re-centers its anchors so (0,0) means screen center regardless of how
    /// the hand container itself is anchored.</summary>
    private void EnterPreview()
    {
        if (previewLayer == null)
        {
            transform.SetAsLastSibling(); // no preview layer configured - at least draw on top in place
            return;
        }

        originalParent = rectTransform.parent;
        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;

        Reparent(previewLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        rectTransform.SetAsLastSibling();
    }

    /// <summary>Undoes EnterPreview(): back under the original parent with its original anchors.
    /// Safe to call even if never previewed (no-op).</summary>
    private void ExitPreview()
    {
        if (originalParent == null) return;

        Reparent(originalParent, originalAnchorMin, originalAnchorMax);
        originalParent = null;
    }

    /// <summary>Changes parent and anchors without any visible jump on screen.</summary>
    private void Reparent(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        Vector3 world = rectTransform.position;
        rectTransform.SetParent(parent, true);
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.position = world; // the anchor change shifts the rect; put it back
    }

    /// <summary>A world-space point expressed as this card's anchoredPosition under its current parent.
    /// anchoredPosition and localPosition differ only by a constant (anchor/pivot) offset, so that
    /// offset is carried over rather than re-deriving it from anchors and parent pivot.</summary>
    private Vector2 WorldToAnchored(Vector3 world)
    {
        Vector3 local = rectTransform.parent.InverseTransformPoint(world);
        return rectTransform.anchoredPosition + (Vector2)(local - rectTransform.localPosition);
    }

    private float CurrentAngle() => rectTransform.localEulerAngles.z;
    private void SetAngle(float degrees) => rectTransform.localRotation = Quaternion.Euler(0f, 0f, degrees);

    private void ResetVisualState()
    {
        if (positionAnimation != null) { StopCoroutine(positionAnimation); positionAnimation = null; }
        if (scaleAnimation != null) { StopCoroutine(scaleAnimation); scaleAnimation = null; }

        ExitPreview(); // never leave this orphaned under previewLayer if it gets hidden/rebound mid-preview

        state = State.Resting;
        rectTransform.anchoredPosition = restingPosition;
        SetAngle(restingAngle);
        rectTransform.localScale = Vector3.one;
    }

    private void RestartPosition(IEnumerator routine)
    {
        if (positionAnimation != null)
            StopCoroutine(positionAnimation);
        positionAnimation = StartCoroutine(routine);
    }

    private void RestartScale(IEnumerator routine)
    {
        if (scaleAnimation != null)
            StopCoroutine(scaleAnimation);
        scaleAnimation = StartCoroutine(routine);
    }

    /// <summary>Tweens position and tilt toward targets that are re-read every frame, so a target that
    /// moves mid-tween (relayout) is followed smoothly instead of snapping at the end.</summary>
    private IEnumerator MoveRoutine(Func<Vector2> target, Func<float> targetAngle, float duration,
                                    AnimationCurve curve, Action onComplete)
    {
        Vector2 from = rectTransform.anchoredPosition;
        float fromAngle = CurrentAngle();
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = curve.Evaluate(Mathf.Clamp01(t / duration));
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, target(), k);
            SetAngle(Mathf.LerpAngle(fromAngle, targetAngle(), k));
            yield return null;
        }

        rectTransform.anchoredPosition = target();
        SetAngle(targetAngle());
        positionAnimation = null;
        onComplete?.Invoke();
    }

    private IEnumerator FlyToPileRoutine(RectTransform discardPile, bool hop, float delay, Action onComplete)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float t;
        if (hop)
        {
            // 1) Hop: ease-out pop straight up, in the current parent's canvas units so the height
            //    looks the same whether the card is in the hand or previewed at screen center.
            Vector2 hopFrom = rectTransform.anchoredPosition;
            Vector2 hopTo = hopFrom + Vector2.up * useHopHeight;
            t = 0f;
            while (t < useHopDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / useHopDuration);
                k = 1f - (1f - k) * (1f - k);
                rectTransform.anchoredPosition = Vector2.LerpUnclamped(hopFrom, hopTo, k);
                yield return null;
            }
            rectTransform.anchoredPosition = hopTo;

            // 2) Hang at the apex for a beat.
            if (useHangDuration > 0f)
                yield return new WaitForSeconds(useHangDuration);
        }

        // 3) Fly: accelerate into the pile, straightening out and shrinking on the way. World space,
        //    because the card and the pile generally don't share a parent.
        Vector3 flyFrom = rectTransform.position;
        Vector3 flyTo = discardPile != null
            ? discardPile.TransformPoint(discardPile.rect.center) // pile's visual center, whatever its pivot
            : rectTransform.parent.TransformPoint(rectTransform.localPosition + Vector3.up * 300f);
        Vector3 scaleFrom = rectTransform.localScale;
        Vector3 scaleTo = Vector3.one * useEndScale;
        float angleFrom = CurrentAngle();

        t = 0f;
        while (t < useFlyDuration)
        {
            t += Time.deltaTime;
            float k = useFlyCurve.Evaluate(Mathf.Clamp01(t / useFlyDuration));
            rectTransform.position = Vector3.LerpUnclamped(flyFrom, flyTo, k);
            rectTransform.localScale = Vector3.LerpUnclamped(scaleFrom, scaleTo, k);
            SetAngle(Mathf.LerpAngle(angleFrom, 0f, k));
            yield return null;
        }

        rectTransform.position = flyTo;
        rectTransform.localScale = scaleTo;
        positionAnimation = null;
        onComplete?.Invoke();
    }

    private IEnumerator ShakeRoutine()
    {
        Vector2 basePosition = rectTransform.anchoredPosition;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float falloff = 1f - t / shakeDuration;
            float offset = Mathf.Sin(t * 40f) * shakeStrength * falloff;
            rectTransform.anchoredPosition = basePosition + new Vector2(offset, 0f);
            yield return null;
        }

        rectTransform.anchoredPosition = basePosition;
        positionAnimation = null;
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale, float duration, AnimationCurve curve)
    {
        Vector3 from = rectTransform.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            rectTransform.localScale = Vector3.LerpUnclamped(from, targetScale, curve.Evaluate(Mathf.Clamp01(t / duration)));
            yield return null;
        }

        rectTransform.localScale = targetScale;
        scaleAnimation = null;
    }
}
