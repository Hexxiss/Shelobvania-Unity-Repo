using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal; // Light2D

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class DayNightCycle2D : MonoBehaviour
{
    [Header("Time")]
    [Tooltip("Normalized time of day. 0=Morning start, 0.25=Afternoon, 0.5=Evening, 0.75=Night.")]
    [Range(0f, 1f)] public float time01 = 0f;

    [Tooltip("If enabled, time will advance automatically (Play Mode only) until it reaches Night and locks (if Stop At Night is enabled).")]
    public bool autoAdvanceInGame = true;

    [Tooltip("How many real-time seconds for a full day from 0..1 (if not stopping at night).")]
    [Min(0.01f)] public float secondsPerFullDay = 300f;

    [Tooltip("Multiply time progression (2 = twice as fast, 0.5 = half speed).")]
    public float timeScale = 1f;

    [Header("Stop At Night (No Loop)")]
    [Tooltip("If enabled, the cycle will stop once time01 reaches Night Lock Time.")]
    public bool stopAtNight = true;

    [Tooltip("Where to stop/lock. 0.75 = start of Night phase. Set closer to 1.0 if you want it to progress deeper into night before stopping.")]
    [Range(0f, 1f)] public float nightLockTime01 = 0.75f;

    [Tooltip("Invoked once when the system first reaches the Night Lock Time (from below).")]
    public UnityEvent onReachedNight;

    [Header("Phase Crossfade")]
    [Tooltip("Portion of each phase segment used to crossfade into the next phase. Example: 0.25 = last 25% of each phase.")]
    [Range(0f, 1f)] public float crossfadeFraction = 0.25f;

    [Header("Edit Mode Preview")]
    [Tooltip("If enabled, updates in edit mode so you can scrub time01 and preview transitions/tinting/light.")]
    public bool previewInEditMode = true;

    [Serializable]
    public class PhaseBackground
    {
        public string name;
        public Transform root;
        public bool includeInactiveChildren = true;

        [NonSerialized] public SpriteRenderer[] renderers;
        [NonSerialized] public Color[] baseColors;

        public bool IsValid => root != null;
    }

    [Serializable]
    public class TintGroup
    {
        public string name;
        public Transform root;
        public bool includeInactiveChildren = true;

        [Header("Phase Tints (Multiplier Colors)")]
        [Tooltip("Multiplier at Morning. Typically near-white with a subtle warm tint.")]
        public Color morningTint = new Color(1f, 0.97f, 0.92f, 1f);

        [Tooltip("Multiplier at Afternoon. Often pure white.")]
        public Color afternoonTint = Color.white;

        [Tooltip("Multiplier at Evening. Often warmer/darker.")]
        public Color eveningTint = new Color(1f, 0.82f, 0.65f, 1f);

        [Tooltip("Multiplier at Night. Often cool/darker.")]
        public Color nightTint = new Color(0.35f, 0.40f, 0.55f, 1f);

        [Tooltip("Overall strength of the tint multiplier. 0 = no tinting, 1 = full phase tint.")]
        [Range(0f, 1f)] public float tintStrength = 1f;

        [NonSerialized] public SpriteRenderer[] renderers;

        public bool IsValid => root != null;
    }

    [Serializable]
    public class LightPhaseSettings
    {
        public Color color = Color.white;
        [Min(0f)] public float intensity = 1f;
    }

    [Header("Phase Backgrounds (4)")]
    public PhaseBackground morning = new PhaseBackground { name = "Morning" };
    public PhaseBackground afternoon = new PhaseBackground { name = "Afternoon" };
    public PhaseBackground evening = new PhaseBackground { name = "Evening" };
    public PhaseBackground night = new PhaseBackground { name = "Night" };

    [Header("Tint Groups (Any Number)")]
    public List<TintGroup> tintGroups = new List<TintGroup>();

    [Header("Global Light 2D (Optional)")]
    [Tooltip("Assign your URP 2D Global Light here (Light2D with Light Type = Global).")]
    public Light2D globalLight;

    [Tooltip("Enable/disable driving the global light from this script.")]
    public bool driveGlobalLight = true;

    [Tooltip("If true, the light transitions use the same crossfade weights as the sky phases.")]
    public bool lightFollowsPhaseCrossfade = true;

    [Tooltip("Optional override for light crossfade timing (only used if lightFollowsPhaseCrossfade is false).")]
    [Range(0f, 1f)] public float lightCrossfadeFraction = 0.25f;

    [Header("Global Light Phase Looks")]
    public LightPhaseSettings lightMorning = new LightPhaseSettings { color = new Color(1f, 0.95f, 0.85f, 1f), intensity = 1.0f };
    public LightPhaseSettings lightAfternoon = new LightPhaseSettings { color = Color.white, intensity = 1.1f };
    public LightPhaseSettings lightEvening = new LightPhaseSettings { color = new Color(1f, 0.75f, 0.55f, 1f), intensity = 0.75f };
    public LightPhaseSettings lightNight = new LightPhaseSettings { color = new Color(0.55f, 0.65f, 1f, 1f), intensity = 0.35f };

    private PhaseBackground[] _phases;

    // Cached phase blend state for this frame (used by global light + tint groups)
    private int _currPhaseIndex;
    private int _nextPhaseIndex;
    private float _currPhaseWeight;
    private float _nextPhaseWeight;

    // Stable original color cache for tint groups (prevents “poisoned” bases)
    private readonly Dictionary<int, Color> _originalColorByRendererId = new Dictionary<int, Color>(256);

    private bool _isLockedAtNight = false;
    private bool _nightEventFired = false;

    private void OnEnable()
    {
        _phases = new[] { morning, afternoon, evening, night };
        RebuildCaches();
        EvaluateNightLockState();
        ApplyAll();
    }

    private void Awake()
    {
        _phases = new[] { morning, afternoon, evening, night };
        RebuildCaches();
        EvaluateNightLockState();
        ApplyAll();
    }

    private void Update()
    {
        // Edit mode preview (no auto-advance in edit mode; you scrub time01 manually)
        if (!Application.isPlaying)
        {
            if (!previewInEditMode) return;

            EvaluateNightLockState();
            ApplyAll();
            return;
        }

        // Play mode auto-advance
        if (autoAdvanceInGame && !_isLockedAtNight)
        {
            float delta01 = (Time.deltaTime / Mathf.Max(0.0001f, secondsPerFullDay)) * timeScale;
            float before = time01;

            time01 = Mathf.Clamp01(time01 + delta01);

            // Stop/lock when reaching night threshold
            if (stopAtNight && before < nightLockTime01 && time01 >= nightLockTime01)
            {
                time01 = nightLockTime01;
                LockAtNightInternal();
            }
        }
        else
        {
            // Even if not auto-advancing, keep lock state consistent with current time01
            EvaluateNightLockState();
        }

        ApplyAll();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _phases = new[] { morning, afternoon, evening, night };

        // Find renderers, but DO NOT overwrite original tint bases
        if (!Application.isPlaying && previewInEditMode)
        {
            RebuildCaches();
            EvaluateNightLockState();
            ApplyAll();
        }
    }
#endif

    [ContextMenu("Rebuild Caches")]
    public void RebuildCaches()
    {
        if (_phases == null || _phases.Length != 4)
            _phases = new[] { morning, afternoon, evening, night };

        // Phase caches
        foreach (var p in _phases)
        {
            if (p == null || !p.IsValid) continue;

            p.renderers = p.root.GetComponentsInChildren<SpriteRenderer>(p.includeInactiveChildren);
            p.baseColors = new Color[p.renderers.Length];
            for (int i = 0; i < p.renderers.Length; i++)
                p.baseColors[i] = p.renderers[i] ? p.renderers[i].color : Color.white;
        }

        // Tint caches + capture original colors ONCE per renderer
        foreach (var g in tintGroups)
        {
            if (g == null || !g.IsValid) continue;

            g.renderers = g.root.GetComponentsInChildren<SpriteRenderer>(g.includeInactiveChildren);
            for (int i = 0; i < g.renderers.Length; i++)
            {
                var r = g.renderers[i];
                if (!r) continue;

                int id = r.GetInstanceID();
                if (!_originalColorByRendererId.ContainsKey(id))
                    _originalColorByRendererId.Add(id, r.color);
            }
        }
    }

    [ContextMenu("Clear Original Color Cache (Tint Groups)")]
    public void ClearOriginalColorCache()
    {
        _originalColorByRendererId.Clear();
        RebuildCaches();
        ApplyAll();
    }

    [ContextMenu("Capture Original Colors Now (Tint Groups)")]
    public void CaptureOriginalColorsNow()
    {
        foreach (var g in tintGroups)
        {
            if (g == null || !g.IsValid) continue;
            if (g.renderers == null) continue;

            for (int i = 0; i < g.renderers.Length; i++)
            {
                var r = g.renderers[i];
                if (!r) continue;
                _originalColorByRendererId[r.GetInstanceID()] = r.color;
            }
        }

        ApplyAll();
    }

    // -------------------------
    // Public API for other scripts
    // -------------------------

    /// <summary>Set normalized time. By default respects Stop At Night (will clamp to nightLockTime01).</summary>
    public void SetTime01(float newTime01, bool forcePastNightLock = false)
    {
        float clamped = Mathf.Clamp01(newTime01);

        if (stopAtNight && !forcePastNightLock)
            clamped = Mathf.Min(clamped, nightLockTime01);

        float before = time01;
        time01 = clamped;

        if (stopAtNight && before < nightLockTime01 && time01 >= nightLockTime01)
        {
            time01 = nightLockTime01;
            LockAtNightInternal();
        }
        else
        {
            EvaluateNightLockState();
        }

        ApplyAll();
    }

    /// <summary>Add delta to normalized time. By default respects Stop At Night (will clamp to nightLockTime01).</summary>
    public void AddTime01(float delta01, bool forcePastNightLock = false)
    {
        SetTime01(time01 + delta01, forcePastNightLock);
    }

    /// <summary>Convenience: set by hours (0..24). Uses 24h mapping into 0..1.</summary>
    public void SetTimeHours(float hours0To24, bool forcePastNightLock = false)
    {
        float t = Mathf.Clamp01(hours0To24 / 24f);
        SetTime01(t, forcePastNightLock);
    }

    /// <summary>Immediately locks the system at nightLockTime01 and stops auto-advance.</summary>
    public void LockAtNightNow()
    {
        time01 = Mathf.Clamp01(nightLockTime01);
        LockAtNightInternal();
        ApplyAll();
    }

    /// <summary>Unlocks time (optional). Does not auto-loop; you can still SetTime01 back down if your game supports it.</summary>
    public void UnlockTime()
    {
        _isLockedAtNight = false;
        _nightEventFired = false;
        ApplyAll();
    }

    // -------------------------
    // Internals
    // -------------------------

    private void EvaluateNightLockState()
    {
        if (!stopAtNight)
        {
            _isLockedAtNight = false;
            _nightEventFired = false;
            return;
        }

        if (time01 >= nightLockTime01)
        {
            time01 = Mathf.Clamp01(nightLockTime01);
            _isLockedAtNight = true;

            if (!_nightEventFired)
            {
                _nightEventFired = true;
                onReachedNight?.Invoke();
            }
        }
        else
        {
            _isLockedAtNight = false;
        }
    }

    private void LockAtNightInternal()
    {
        _isLockedAtNight = true;
        autoAdvanceInGame = false;

        if (!_nightEventFired)
        {
            _nightEventFired = true;
            onReachedNight?.Invoke();
        }
    }

    private void ApplyAll()
    {
        ApplyPhaseCrossfade();
        ApplyTintGroups_PhaseBlended(); // NEW phase-based tinting
        ApplyGlobalLight();
    }

    private void ApplyPhaseCrossfade()
    {
        const float segLen = 0.25f;

        int curr = Mathf.FloorToInt(time01 / segLen);
        curr = Mathf.Clamp(curr, 0, 3);

        int next = (curr + 1) % 4;

        float segEnd = (curr + 1) * segLen;
        float fadeLen = Mathf.Clamp01(crossfadeFraction) * segLen;

        float currW = 1f;
        float nextW = 0f;

        if (fadeLen > 0f && time01 >= (segEnd - fadeLen))
        {
            float b = (time01 - (segEnd - fadeLen)) / fadeLen;
            b = Mathf.Clamp01(SmoothStep01(b));
            currW = 1f - b;
            nextW = b;
        }

        _currPhaseIndex = curr;
        _nextPhaseIndex = next;
        _currPhaseWeight = currW;
        _nextPhaseWeight = nextW;

        for (int i = 0; i < 4; i++)
        {
            float a = (i == curr) ? currW : (i == next) ? nextW : 0f;
            SetPhaseAlpha(_phases[i], a);
        }
    }

    private void SetPhaseAlpha(PhaseBackground phase, float alpha01)
    {
        if (phase == null || !phase.IsValid || phase.renderers == null || phase.baseColors == null)
            return;

        // Absolute alpha (prevents cached/base alpha = 0 from breaking fades)
        for (int i = 0; i < phase.renderers.Length; i++)
        {
            var r = phase.renderers[i];
            if (!r) continue;

            Color c = phase.baseColors[i];
            c.a = alpha01;
            r.color = c;
        }
    }

    // NEW: Tint groups blend between phase tints just like the global light blends between phase looks.
    private void ApplyTintGroups_PhaseBlended()
    {
        for (int gi = 0; gi < tintGroups.Count; gi++)
        {
            var g = tintGroups[gi];
            if (g == null || !g.IsValid || g.renderers == null) continue;

            // Get phase multipliers for current + next
            Color currTint = GetTintPhaseColor(g, _currPhaseIndex);
            Color nextTint = GetTintPhaseColor(g, _nextPhaseIndex);

            // Weighted blend (curr->next), matching the background crossfade
            Color blended = (currTint * _currPhaseWeight) + (nextTint * _nextPhaseWeight);

            // Strength control: lerp multiplier from white -> blended
            float s = Mathf.Clamp01(g.tintStrength);
            Color multiplier = Color.Lerp(Color.white, new Color(blended.r, blended.g, blended.b, 1f), s);

            for (int i = 0; i < g.renderers.Length; i++)
            {
                var r = g.renderers[i];
                if (!r) continue;

                int id = r.GetInstanceID();
                if (!_originalColorByRendererId.TryGetValue(id, out var baseC))
                {
                    // If somehow missed, capture now (won’t be overwritten again)
                    baseC = r.color;
                    _originalColorByRendererId[id] = baseC;
                }

                r.color = new Color(
                    baseC.r * multiplier.r,
                    baseC.g * multiplier.g,
                    baseC.b * multiplier.b,
                    baseC.a
                );
            }
        }
    }

    private static Color GetTintPhaseColor(TintGroup g, int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 0: return g.morningTint;
            case 1: return g.afternoonTint;
            case 2: return g.eveningTint;
            default: return g.nightTint;
        }
    }

    private void ApplyGlobalLight()
    {
        if (!driveGlobalLight || globalLight == null) return;

        int curr, next;
        float currW, nextW;

        if (lightFollowsPhaseCrossfade)
        {
            curr = _currPhaseIndex;
            next = _nextPhaseIndex;
            currW = _currPhaseWeight;
            nextW = _nextPhaseWeight;
        }
        else
        {
            const float segLen = 0.25f;

            curr = Mathf.FloorToInt(time01 / segLen);
            curr = Mathf.Clamp(curr, 0, 3);

            next = (curr + 1) % 4;

            float segEnd = (curr + 1) * segLen;
            float fadeLen = Mathf.Clamp01(lightCrossfadeFraction) * segLen;

            currW = 1f;
            nextW = 0f;

            if (fadeLen > 0f && time01 >= (segEnd - fadeLen))
            {
                float b = (time01 - (segEnd - fadeLen)) / fadeLen;
                b = Mathf.Clamp01(SmoothStep01(b));
                currW = 1f - b;
                nextW = b;
            }
        }

        var a = GetLightPhase(curr);
        var bPhase = GetLightPhase(next);

        globalLight.color = (a.color * currW) + (bPhase.color * nextW);
        globalLight.intensity = (a.intensity * currW) + (bPhase.intensity * nextW);
    }

    private LightPhaseSettings GetLightPhase(int phaseIndex)
    {
        switch (phaseIndex)
        {
            case 0: return lightMorning;
            case 1: return lightAfternoon;
            case 2: return lightEvening;
            default: return lightNight;
        }
    }

    private static float SmoothStep01(float x) => x * x * (3f - 2f * x);
}
