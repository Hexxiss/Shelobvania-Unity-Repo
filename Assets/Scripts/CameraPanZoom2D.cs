using UnityEngine;
using System.Collections;
using UnityEngine.U2D; // 2D Pixel Perfect Camera

public class CameraPanZoom2D : MonoBehaviour
{
    public enum Compass8 { N, NE, E, SE, S, SW, W, NW }

    [Header("References")]
    [Tooltip("Your player root transform the camera normally follows.")]
    public Transform player;
    [Tooltip("Optional: 2D Pixel Perfect Camera component (if used).")]
    public PixelPerfectCamera pixelPerfectCam; // optional
    [Tooltip("Camera to zoom (auto-fills from this GO).")]
    public Camera cam;

    [Header("Follow Settings")]
    [Tooltip("Normal follow offset from player (world units).")]
    public Vector3 followOffset = new Vector3(0f, 0f, -10f);
    [Tooltip("If true, camera keeps following player during pan; otherwise it locks at effect start.")]
    public bool keepFollowingDuringEffect = false;

    [Header("Pixel Perfect Zoom (optional)")]
    [Tooltip("If true and PixelPerfectCamera is assigned, zoom snapping uses integer steps by adjusting orthographicSize.")]
    public bool usePixelPerfectZoom = true;

    // Runtime
    bool _inEffect;
    float _originOrthoSize;
    int _originZoomFactor = 1; // computed from PPC settings + ortho
    Vector3 _lockedFollowAnchor;
    Vector3 _currentPanOffset;

    void Awake()
    {
        if (!cam) cam = GetComponent<Camera>();
        if (!cam || !cam.orthographic)
        {
            Debug.LogError("[CameraPanZoom2D] Requires an orthographic Camera.");
        }
        if (!player)
        {
            Debug.LogError("[CameraPanZoom2D] Assign the Player transform.");
        }

        _originOrthoSize = cam.orthographicSize;

        if (pixelPerfectCam && usePixelPerfectZoom)
        {
            _originZoomFactor = OrthoToIntegerZoom(pixelPerfectCam, _originOrthoSize);
        }
    }

    void LateUpdate()
    {
        if (_inEffect) return;

        if (player)
            transform.position = player.position + followOffset;
    }

    /// Begin a pan/zoom that returns to original
    /// If using PixelPerfect, pass an integer for targetZoomOrOrthoSize (e.g., 2), and leave targetIsIntegerZoom=true.
    /// If not, pass the target orthographicSize and set targetIsIntegerZoom=false.
    public void BeginPanZoom(Compass8 direction, float panDistance, float targetZoomOrOrthoSize,
                             float panOutSeconds, float holdSeconds, float panBackSeconds,
                             bool targetIsIntegerZoom = true)
    {
        if (!_inEffect)
            StartCoroutine(PanZoomRoutine(direction, panDistance, targetZoomOrOrthoSize,
                                          panOutSeconds, holdSeconds, panBackSeconds,
                                          targetIsIntegerZoom));
    }

    IEnumerator PanZoomRoutine(Compass8 direction, float panDistance, float targetZoomOrOrthoSize,
                               float panOutSeconds, float holdSeconds, float panBackSeconds,
                               bool targetIsIntegerZoom)
    {
        _inEffect = true;
        _currentPanOffset = Vector3.zero;

        _lockedFollowAnchor = keepFollowingDuringEffect && player
            ? Vector3.zero // dynamic, computed per frame
            : (player ? (player.position + followOffset) : transform.position);

        // Pan direction
        Vector2 dir = DirToVector(direction);
        Vector3 panTarget = new Vector3(dir.x, dir.y, 0f) * panDistance;

        // Zoom endpoints
        float startOrtho = cam.orthographicSize;
        float endOrtho = startOrtho;

        int startZoomInt = pixelPerfectCam && usePixelPerfectZoom
            ? OrthoToIntegerZoom(pixelPerfectCam, startOrtho)
            : 1;

        int endZoomInt = startZoomInt;

        if (pixelPerfectCam && usePixelPerfectZoom && targetIsIntegerZoom)
        {
            endZoomInt = Mathf.Max(1, Mathf.RoundToInt(targetZoomOrOrthoSize));
            endOrtho = OrthoForIntegerZoom(pixelPerfectCam, endZoomInt);
        }
        else
        {
            endOrtho = Mathf.Max(0.0001f, targetZoomOrOrthoSize); // treat as raw orthographic size
        }

        // Phase 1: pan/zoom out
        yield return Animate((t) =>
        {
            float u = EaseInOutQuad(t);
            _currentPanOffset = Vector3.Lerp(Vector3.zero, panTarget, u);

            ApplyOrtho(
                Mathf.Lerp(startOrtho, endOrtho, u),
                pixelPerfectCam && usePixelPerfectZoom,
                startZoomInt, endZoomInt, u
            );

            PositionCameraDuringEffect();
        }, panOutSeconds);

        // Hold
        if (holdSeconds > 0f)
        {
            float elapsed = 0f;
            while (elapsed < holdSeconds)
            {
                elapsed += Time.deltaTime;
                PositionCameraDuringEffect();
                yield return null;
            }
        }

        // Phase 2: pan/zoom back
        yield return Animate((t) =>
        {
            float u = EaseInOutQuad(t);
            _currentPanOffset = Vector3.Lerp(panTarget, Vector3.zero, u);

            ApplyOrtho(
                Mathf.Lerp(endOrtho, startOrtho, u),
                pixelPerfectCam && usePixelPerfectZoom,
                endZoomInt, startZoomInt, u
            );

            PositionCameraDuringEffect();
        }, panBackSeconds);

        // Cleanup
        _currentPanOffset = Vector3.zero;

        if (pixelPerfectCam && usePixelPerfectZoom)
        {
            // Snap back to original integer zoom via orthographic size
            int z = OrthoToIntegerZoom(pixelPerfectCam, _originOrthoSize);
            cam.orthographicSize = OrthoForIntegerZoom(pixelPerfectCam, z);
        }
        else
        {
            cam.orthographicSize = _originOrthoSize;
        }

        if (player) transform.position = player.position + followOffset;

        _inEffect = false;
    }

    void PositionCameraDuringEffect()
    {
        Vector3 basePos;
        if (keepFollowingDuringEffect && player)
            basePos = player.position + followOffset;
        else
            basePos = _lockedFollowAnchor;

        transform.position = basePos + _currentPanOffset;
    }

    void ApplyOrtho(float candidateOrtho, bool snapPixelPerfect, int fromZoom, int toZoom, float u)
    {
        if (snapPixelPerfect && pixelPerfectCam)
        {
            // Blend across integer zoom steps smoothly by picking the nearest integer at this point
            float blended = Mathf.Lerp(fromZoom, toZoom, u);
            int z = Mathf.Max(1, Mathf.RoundToInt(blended));
            cam.orthographicSize = OrthoForIntegerZoom(pixelPerfectCam, z);
        }
        else
        {
            cam.orthographicSize = candidateOrtho;
        }
    }

    // --- Pixel Perfect helpers: convert between integer zoom and ortho size ---
    // Orthographic size that yields integer pixel-perfect zoom:
    // ortho = refResolutionY / (2 * assetsPPU * zoom)
    static float OrthoForIntegerZoom(PixelPerfectCamera ppc, int zoom)
    {
        float resY = Mathf.Max(1, ppc.refResolutionY);
        float ppu = Mathf.Max(1, ppc.assetsPPU);
        int z = Mathf.Max(1, zoom);
        return resY / (2f * ppu * z);
    }

    // Given current ortho, what integer zoom would PPC compute? (rounded)
    static int OrthoToIntegerZoom(PixelPerfectCamera ppc, float ortho)
    {
        float resY = Mathf.Max(1, ppc.refResolutionY);
        float ppu = Mathf.Max(1, ppc.assetsPPU);
        float zFloat = resY / (2f * ppu * Mathf.Max(0.0001f, ortho));
        return Mathf.Max(1, Mathf.RoundToInt(zFloat));
    }

    static Vector2 DirToVector(Compass8 d)
    {
        switch (d)
        {
            case Compass8.N: return new Vector2(0, 1);
            case Compass8.NE: return (new Vector2(1, 1)).normalized;
            case Compass8.E: return new Vector2(1, 0);
            case Compass8.SE: return (new Vector2(1, -1)).normalized;
            case Compass8.S: return new Vector2(0, -1);
            case Compass8.SW: return (new Vector2(-1, -1)).normalized;
            case Compass8.W: return new Vector2(-1, 0);
            case Compass8.NW: return (new Vector2(-1, 1)).normalized;
        }
        return Vector2.zero;
    }

    static float EaseInOutQuad(float t)
    {
        t = Mathf.Clamp01(t);
        return (t < 0.5f) ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
    }

    IEnumerator Animate(System.Action<float> step, float duration)
    {
        duration = Mathf.Max(0.0001f, duration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            step(Mathf.Clamp01(t / duration));
            yield return null;
        }
        step(1f);
    }
}
