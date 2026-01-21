using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MenuSpriteItem : MonoBehaviour
{
    public string sceneName;

    [Header("Glow (URP Bloom)")]
    [SerializeField] private bool enableGlow = true;

    // Keep alpha modest; bloom comes from HDR brightness, not alpha.
    [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.25f, 0.35f);

    // KEY: HDR intensity (must be > 1 for bloom to trigger reliably)
    [SerializeField] private float glowHDRIntensity = 3.0f;

    [SerializeField] private float glowScaleMultiplier = 1.15f;

    [Header("Glow Pulse")]
    [SerializeField] private bool pulseGlow = true;
    [SerializeField] private float pulseSpeed = 3.0f;
    [SerializeField] private float pulseIntensityMin = 2.0f;
    [SerializeField] private float pulseIntensityMax = 4.0f;

    [HideInInspector] public Vector3 baseScale;

    private SpriteRenderer mainRenderer;
    private SpriteRenderer glowRenderer;

    private void Awake()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;

        if (enableGlow)
            CreateOrFindGlow();

        SetSelected(false);
    }

    private void CreateOrFindGlow()
    {
        Transform existing = transform.Find("Glow");
        if (existing != null) glowRenderer = existing.GetComponent<SpriteRenderer>();

        if (glowRenderer == null)
        {
            var glowObj = new GameObject("Glow");
            glowObj.transform.SetParent(transform, false);
            glowObj.transform.localPosition = Vector3.zero;

            glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        }

        glowRenderer.sprite = mainRenderer.sprite;
        glowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
        glowRenderer.sortingOrder = mainRenderer.sortingOrder - 1;
        glowRenderer.transform.localScale = Vector3.one * glowScaleMultiplier;

        // Force an Unlit URP 2D sprite shader (more consistent for bloom).
        // If this shader isn't found, it will simply keep default.
        Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (s != null)
        {
            glowRenderer.material = new Material(s);
        }

        ApplyGlowIntensity(glowHDRIntensity);
        glowRenderer.enabled = false;
    }

    private void ApplyGlowIntensity(float intensity)
    {
        // HDR brightness for bloom: color * intensity (>1)
        Color c = glowColor * intensity;
        c.a = glowColor.a; // keep alpha controlled
        glowRenderer.color = c;
    }

    public void SetSelected(bool selected)
    {
        if (!enableGlow) return;

        if (glowRenderer == null) CreateOrFindGlow();

        // keep sprite synced
        if (glowRenderer.sprite != mainRenderer.sprite)
            glowRenderer.sprite = mainRenderer.sprite;

        glowRenderer.enabled = selected;

        if (selected)
            ApplyGlowIntensity(glowHDRIntensity);
    }

    public void UpdateGlowVisual(float unscaledTime)
    {
        if (!enableGlow || glowRenderer == null || !glowRenderer.enabled)
            return;

        glowRenderer.transform.localScale = Vector3.one * glowScaleMultiplier;

        if (!pulseGlow) return;

        float t = (Mathf.Sin(unscaledTime * pulseSpeed) + 1f) * 0.5f;
        float intensity = Mathf.Lerp(pulseIntensityMin, pulseIntensityMax, t);
        ApplyGlowIntensity(intensity);
    }
}
