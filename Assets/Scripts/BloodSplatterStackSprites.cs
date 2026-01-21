using UnityEngine;
using System.Collections;

/// Attach to an empty "BloodOverlay" GameObject. 
/// Assign 6 SpriteRenderers (least -> most intense). 
/// Put the BloodOverlay as a child of your Camera (recommended) so it stays on-screen.
public class BloodSplatterStackSprites : MonoBehaviour
{
    public SpriteRenderer[] layers;      // assign 6 (or fewer/more) SpriteRenderers

    public float comboWindow = 0.5f;

    public float fadeDelay = 0.25f;

    public float fadeDuration = 0.2f;

    public bool useUnscaledTime = true;

    [Header("Test")]
    public bool enableKeyboardTest = true;
    public KeyCode testKey = KeyCode.Return; // Enter

    int currentCount = 0;
    float lastHitTime = -999f;
    Coroutine fadeRoutine;

    void Awake()
    {
        if (layers == null || layers.Length == 0)
        {
            Debug.LogWarning("[BloodSplatterStackSprites] No layers assigned.");
            return;
        }

        // Ensure all layers start fully transparent
        for (int i = 0; i < layers.Length; i++)
            SetAlpha(layers[i], 0f);
    }

    void Update()
    {
        if (enableKeyboardTest && Input.GetKeyDown(testKey))
            AddHit();
    }

    /// Call this from gameplay when you register a consecutive hit.
    public void AddHit()
    {
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;

        // If we waited too long, reset stack first
        if (now - lastHitTime > comboWindow)
            currentCount = 0;

        lastHitTime = now;
        currentCount = Mathf.Min(currentCount + 1, layers.Length);

        // Show exactly 'currentCount' layers at full alpha; hide the rest
        for (int i = 0; i < layers.Length; i++)
        {
            if (!layers[i]) continue;
            SetAlpha(layers[i], i < currentCount ? 1f : 0f);
        }

        // Restart fade timer
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutAfterDelay());
    }

    IEnumerator FadeOutAfterDelay()
    {
        // Wait until fadeDelay after the last hit
        while ((useUnscaledTime ? Time.unscaledTime : Time.time) - lastHitTime < fadeDelay)
            yield return null;

        int n = layers.Length;
        float[] startAlpha = new float[n];
        for (int i = 0; i < n; i++)
            startAlpha[i] = layers[i] ? layers[i].color.a : 0f;

        float t = 0f;
        while (t < fadeDuration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;
            float u = Mathf.Clamp01(t / fadeDuration);

            for (int i = 0; i < n; i++)
            {
                if (!layers[i]) continue;
                float a = Mathf.Lerp(startAlpha[i], 0f, u);
                SetAlpha(layers[i], a);
            }
            yield return null;
        }

        // Fully clear & reset
        for (int i = 0; i < n; i++)
            if (layers[i]) SetAlpha(layers[i], 0f);

        currentCount = 0;
        fadeRoutine = null;
    }

    static void SetAlpha(SpriteRenderer sr, float a)
    {
        if (!sr) return;
        var c = sr.color;
        c.a = a;
        sr.color = c;
    }
}
