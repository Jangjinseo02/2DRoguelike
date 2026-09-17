using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// One enemy on screen, spawned from a prefab by UIManager. Adds the enemy's action animation on top
/// of CombatantView: pull back, lunge toward the target, resolve the action at the moment of impact,
/// return, then report completion so TurnManager can move on to the next enemy.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class EnemyView : CombatantView
{
    [Header("Action")]
    [Tooltip("How far (canvas units) the enemy pulls back before lunging.")]
    [SerializeField] private float windUpDistance = 25f;
    [SerializeField] private float windUpDuration = 0.18f;
    [Tooltip("Fraction of the way to the target the lunge covers (0 = in place, 1 = all the way).")]
    [Range(0f, 1f)]
    [SerializeField] private float lungeReach = 0.35f;
    [SerializeField] private float lungeDuration = 0.1f;
    [Tooltip("Hold at the point of impact, while the target's hit reaction starts.")]
    [SerializeField] private float impactHold = 0.12f;
    [SerializeField] private float returnDuration = 0.2f;
    [Tooltip("Pause after returning before the next enemy (or the player's turn) begins.")]
    [SerializeField] private float afterActionDelay = 0.25f;

    [Header("Death")]
    [SerializeField] private float deathFadeDuration = 0.4f;

    private RectTransform root;
    private CanvasGroup canvasGroup;

    protected override void Awake()
    {
        base.Awake();
        root = (RectTransform)transform;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// Plays one action aimed at targetWorld (e.g. the player view's position). resolve is invoked at
    /// impact - that's when damage is actually applied - and onComplete once the enemy is back in place.
    /// </summary>
    public void PlayAction(Vector3 targetWorld, Action resolve, Action onComplete)
    {
        StartCoroutine(ActionRoutine(targetWorld, resolve, onComplete));
    }

    protected override void OnDied()
    {
        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator ActionRoutine(Vector3 targetWorld, Action resolve, Action onComplete)
    {
        Vector2 rest = root.anchoredPosition;

        // Direction toward the target in the root's parent space, so distances are canvas units.
        Vector3 localTarget = root.parent.InverseTransformPoint(targetWorld);
        Vector2 toTarget = (Vector2)(localTarget - root.localPosition);
        Vector2 direction = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector2.down;

        Vector2 windUp = rest - direction * windUpDistance;
        Vector2 impact = rest + toTarget * lungeReach;

        yield return Move(rest, windUp, windUpDuration, EaseOut);
        yield return Move(windUp, impact, lungeDuration, EaseIn);

        resolve?.Invoke();
        if (impactHold > 0f) yield return new WaitForSeconds(impactHold);

        yield return Move(impact, rest, returnDuration, EaseOut);
        if (afterActionDelay > 0f) yield return new WaitForSeconds(afterActionDelay);

        onComplete?.Invoke();
    }

    private IEnumerator Move(Vector2 from, Vector2 to, float duration, Func<float, float> ease)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            root.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        root.anchoredPosition = to;
    }

    private IEnumerator FadeOutRoutine()
    {
        canvasGroup.blocksRaycasts = false;
        float t = 0f;
        while (t < deathFadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - t / deathFadeDuration;
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    private static float EaseOut(float k) => 1f - (1f - k) * (1f - k);
    private static float EaseIn(float k) => k * k;
}
