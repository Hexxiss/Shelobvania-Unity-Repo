using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ChildLight2DIntensityController : MonoBehaviour
{
  
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool autoRefreshInEditor = true;
    [Min(0f)]
    [SerializeField] private float intensityMultiplier = 1f;
    [SerializeField] private Vector2 intensityClamp = new Vector2(0f, 10f);

    private readonly List<Light2D> _lights = new List<Light2D>();
    private readonly Dictionary<Light2D, float> _baseIntensity = new Dictionary<Light2D, float>();

    private float _lastMultiplier = -1f;
    private Vector2 _lastClamp;

    private void OnEnable()
    {
        RefreshLights(recordBase: true);
        ApplyIntensity(force: true);
    }

    private void OnTransformChildrenChanged()
    {
        RefreshLights(recordBase: true);
        ApplyIntensity(force: true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!autoRefreshInEditor) return;

        RefreshLights(recordBase: true);
        ApplyIntensity(force: true);
    }
#endif

 
    public void SetMultiplier(float multiplier)
    {
        intensityMultiplier = Mathf.Max(0f, multiplier);
        ApplyIntensity(force: true);
    }

   
    public void RefreshLights(bool recordBase)
    {
        _lights.Clear();
        _baseIntensity.Clear();

        var found = GetComponentsInChildren<Light2D>(includeInactive);

        foreach (var l in found)
        {
            if (l == null) continue;
            if (l.transform == transform) continue; // Only children

            _lights.Add(l);

            if (recordBase)
                _baseIntensity[l] = l.intensity;
        }

        _lastMultiplier = -1f; // Force next Apply to run
    }

    public void ApplyIntensity(bool force)
    {
        if (!force &&
            Mathf.Approximately(_lastMultiplier, intensityMultiplier) &&
            _lastClamp == intensityClamp)
        {
            return;
        }

        _lastMultiplier = intensityMultiplier;
        _lastClamp = intensityClamp;

        for (int i = _lights.Count - 1; i >= 0; i--)
        {
            var l = _lights[i];
            if (l == null)
            {
                _lights.RemoveAt(i);
                continue;
            }

            // If the light wasn't recorded (rare edge case), treat current intensity as base.
            if (!_baseIntensity.TryGetValue(l, out float baseVal))
                baseVal = l.intensity;

            float applied = baseVal * intensityMultiplier;
            applied = Mathf.Clamp(applied, intensityClamp.x, intensityClamp.y);

            l.intensity = applied;
        }
    }

    private void Update()
    {
        ApplyIntensity(force: false);
    }
}
