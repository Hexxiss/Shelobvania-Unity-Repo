using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class Light2DPulse : MonoBehaviour
{
    [Header("Target (auto-fills if on same GameObject)")]
    public Light2D light2D;

    [Header("Pulse Intensity")]
    [Tooltip("Base intensity around which the light will pulse.")]
    public float baseIntensity = 1.0f;

    [Tooltip("How far above/below baseIntensity to swing.")]
    public float amplitude = 0.25f;

    [Tooltip("Cycles per second (e.g., 0.5 = one full pulse every 2 seconds).")]
    public float frequency = 0.5f;

    [Tooltip("Phase offset in degrees (start point in the cycle).")]
    [Range(0f, 360f)]
    public float phaseDegrees = 0f;

    [Header("Waveform (optional)")]
    [Tooltip("If provided, overrides sine with a custom 0..1 curve (evaluated over one cycle).")]
    public AnimationCurve customCurve; // leave empty to use sine

    [Header("Jitter (optional, subtle randomness)")]
    [Tooltip("Randomly varies amplitude by up to this fraction (e.g., 0.1 = ±10%).")]
    [Range(0f, 0.5f)]
    public float amplitudeJitter = 0.05f;

    [Tooltip("Randomly varies frequency by up to this fraction (e.g., 0.05 = ±5%).")]
    [Range(0f, 0.5f)]
    public float frequencyJitter = 0.02f;

    [Tooltip("Re-seed jitter every N seconds (0 = never).")]
    public float jitterReseedSeconds = 2.5f;

    [Header("Timing")]
    public bool useUnscaledTime = false;

    float _baseIntensityCached;
    float _ampNow;
    float _freqNow;
    float _phaseRad;
    float _jitterTimer;

    void Reset()
    {
        light2D = GetComponent<Light2D>();
    }

    void OnEnable()
    {
        if (!light2D) light2D = GetComponent<Light2D>();
        if (!light2D)
        {
            Debug.LogWarning("[Light2DPulse] No Light2D found.");
            enabled = false; return;
        }

        // Initialize
        _baseIntensityCached = baseIntensity <= 0f ? light2D.intensity : baseIntensity;
        _ampNow = Mathf.Max(0f, amplitude);
        _freqNow = Mathf.Max(0f, frequency);
        _phaseRad = phaseDegrees * Mathf.Deg2Rad;

        // First jitter
        ApplyJitter();
    }

    void Update()
    {
        if (!light2D) return;

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;

        // Reseed jitter periodically
        if (jitterReseedSeconds > 0f)
        {
            _jitterTimer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (_jitterTimer >= jitterReseedSeconds)
            {
                _jitterTimer = 0f;
                ApplyJitter();
            }
        }

        float cycle; // 0..1 over a full pulse
        if (customCurve != null && customCurve.length > 0)
        {
            // Map time to 0..1 cycle by frequency
            cycle = Mathf.Repeat((t * _freqNow) + (phaseDegrees / 360f), 1f);
            float shaped = Mathf.Clamp01(customCurve.Evaluate(cycle));
            // Remap 0..1 to -1..1 so we swing around base
            float wave = (shaped * 2f) - 1f;
            light2D.intensity = Mathf.Max(0f, _baseIntensityCached + wave * _ampNow);
        }
        else
        {
            // Sine wave: -1..1
            float wave = Mathf.Sin((t * _freqNow * Mathf.PI * 2f) + _phaseRad);
            light2D.intensity = Mathf.Max(0f, _baseIntensityCached + wave * _ampNow);
        }
    }

    void ApplyJitter()
    {
        // Small random multipliers around 1.0
        float ampMul = 1f + Random.Range(-amplitudeJitter, amplitudeJitter);
        float freqMul = 1f + Random.Range(-frequencyJitter, frequencyJitter);

        _ampNow = Mathf.Max(0f, amplitude * ampMul);
        _freqNow = Mathf.Max(0.0001f, frequency * freqMul);
    }

    // Optional helpers
    [ContextMenu("Snap Base Intensity From Light")]
    public void SnapBaseFromLight()
    {
        if (!light2D) light2D = GetComponent<Light2D>();
        if (light2D) baseIntensity = light2D.intensity;
    }
}
