using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Roguelike.Combat;

/// <summary>
/// Shared on-screen presentation for anything that fights: name, health bar, block, and the hit
/// reaction (shake + floating number). Purely reactive - it subscribes to its CombatantInstance's
/// Changed/Damaged/Died events and never changes combat state itself.
///
/// Expected hierarchy: this component on a root RectTransform, with the visual parts under a Body
/// child. Hit shake moves Body; anything a subclass does to the root (an enemy's lunge) moves the
/// whole thing, so the two never fight over the same transform.
/// </summary>
public abstract class CombatantView : MonoBehaviour
{
    [Header("Parts")]
    [Tooltip("Visual child that shakes when hit. Defaults to this transform if empty.")]
    [SerializeField] private RectTransform body;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [Tooltip("Image with Image Type = Filled. Its fill amount tweens to current / max health.")]
    [SerializeField] private Image healthFill;
    [Tooltip("Shown only while block > 0.")]
    [SerializeField] private GameObject blockBadge;
    [SerializeField] private TextMeshProUGUI blockText;
    [Tooltip("Optional text whose GameObject is switched on with the damage taken when hit, then switched " +
             "off again after Popup Visible Duration + Popup Fade Duration. Hidden automatically on Awake.")]
    [SerializeField] private TextMeshProUGUI damagePopup;

    [Header("Hit reaction")]
    [SerializeField] private float healthTweenDuration = 0.25f;
    [SerializeField] private float hitShakeStrength = 14f;
    [SerializeField] private float hitShakeDuration = 0.25f;

    [Header("Damage popup")]
    [Tooltip("Seconds the number stays fully visible (while rising) before it starts fading.")]
    [SerializeField] private float popupVisibleDuration = 0.5f;
    [Tooltip("Seconds it takes to fade out before the GameObject is switched off. 0 = switch off instantly.")]
    [SerializeField] private float popupFadeDuration = 0.25f;
    [Tooltip("How far (canvas units) it rises during the visible time. 0 = stays in place.")]
    [SerializeField] private float popupRise = 60f;
    [Tooltip("Hits landing within this many seconds of the previous one add up into one number. A multi-hit " +
             "effect (hitCount > 1) lands all its hits in the same frame, so it always shows its total.")]
    [SerializeField] private float popupComboWindow = 0.2f;

    protected CombatantInstance Model { get; private set; }
    public RectTransform Body => body;

    private Vector2 bodyRestPosition;
    private Vector2 popupRestPosition;
    private Coroutine healthTween;
    private Coroutine shake;
    private Coroutine popup;

    // Running total for the number currently on screen - see popupComboWindow.
    private int comboHealthLost;
    private float lastHitTime = float.NegativeInfinity;

    protected virtual void Awake()
    {
        if (body == null) body = (RectTransform)transform;

        if (damagePopup != null)
        {
            popupRestPosition = damagePopup.rectTransform.anchoredPosition;
            damagePopup.gameObject.SetActive(false);
        }
    }

    protected virtual void OnDestroy() => Unsubscribe();

    /// <summary>Attaches this view to a combatant and snaps every display to its current values.</summary>
    public void Bind(CombatantInstance model)
    {
        Unsubscribe();
        Model = model;
        Model.Changed += HandleChanged;
        Model.Damaged += HandleDamaged;
        Model.Died += HandleDied;

        if (nameText != null) nameText.text = model.DisplayName;
        Refresh(animate: false);
    }

    private void Unsubscribe()
    {
        if (Model == null) return;
        Model.Changed -= HandleChanged;
        Model.Damaged -= HandleDamaged;
        Model.Died -= HandleDied;
    }

    private void HandleChanged() => Refresh(animate: true);

    private void HandleDamaged(int healthLost, int blocked)
    {
        // Capture the rest position only when not already mid-shake (mid-shake, the body is offset).
        // Read now rather than in Awake: a spawned view gets positioned after Awake has run.
        if (shake != null) StopCoroutine(shake);
        else bodyRestPosition = body.anchoredPosition;
        shake = StartCoroutine(ShakeRoutine());

        if (damagePopup != null)
        {
            // Each hit of a multi-hit effect arrives as its own Damaged event - sum them instead of
            // letting the last hit's number overwrite the rest.
            if (popup != null && Time.time - lastHitTime <= popupComboWindow)
                comboHealthLost += healthLost;
            else
                comboHealthLost = healthLost;
            lastHitTime = Time.time;

            if (popup != null) StopCoroutine(popup);
            popup = StartCoroutine(PopupRoutine(comboHealthLost > 0 ? $"-{comboHealthLost}" : "Blocked"));
        }
    }

    private void HandleDied() => OnDied();

    /// <summary>Health reached 0. Default does nothing - subclasses decide (fade out, collapse, ...).</summary>
    protected virtual void OnDied() { }

    /// <summary>Re-reads the model into every display. Subclasses extend this for their own numbers.</summary>
    protected virtual void Refresh(bool animate)
    {
        if (healthText != null) healthText.text = $"{Model.CurrentHealth} / {Model.MaxHealth}";

        if (healthFill != null)
        {
            float target = Model.MaxHealth > 0 ? (float)Model.CurrentHealth / Model.MaxHealth : 0f;
            if (healthTween != null) StopCoroutine(healthTween);
            if (animate && isActiveAndEnabled)
                healthTween = StartCoroutine(FillRoutine(target));
            else
                healthFill.fillAmount = target;
        }

        if (blockBadge != null) blockBadge.SetActive(Model.Block > 0);
        if (blockText != null) blockText.text = Model.Block.ToString();
    }

    private IEnumerator FillRoutine(float target)
    {
        float from = healthFill.fillAmount;
        float t = 0f;
        while (t < healthTweenDuration)
        {
            t += Time.deltaTime;
            healthFill.fillAmount = Mathf.Lerp(from, target, t / healthTweenDuration);
            yield return null;
        }
        healthFill.fillAmount = target;
        healthTween = null;
    }

    private IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < hitShakeDuration)
        {
            t += Time.deltaTime;
            float falloff = 1f - t / hitShakeDuration;
            body.anchoredPosition = bodyRestPosition + new Vector2(Mathf.Sin(t * 50f) * hitShakeStrength * falloff, 0f);
            yield return null;
        }
        body.anchoredPosition = bodyRestPosition;
        shake = null;
    }

    private IEnumerator PopupRoutine(string text)
    {
        var rect = damagePopup.rectTransform;
        Color color = damagePopup.color;

        // Show: back at its start spot, fully opaque (a previous popup may have been cut off mid-fade).
        damagePopup.text = text;
        color.a = 1f;
        damagePopup.color = color;
        rect.anchoredPosition = popupRestPosition;
        damagePopup.gameObject.SetActive(true);

        // Visible: fully readable, easing upward.
        float t = 0f;
        while (t < popupVisibleDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / popupVisibleDuration);
            rect.anchoredPosition = popupRestPosition + Vector2.up * (popupRise * (1f - (1f - k) * (1f - k)));
            yield return null;
        }

        // Fade: stays at the top of the rise while alpha drops.
        t = 0f;
        while (t < popupFadeDuration)
        {
            t += Time.deltaTime;
            color.a = 1f - Mathf.Clamp01(t / popupFadeDuration);
            damagePopup.color = color;
            yield return null;
        }

        // Hide, and leave it reset for next time.
        damagePopup.gameObject.SetActive(false);
        color.a = 1f;
        damagePopup.color = color;
        rect.anchoredPosition = popupRestPosition;
        popup = null;
    }
}
