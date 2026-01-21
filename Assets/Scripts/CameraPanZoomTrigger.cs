using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CameraPanZoomTrigger2D : MonoBehaviour
{
    public CameraPanZoom2D cameraController;

    [Header("Trigger Settings")]
    public string playerTag = "Player";
    public bool oneShot = true;
    bool _used;

    [Header("Pan & Zoom")]
    public CameraPanZoom2D.Compass8 direction = CameraPanZoom2D.Compass8.N;
    [Tooltip("World units to pan in the chosen direction.")]
    public float panDistance = 3f;

    [Tooltip("If using PixelPerfect, this is the integer zoom (e.g., 1=default, 2=further out). Otherwise it's target orthographic size.")]
    public float targetZoomOrOrthoSize = 2f;
    public bool targetIsIntegerZoom = true;

    [Header("Timings (seconds)")]
    public float panOutSeconds = 0.6f;
    public float holdSeconds = 0.8f;
    public float panBackSeconds = 0.6f;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_used && oneShot) return;
        if (!other.CompareTag(playerTag)) return;
        if (!cameraController)
        {
            Debug.LogWarning("[CameraPanZoomTrigger2D] No cameraController assigned.");
            return;
        }

        cameraController.BeginPanZoom(direction, panDistance, targetZoomOrOrthoSize,
                                      panOutSeconds, holdSeconds, panBackSeconds,
                                      targetIsIntegerZoom);
        _used = true;
    }
}
