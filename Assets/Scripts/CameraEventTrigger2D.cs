using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CameraEventTrigger2D : MonoBehaviour
{
    public enum Mode { PanAndZoomKeepPlayer, PanOnly }

    [Header("Controller")]
    public CameraEventController2D controller;

    [Header("Behavior")]
    public Mode mode = Mode.PanAndZoomKeepPlayer;
    public Transform destinationAnchor;

    [Header("Timing (seconds)")]
    public float moveSeconds = 0.6f;
    public float holdSeconds = 0.8f;
    public float returnSeconds = 0.6f;

    [Header("Zoom Method (only for Pan+Zoom)")]
    public CameraEventController2D.ZoomMethod zoomMethod = CameraEventController2D.ZoomMethod.Ortho;

    [Header("PPU Zoom Controls (only if ZoomMethod = PPU)")]
    [Tooltip("If <= 0, current assetsPPU is reused (no change).")]
    public int targetAssetsPPU = 0;
    [Tooltip("Interpolate PPU across the move/return. If false, snaps at start/return.")]
    public bool animatePPU = true;

    [Header("Ortho Zoom Controls (only if ZoomMethod = Ortho)")]
    [Tooltip("Multiply the auto-fit size (1.0 = exact fit, 1.15 = +15%).")]
    public float fitScale = 1.0f;
    [Tooltip("Additive world units beyond fit (e.g., 0.5).")]
    public float extraOrtho = 0.0f;
    [Tooltip("Hard target orthographic size; if > 0, overrides fitScale/extraOrtho.")]
    public float absoluteTargetOrtho = 0.0f;
    [Tooltip("Snap to this integer pixel zoom; if > 0 and PixelPerfect is assigned, overrides all.")]
    public int absolutePixelZoom = 0;

    [Header("Triggering")]
    public string playerTag = "Player";
    public bool oneShot = true;
    bool _used;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!controller || !destinationAnchor) return;
        if (_used && oneShot) return;
        if (!other.CompareTag(playerTag)) return;

        if (mode == Mode.PanAndZoomKeepPlayer)
        {
            controller.PlayPanAndZoom(
                destinationAnchor,
                moveSeconds, holdSeconds, returnSeconds,
                zoomMethod,
                targetAssetsPPU,
                animatePPU,
                fitScale,
                extraOrtho,
                absoluteTargetOrtho,
                absolutePixelZoom
            );
        }
        else
        {
            controller.PlayPanOnly(destinationAnchor, moveSeconds, holdSeconds, returnSeconds);
        }

        _used = true;
    }
}
