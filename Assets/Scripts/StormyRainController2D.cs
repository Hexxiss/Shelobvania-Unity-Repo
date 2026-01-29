using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class StormyRainWind2D : MonoBehaviour
{
    [Header("Wind Direction")]
    [Tooltip("1 = wind pushes rain right, -1 = left")]
    [Range(-1f, 1f)] public float windSign = 1f;

    [Header("Storm Base Wind (constant lean)")]
    [Tooltip("Constant horizontal wind (units/sec). This sets the default diagonal angle.")]
    public float baseWindX = 6.0f;

    [Tooltip("Optional vertical contribution (usually 0). Keep small if used.")]
    public float baseWindY = 0.0f;

    [Header("Gust Bursts (storm cadence)")]
    [Tooltip("Max extra horizontal speed during gust peaks (units/sec).")]
    public float gustPeakX = 8.0f;

    [Tooltip("Optional: makes gusts feel like 'sheets' by slightly increasing downward speed.")]
    public float gustPeakY = 0.5f;

    [Tooltip("How often gust *events* come and go (lower = longer gusts).")]
    public float gustCadence = 0.12f; // stormy: ~0.08–0.18

    [Tooltip("How chaotic gust direction is within a storm (higher = more variation).")]
    public float gustDirectionFrequency = 0.25f;

    [Tooltip("How quickly wind responds (higher = snappier gust edges).")]
    public float response = 8.0f;

    [Header("Micro Turbulence (small jitter, not random snapping)")]
    public float microAmpX = 0.35f;
    public float microFreq = 3.0f;

    [Header("Clamp (safety)")]
    [Tooltip("Caps total horizontal wind so it doesn't go absurd.")]
    public float maxWindX = 18.0f;

    private ParticleSystem ps;
    private ParticleSystem.VelocityOverLifetimeModule vel;

    private float seedA, seedB, seedC;
    private float curX, curY;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        vel = ps.velocityOverLifetime;
        vel.enabled = true;

        seedA = Random.value * 1000f;
        seedB = Random.value * 1000f + 33.3f;
        seedC = Random.value * 1000f + 77.7f;
    }

    void Update()
    {
        float t = Time.time;

        // 1) Gust envelope: 0..1 (controls when gusts "happen")
        //    Make it spikier (more storm-like) with a power curve.
        float env = Mathf.PerlinNoise(seedA, t * gustCadence);     // 0..1
        env = Mathf.Pow(env, 2.2f);                                // bias toward calmer with occasional strong peaks

        // 2) Gust direction variation: -1..1
        float dirNoise = Mathf.PerlinNoise(seedB, t * gustDirectionFrequency) * 2f - 1f;

        // 3) Micro turbulence: small fast wobble (keeps sprite rain from looking too parallel)
        float micro = Mathf.Sin(t * microFreq) * microAmpX;

        // Target gust contributions (scaled by envelope)
        float gustX = dirNoise * gustPeakX * env;
        float gustY = gustPeakY * env;

        float targetX = windSign * (baseWindX + gustX + micro);
        float targetY = baseWindY - gustY; // negative makes it fall slightly faster during gusts

        // Clamp total X for sanity
        targetX = Mathf.Clamp(targetX, -maxWindX, maxWindX);

        // Smooth response (prevents visible snapping)
        float k = 1f - Mathf.Exp(-response * Time.deltaTime);
        curX = Mathf.Lerp(curX, targetX, k);
        curY = Mathf.Lerp(curY, targetY, k);

        // Apply constant velocity over lifetime
        vel.x = new ParticleSystem.MinMaxCurve(curX);
        vel.y = new ParticleSystem.MinMaxCurve(curY);
        vel.z = new ParticleSystem.MinMaxCurve(0f);
    }
}
