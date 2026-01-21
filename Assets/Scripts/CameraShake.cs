using UnityEngine;

/// Add this to your Camera (or a parent rig).
/// Press Enter/Return to test. Call CameraShake.Instance.AddTrauma(amount) from other scripts.
[DisallowMultipleComponent]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

   //Max positional offset in units when trauma=1
    public Vector2 maxPosShake = new Vector2(0.25f, 0.25f);

    //Max rotational Z shake in degrees when trauma=1 (2D tilt). Set 0 to disable.
    public float maxRotZ = 3f;

    //"How fast the noise changes (Hz)
    public float frequency = 25f;

   //How quickly shake decays per second
    public float traumaDecay = 1.5f;

    //Exponent to shape intensity (2 is common). Higher = snappier falloff
    public float traumaExponent = 2f;

    [Header("Test")]
    public KeyCode testKey = KeyCode.Return;     // Enter/Return
    public float testTrauma = 0.6f;

    float trauma;                                 // 0..1
    float noiseTime;
    Vector3 baseLocalPos;
    Quaternion baseLocalRot;

    // Noise channel offsets so X/Y/Z are decorrelated
    float nOffX, nOffY, nOffR;

    // To affect this script via other scripts, add this to the other script:

            // light tap
            //add this >>>> CameraShake.Instance.AddTrauma(0.2f);

            // heavy impact with temporary faster decay
            //add this >>>>> CameraShake.Instance.ShakeOneShot(0.8f, customDecay: 3.0f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        baseLocalPos = transform.localPosition;
        baseLocalRot = transform.localRotation;

        // Randomize noise offsets so multiple cameras don’t look identical
        nOffX = Random.value * 1000f;
        nOffY = Random.value * 1000f + 111.1f;
        nOffR = Random.value * 1000f + 222.2f;
    }

    void Update()
    {
        // Simple test trigger
        if (Input.GetKeyDown(testKey))
            AddTrauma(testTrauma);

        // If no shake, keep tracking the "base" in case other scripts move the camera rig
        if (trauma <= 0f)
        {
            baseLocalPos = transform.localPosition;
            baseLocalRot = transform.localRotation;
            return;
        }

        // Advance noise
        noiseTime += Time.deltaTime * frequency;

        // Intensity shaping
        float t = Mathf.Pow(Mathf.Clamp01(trauma), traumaExponent);

        // Perlin returns 0..1; remap to -1..1
        float nx = Mathf.PerlinNoise(nOffX, noiseTime) * 2f - 1f;
        float ny = Mathf.PerlinNoise(nOffY, noiseTime) * 2f - 1f;
        float nr = Mathf.PerlinNoise(nOffR, noiseTime) * 2f - 1f;

        // Apply offsets scaled by t
        Vector3 posOffset = new Vector3(nx * maxPosShake.x * t, ny * maxPosShake.y * t, 0f);
        float rotZ = (maxRotZ != 0f) ? (nr * maxRotZ * t) : 0f;

        transform.localPosition = baseLocalPos + posOffset;
        transform.localRotation = Quaternion.Euler(0f, 0f, rotZ) * baseLocalRot;

        // Decay trauma
        trauma = Mathf.Max(0f, trauma - traumaDecay * Time.deltaTime);

        // If we just finished shaking, snap back perfectly
        if (trauma <= 0f)
        {
            transform.localPosition = baseLocalPos;
            transform.localRotation = baseLocalRot;
        }
    }

    /// Add shake intensity (0..1). Values add up and are clamped to 1.
    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + Mathf.Max(0f, amount));
    }

    /// One-shot helper: apply a specific intensity and optional custom decay (temporarily).
    public void ShakeOneShot(float intensity, float customDecay = -1f)
    {
        if (customDecay > 0f) StartCoroutine(TempDecay(customDecay, 0.25f));
        AddTrauma(intensity);
    }

    System.Collections.IEnumerator TempDecay(float newDecay, float duration)
    {
        float old = traumaDecay;
        traumaDecay = newDecay;
        yield return new WaitForSeconds(duration);
        traumaDecay = old;
    }
}
