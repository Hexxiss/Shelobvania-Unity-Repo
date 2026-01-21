using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FadeOnPlayerProximity2D : MonoBehaviour
{
    [Header("Behavior")]
    [Tooltip("Collider tag that triggers the fade.")]
    public string triggerTag = "Player";

    [Tooltip("Alpha to fade to while the player is inside.")]
    [Range(0f, 1f)] public float fadedAlpha = 0.3f;

    [Tooltip("Seconds to fade in/out.")]
    [Min(0f)] public float fadeDuration = 0.15f;

    [Tooltip("Affects this object and all child SpriteRenderers.")]
    public bool includeChildren = true;

    [Tooltip("Use unscaled time (ignores pauses/timeScale).")]
    public bool useUnscaledTime = false;

    // Internal
    SpriteRenderer[] _renders;
    float[] _baseAlphas;
    Coroutine _currentRoutine;
    int _insideCount = 0; // supports multiple overlaps without flicker

    void Awake()
    {
        _renders = includeChildren
            ? GetComponentsInChildren<SpriteRenderer>(includeInactive: false)
            : new[] { GetComponent<SpriteRenderer>() };

        // Filter nulls (in case no SpriteRenderer on root)
        _renders = System.Array.FindAll(_renders, r => r != null);

        _baseAlphas = new float[_renders.Length];
        for (int i = 0; i < _renders.Length; i++)
            _baseAlphas[i] = _renders[i].color.a;

        // Ensure our Collider2D is a trigger
        var col = GetComponent<Collider2D>();
        if (col && !col.isTrigger)
            Debug.LogWarning($"{name}: Collider2D is not set as Trigger; set isTrigger = true for FadeOnPlayerProximity2D.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(triggerTag)) return;
        _insideCount++;
        if (_insideCount == 1) StartFade(toFaded: true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(triggerTag)) return;
        _insideCount = Mathf.Max(0, _insideCount - 1);
        if (_insideCount == 0) StartFade(toFaded: false);
    }

    void StartFade(bool toFaded)
    {
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentRoutine = StartCoroutine(FadeRoutine(toFaded));
    }

    IEnumerator FadeRoutine(bool toFaded)
    {
        if (_renders.Length == 0) yield break;

        // Capture start alphas so mid-fade reversals are smooth
        float[] startAlphas = new float[_renders.Length];
        for (int i = 0; i < _renders.Length; i++)
            startAlphas[i] = _renders[i].color.a;

        float t = 0f;
        float dur = Mathf.Max(0.0001f, fadeDuration);

        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            // Smoothstep easing
            float e = u * u * (3f - 2f * u);

            for (int i = 0; i < _renders.Length; i++)
            {
                var c = _renders[i].color;
                float target = toFaded ? fadedAlpha : _baseAlphas[i];
                c.a = Mathf.Lerp(startAlphas[i], target, e);
                _renders[i].color = c;
            }

            yield return null;
        }

        // Ensure exact final
        for (int i = 0; i < _renders.Length; i++)
        {
            var c = _renders[i].color;
            c.a = toFaded ? fadedAlpha : _baseAlphas[i];
            _renders[i].color = c;
        }

        _currentRoutine = null;
    }
}
