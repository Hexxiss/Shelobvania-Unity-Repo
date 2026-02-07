using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class GlobalSaturationController : MonoBehaviour
{
    [Header("Volume Reference")]
    [Tooltip("If left empty, the script will try to find a Global Volume in the scene.")]
    public Volume volume;

    [Header("Art Direction")]
    [Range(-100f, 0f)]
    public float targetSaturation = -50f;

    [Tooltip("How quickly saturation moves toward the target (higher = faster).")]
    public float lerpSpeed = 6f;

    ColorAdjustments _colorAdjustments;

    void Awake()
    {
        if (volume == null)
            volume = FindFirstObjectByType<Volume>();

        if (volume == null)
        {
            Debug.LogError("No Volume found. Create a Global Volume with a profile that includes Color Adjustments.");
            enabled = false;
            return;
        }

        if (volume.profile == null)
        {
            Debug.LogError("Volume has no profile assigned.");
            enabled = false;
            return;
        }

        if (!volume.profile.TryGet(out _colorAdjustments) || _colorAdjustments == null)
        {
            Debug.LogError("Volume profile is missing a Color Adjustments override.");
            enabled = false;
            return;
        }

        // Ensure the override is active and saturation is controlled by the profile
        _colorAdjustments.active = true;
        _colorAdjustments.saturation.overrideState = true;
    }

    void Update()
    {
        // Smoothly move saturation toward your target
        float current = _colorAdjustments.saturation.value;
        float next = Mathf.Lerp(current, targetSaturation, 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime));
        _colorAdjustments.saturation.value = next;
    }

    // Optional: call this from other scripts
    public void SetSaturation(float saturation)
    {
        targetSaturation = Mathf.Clamp(saturation, -100f, 0f);
    }
}
