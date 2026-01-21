using System.Collections.Generic;
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("All Weather Effects in Scene")]
    public List<WeatherEffect> effects = new List<WeatherEffect>();

    private readonly Dictionary<string, WeatherEffect> _effectById = new Dictionary<string, WeatherEffect>();
    private WeatherEffect _currentEffect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Build lookup dictionary
        _effectById.Clear();
        foreach (var effect in effects)
        {
            if (effect == null || string.IsNullOrEmpty(effect.effectId))
                continue;

            if (!_effectById.ContainsKey(effect.effectId))
            {
                _effectById.Add(effect.effectId, effect);
                // Start all hidden
                effect.SetVisibleInstant(false);
            }
            else
            {
                Debug.LogWarning($"Duplicate WeatherEffect ID detected: {effect.effectId}", effect);
            }
        }
    }

    /// Called by triggers when the player enters them.
    /// If the requested ID is already active, it will be turned OFF.
    /// If a different effect is active, it will fade out and the new one will fade in.
    public void ToggleEffect(string effectId)
    {
        // Special case: empty ID means "turn everything off"
        if (string.IsNullOrEmpty(effectId))
        {
            if (_currentEffect != null)
            {
                _currentEffect.FadeOut();
                _currentEffect = null;
            }
            return;
        }

        // If we’re already on this effect, turn it off (second pass through same trigger)
        if (_currentEffect != null && _currentEffect.effectId == effectId)
        {
            _currentEffect.FadeOut();
            _currentEffect = null;
            return;
        }

        // Fade out old effect
        if (_currentEffect != null)
        {
            _currentEffect.FadeOut();
            _currentEffect = null;
        }

        // Fade in new effect
        if (_effectById.TryGetValue(effectId, out var newEffect) && newEffect != null)
        {
            newEffect.FadeIn();
            _currentEffect = newEffect;
        }
        else
        {
            Debug.LogWarning($"WeatherManager: No WeatherEffect found with ID '{effectId}'.");
        }
    }
}
