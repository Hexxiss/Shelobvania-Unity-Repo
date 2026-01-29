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

        [Tooltip("Color multiplier at day (typically white).")]
        public Color dayTint = Color.white;

        [Tooltip("Color multiplier at night (RGB only; alpha is ignored).")]
        public Color nightTint = new Color(0.25f, 0.28f, 0.35f, 1f);

        [Tooltip("How strong night tint can get at maximum (0 = no tint ever, 1 = full nightTint).")]
        [Range(0f, 1f)] public float maxNightStrength = 1f;

        [Tooltip("0 = dayTint, 1 = nightTint (then scaled by maxNightStrength). Make sure it returns to 0 at time=1 if you ever unlock/continue.")]
        public AnimationCurve nightAmountOverDay = DefaultNightCurve();

        [NonSerialized] public SpriteRenderer[] renderers;

        public bool IsValid => root != null;

        private static AnimationCurve DefaultNightCurve()
        {
            // Returns to 0 at the end; useful if you ever unlock/continue past 1 in a custom system.
            // If you always stop at night, this still works fine.
            return new AnimationCurve(
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.20f, 0.00f),
                ne
