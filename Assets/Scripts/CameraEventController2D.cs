using System.Collections;
using UnityEngine;
using UnityEngine.U2D; // PixelPerfectCamera

[DisallowMultipleComponent]
public class CameraEventController2D : MonoBehaviour
{
    public enum ZoomMethod { Ortho, PPU }

    [Header("References (optional if auto-bind is ON)")]
    [Tooltip("Player root (used to keep the player visible in Pan+Zoom). If left empty, auto-bind will try Lilith_Placeholder.")]
    public Transform player;
    [Tooltip("Orthographic camera to control (auto-fills if null).")]
    public Camera cam;
    [Tooltip("Optional: PixelPerfectCamera on the same object.")]
    public PixelPerfectCamera pixelPerfect;
    [Tooltip("Optional: your follow component (e.g., CinemachineBrain or custom follow). Disabled during effects.")]
    public Behaviour followComponentToDisable;

    [Header("Auto-bind (no inspector hookup required)")]
    [Tooltip("If true, the controller will auto-find and use SimpleCharacter2D on Lilith_Placeholder.")]
    public bool autoBindSimpleCharacter = true;
    [Tooltip("GameObject name that holds the SimpleCharacter2D script.")]
    public string simpleCharacterObjectName = "Lilith_Placeholder";
    [Tooltip("Type name of the movement script to disable during shots. Use fully-qualified name if namespaced.")]
    public string simpleCharacterTypeName = "SimpleCharacter2D";

    [Header("Pixel-Perfect (general)")]
    [Tooltip("Use integer pixel-perfect zoom during normal play (snap after effects).")]
    public bool usePixelPerfectZoom = true;

    [Header("Ortho Zoom Settings")]
    [Tooltip("When using ORTHO zoom, temporarily disable PixelPerfectCamera during the animation for a guaranteed visible zoom.")]
    public bool disablePixelPerfectDuringOrthoZoom = true;
    [Tooltip("Extra world-space padding when fitting the player (Pan+Zoom).")]
    public float fitMargin = 0.5f;

    [Header("Player Control Freeze (optional)")]
    [Tooltip("If left empty and auto-bind is ON, this will be bound to SimpleCharacter2D on Lilith_Placeholder automatically.")]
    public Behaviour playerMovementToDisable;
    [Tooltip("Optional: your input component (e.g., PlayerInput). Disabled/enabled during shots.")]
    public Behaviour playerInputToDisable;
    [Tooltip("Optional: player Rigidbody2D; used to stop sliding/drift during shots. Auto-binds from Lilith_Placeholder if present.")]
    public Rigidbody2D playerRigidbody2D;
    [Tooltip("Also zero angular velocity when freezing the Rigidbody2D.")]
    public bool zeroAngularVelocity = true;

    [Header("Player Physics Freeze (optional)")]
    [Tooltip("Freeze the Rigidbody2D completely during the shot (no drift on slopes/platforms).")]
    public bool hardFreezeRigidbody = true;
    [Tooltip("If not hard freezing, temporarily zero gravity so the body won’t slide.")]
    public bool freezeGravityDuringFreeze = true;
    [Tooltip("Extra linear drag to apply only while frozen when not hard freezing.")]
    public float freezeDrag = 10f;

    // Runtime state
    bool _busy;
    Vector3 _startPos;
    float _startOrtho;
    int _ppuStart = -1;

    // Freeze state caches
    bool _movementWasEnabled;
    bool _inputWasEnabled;

    // Physics caches
    RigidbodyConstraints2D _cachedConstraints;
    float _cachedGravity;
    float _cachedDrag;
    bool _cachedSimulated;

    void Awake()
    {
        if (!cam) cam = GetComponent<Camera>();
        if (!cam || !cam.orthographic)
        {
            Debug.LogError("[CameraEventController2D] Requires an orthographic Camera.");
            enabled = false; return;
        }

        // Auto-bind PixelPerfect if present on this camera
        if (!pixelPerfect) pixelPerfect = GetComponent<PixelPerfectCamera>();

        // Auto-bind movement / player transform / rigidbody if requested
        TryAutoBindSimpleCharacter();
    }

    void TryAutoBindSimpleCharacter()
    {
        if (!autoBindSimpleCharacter) return;
        // Movement script
        if (playerMovementToDisable == null)
        {
            var go = GameObject.Find(simpleCharacterObjectName);
            if (go)
            {
                // Use string-based GetComponent to avoid namespace issues
                var comp = go.GetComponent(simpleCharacterTypeName) as Behaviour;
                if (comp) playerMovementToDisable = comp;

                // Bind player transform if missing
                if (!player) player = go.transform;

                // Bind rigidbody if missing
                if (!playerRigidbody2D) playerRigidbody2D = go.GetComponent<Rigidbody2D>();
            }
        }
        // If movement is still not found but a player transform is set, try on that transform
        if (playerMovementToDisable == null && player != null)
        {
            var comp = player.GetComponent(simpleCharacterTypeName) as Behaviour;
            if (comp) playerMovementToDisable = comp;
            if (!playerRigidbody2D) playerRigidbody2D = player.GetComponent<Rigidbody2D>();
        }
    }

    // -------------------- Public API --------------------

    /// <summary>
    /// Pan to anchor AND zoom so the player remains visible; pause; return.
    /// Choose zoom via ZoomMethod.Ortho or ZoomMethod.PPU.
    /// PPU path uses 'targetAssetsPPU' (if >0) and can animate it across the tween.
    /// Ortho path supports per-shot knobs: fitScale, extraOrtho, absoluteTargetOrtho, absolutePixelZoom.
    /// </summary>
    public void PlayPanAndZoom(
        Transform destinationAnchor,
        float moveSeconds, float holdSeconds, float returnSeconds,
        ZoomMethod zoomMethod = ZoomMethod.Ortho,
        int targetAssetsPPU = 0,      // PPU mode
        bool animatePPU = true,       // PPU mode
        float fitScale = 1.0f,        // Ortho mode
        float extraOrtho = 0.0f,      // Ortho mode (world units)
        float absoluteTargetOrtho = 0.0f, // Ortho mode absolute override (if > 0)
        int absolutePixelZoom = 0          // Ortho mode absolute integer zoom override (if > 0 and pixelPerfect != null)
    )
    {
        if (_busy || !destinationAnchor) return;

        // Late auto-bind safety if scene order changed at runtime
        if (autoBindSimpleCharacter && (playerMovementToDisable == null || player == null || playerRigidbody2D == null))
            TryAutoBindSimpleCharacter();

        StartCoroutine(RunPanAndZoom(
            destinationAnchor,
            moveSeconds, holdSeconds, returnSeconds,
            zoomMethod, targetAssetsPPU, animatePPU,
            fitScale, extraOrtho, absoluteTargetOrtho, absolutePixelZoom
        ));
    }

    /// <summary>
    /// Pan to anchor only (leave player behind); pause; return at same zoom.
    /// </summary>
    public void PlayPanOnly(Transform destinationAnchor, float moveSeconds, float holdSeconds, float returnSeconds)
    {
        if (_busy || !destinationAnchor) return;

        if (autoBindSimpleCharacter && (playerMovementToDisable == null || player == null || playerRigidbody2D == null))
            TryAutoBindSimpleCharacter();

        StartCoroutine(RunPanOnly(destinationAnchor, moveSeconds, holdSeconds, returnSeconds));
    }

    // -------------------- Coroutines --------------------

    IEnumerator RunPanAndZoom(
        Transform anchor, float tMove, float tHold, float tBack,
        ZoomMethod zoomMethod, int targetAssetsPPU, bool animatePPU,
        float fitScale, float extraOrtho, float absoluteTargetOrtho, int absolutePixelZoom
    )
    {
        _busy = true;
        TakeControl();

        _startPos = cam.transform.position;
        _startOrtho = cam.orthographicSize;

        // Target position: center on anchor; keep Z unchanged
        Vector3 targetPos = new Vector3(anchor.position.x, anchor.position.y, _startPos.z);

        // Compute baseline auto-fit ortho to keep player visible
        float fitOrtho = _startOrtho;
        if (player)
            fitOrtho = ComputeOrthoToFitPointFromCenter(targetPos, player.position, _startOrtho, fitMargin);

        // --- ORTHO ZOOM path ---
        if (zoomMethod == ZoomMethod.Ortho)
        {
            // 1) Start from auto-fit, apply per-shot knobs
            float desired = Mathf.Max(_startOrtho, fitOrtho * Mathf.Max(0.01f, fitScale)) + Mathf.Max(0f, extraOrtho);

            // 2) Absolute overrides (precedence)
            if (absoluteTargetOrtho > 0f)
                desired = Mathf.Max(_startOrtho, absoluteTargetOrtho);

            if (absolutePixelZoom > 0 && pixelPerfect != null)
            {
                // Convert integer pixel zoom to exact orthographic size
                float resY = Mathf.Max(1, pixelPerfect.refResolutionY);
                float ppu = Mathf.Max(1, pixelPerfect.assetsPPU);
                desired = resY / (2f * ppu * Mathf.Max(1, absolutePixelZoom));
                desired = Mathf.Max(_startOrtho, desired);
            }

            float targetOrtho = desired;

            if (disablePixelPerfectDuringOrthoZoom) SetPixelPerfectEnabled(false);

            // Move & zoom out
            yield return Tween(tMove, u =>
            {
                float e = EaseInOut(u);
                cam.transform.position = Vector3.Lerp(_startPos, targetPos, e);
                cam.orthographicSize = Mathf.Lerp(_startOrtho, targetOrtho, e);
            });

            if (tHold > 0f) yield return new WaitForSeconds(tHold);

            // Return
            yield return Tween(tBack, u =>
            {
                float e = EaseInOut(u);
                cam.transform.position = Vector3.Lerp(targetPos, _startPos, e);
                cam.orthographicSize = Mathf.Lerp(targetOrtho, _startOrtho, e);
            });

            // Restore Pixel Perfect & snap
            if (disablePixelPerfectDuringOrthoZoom) SetPixelPerfectEnabled(true);
            if (usePixelPerfectZoom && pixelPerfect) SnapToNearestIntegerZoom();

            ReleaseControl();
            _busy = false;
            yield break;
        }

        // --- PPU ZOOM path ---
        if (!pixelPerfect)
        {
            Debug.LogWarning("[CameraEventController2D] PPU zoom requested but no PixelPerfectCamera assigned; falling back to ORTHO zoom.");
            SetPixelPerfectEnabled(false);
            yield return StartCoroutine(RunPanAndZoom(anchor, tMove, tHold, tBack, ZoomMethod.Ortho, 0, false, fitScale, extraOrtho, absoluteTargetOrtho, absolutePixelZoom));
            SetPixelPerfectEnabled(true);
            _busy = false;
            yield break;
        }

        // Cache starting PPU and clamp target
        _ppuStart = pixelPerfect.assetsPPU;
        int ppuTarget = (targetAssetsPPU > 0) ? targetAssetsPPU : _ppuStart;
        ppuTarget = Mathf.Max(1, ppuTarget);

        // Keep ortho constant while changing PPU
        float orthoConst = _startOrtho;

        // Move + PPU change
        yield return Tween(tMove, u =>
        {
            float e = EaseInOut(u);
            cam.transform.position = Vector3.Lerp(_startPos, targetPos, e);

            if (animatePPU && ppuTarget != _ppuStart)
                pixelPerfect.assetsPPU = LerpInt(_ppuStart, ppuTarget, e);

            cam.orthographicSize = orthoConst;
        });

        if (tHold > 0f) yield return new WaitForSeconds(tHold);

        // Return + PPU back
        yield return Tween(tBack, u =>
        {
            float e = EaseInOut(u);
            cam.transform.position = Vector3.Lerp(targetPos, _startPos, e);

            if (animatePPU && ppuTarget != _ppuStart)
                pixelPerfect.assetsPPU = LerpInt(ppuTarget, _ppuStart, e);

            cam.orthographicSize = orthoConst;
        });

        // Ensure exact restoration
        pixelPerfect.assetsPPU = _ppuStart;
        cam.orthographicSize = _startOrtho;

        if (usePixelPerfectZoom && pixelPerfect) SnapToNearestIntegerZoom();

        ReleaseControl();
        _busy = false;
    }

    IEnumerator RunPanOnly(Transform anchor, float tMove, float tHold, float tBack)
    {
        _busy = true;
        TakeControl();

        _startPos = cam.transform.position;
        _startOrtho = cam.orthographicSize;

        Vector3 targetPos = new Vector3(anchor.position.x, anchor.position.y, _startPos.z);

        // Pan out
        yield return Tween(tMove, u =>
        {
            cam.transform.position = Vector3.Lerp(_startPos, targetPos, EaseInOut(u));
        });

        if (tHold > 0f) yield return new WaitForSeconds(tHold);

        // Pan back
        yield return Tween(tBack, u =>
        {
            cam.transform.position = Vector3.Lerp(targetPos, _startPos, EaseInOut(u));
        });

        // Restore exact zoom
        cam.orthographicSize = _startOrtho;
        if (usePixelPerfectZoom && pixelPerfect) SnapToNearestIntegerZoom();

        ReleaseControl();
        _busy = false;
    }

    // -------------------- Helpers --------------------

    void TakeControl()
    {
        // Disable camera follow so it doesn't fight our animation
        if (followComponentToDisable) followComponentToDisable.enabled = false;

        // Freeze player control scripts (auto-bound to SimpleCharacter2D if configured)
        if (playerMovementToDisable)
        {
            _movementWasEnabled = playerMovementToDisable.enabled;
            playerMovementToDisable.enabled = false;
        }
        if (playerInputToDisable)
        {
            _inputWasEnabled = playerInputToDisable.enabled;
            playerInputToDisable.enabled = false;
        }

        // Physics freeze to avoid sliding/drift
        if (playerRigidbody2D)
        {
            // Cache current physics state
            _cachedConstraints = playerRigidbody2D.constraints;
            _cachedGravity = playerRigidbody2D.gravityScale;
            _cachedDrag = playerRigidbody2D.linearDamping;
            _cachedSimulated = playerRigidbody2D.simulated;

            // Kill current motion
            playerRigidbody2D.linearVelocity = Vector2.zero;
            if (zeroAngularVelocity) playerRigidbody2D.angularVelocity = 0f;

            if (hardFreezeRigidbody)
            {
                // Absolute freeze: no drift on slopes or moving platforms
                playerRigidbody2D.constraints = RigidbodyConstraints2D.FreezeAll;
                // If you prefer, you can also stop physics entirely:
                // playerRigidbody2D.simulated = false;
            }
            else
            {
                // Soft freeze: keep sim on, remove slide
                if (freezeGravityDuringFreeze) playerRigidbody2D.gravityScale = 0f;
                playerRigidbody2D.linearDamping = Mathf.Max(_cachedDrag, freezeDrag);
            }

            playerRigidbody2D.Sleep(); // ensure it rests this frame
        }

        // Keep camera Z stable
        Vector3 p = cam.transform.position;
        if (Mathf.Approximately(p.z, 0f)) cam.transform.position = new Vector3(p.x, p.y, -10f);
    }

    void ReleaseControl()
    {
        // Restore physics
        if (playerRigidbody2D)
        {
            playerRigidbody2D.constraints = _cachedConstraints;
            playerRigidbody2D.gravityScale = _cachedGravity;
            playerRigidbody2D.linearDamping = _cachedDrag;
            playerRigidbody2D.simulated = _cachedSimulated;
        }

        // Unfreeze player control (restore prior states)
        if (playerMovementToDisable)
            playerMovementToDisable.enabled = _movementWasEnabled;
        if (playerInputToDisable)
            playerInputToDisable.enabled = _inputWasEnabled;

        // Re-enable camera follow
        if (followComponentToDisable) followComponentToDisable.enabled = true;
    }

    // Minimal half-height to keep 'pt' visible when centered at 'center'
    float ComputeOrthoToFitPointFromCenter(Vector3 center, Vector3 pt, float currentOrtho, float margin)
    {
        float dx = Mathf.Abs(pt.x - center.x) + margin;
        float dy = Mathf.Abs(pt.y - center.y) + margin;
        float halfHeightNeeded = Mathf.Max(dy, dx / Mathf.Max(0.0001f, cam.aspect));
        return Mathf.Max(currentOrtho, halfHeightNeeded);
    }

    void SetPixelPerfectEnabled(bool active)
    {
        if (pixelPerfect) pixelPerfect.enabled = active;
    }

    void SnapToNearestIntegerZoom()
    {
        if (!pixelPerfect) return;
        float resY = Mathf.Max(1, pixelPerfect.refResolutionY);
        float ppu = Mathf.Max(1, pixelPerfect.assetsPPU);
        float zf = resY / (2f * ppu * Mathf.Max(0.0001f, cam.orthographicSize));
        int zi = Mathf.Max(1, Mathf.RoundToInt(zf));
        cam.orthographicSize = resY / (2f * ppu * zi);
    }

    static int LerpInt(int a, int b, float t)
    {
        return Mathf.RoundToInt(Mathf.Lerp(a, b, Mathf.Clamp01(t)));
    }

    static float EaseInOut(float t)
    {
        t = Mathf.Clamp01(t);
        return (t < 0.5f) ? (2f * t * t) : (1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f);
    }

    static IEnumerator Tween(float duration, System.Action<float> step)
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
