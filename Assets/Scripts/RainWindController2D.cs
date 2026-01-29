using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RainWindController2D : MonoBehaviour
{
    [Header("Wind Direction")]
    [Tooltip("1 = wind pushes rain to the right, -1 = to the left")]
    [Range(-1f, 1f)] public float windSign = 1f;

    [Header("Base Wind")]
    [Tooltip("Base horizontal wind speed applied to particles (units/sec).")]
    public float baseWindX = 2.0f;

    [Tooltip("Optional: add/subtract a bit of vertical speed during gusts (usually keep small).")]
    public float baseWindY = 0.0f;

    [Header("Gusts (Smooth Variation)")]
    [Tooltip("Max additional horizontal wind from gusts (units/sec).")]
    public float gustAmplitudeX = 3.0f;

    [Tooltip("Optional: vertical gust contribution (keep small).")]
    public float gustAmplitudeY = 0.0f;

    [Tooltip("How quickly gusts change. Lower = slower drift, higher = more lively.")]
    public float gustFrequency = 0.25f;

    [Tooltip("How quickly we interpolate to the new gust value (prevents snapping).")]
    public float gustSmoothing = 6.0f;

    [Header("Micro Variation (tiny wobble)")]
    [Tooltip("Small fast variation so the rain isn't perfectly parallel.")]
    public float microAmplitudeX = 0.25f;

    public float microFrequency = 2.0f;

    private ParticleSystem ps;
    private ParticleSystem.VelocityOverLifetimeModule vel;

    private float noiseSeed;
    private float currentGustX;
    private float currentGustY;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        vel = ps.velocityOverLifetime;
        vel.enabled = true;

        noiseSeed = Random.value * 1000f;
    }

    void Update()
    {
        float t = Time.time;

        // Smooth gust using Perlin noise (0..1) mapped to (-1..1)
        float nX = Mathf.PerlinNoise(noiseSeed, t * gustFrequency) * 2f - 1f;
        float nY = Mathf.PerlinNoise(noiseSeed + 13.37f, t * gustFrequency) * 2f - 1f;

        float targetGustX = nX * gustAmplitudeX;
        float targetGustY = nY * gustAmplitudeY;

        // Smoothly approach target gust (avoids jitter)
        currentGustX = Mathf.Lerp(currentGustX, targetGustX, 1f - Mathf.Exp(-gustSmoothing * Time.deltaTime));
        currentGustY = Mathf.Lerp(currentGustY, targetGustY, 1f - Mathf.Exp(-gustSmoothing * Time.deltaTime));

        // Tiny faster wobble
        float microX = Mathf.Sin(t * microFrequency) * microAmplitudeX;

        float windX = windSign * (baseWindX + currentGustX + microX);
        float windY = baseWindY + currentGustY;

        // Apply as constant velocity over lifetime (so every particle is pushed)
        // For 2D (XY plane), keep Z at 0.
        vel.x = new ParticleSystem.MinMaxCurve(windX);
        vel.y = new ParticleSystem.MinMaxCurve(windY);
        vel.z = new ParticleSystem.MinMaxCurve(0f);
    }
}
