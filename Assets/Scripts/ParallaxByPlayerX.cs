using UnityEngine;

[DisallowMultipleComponent]
public class ParallaxByPlayerX : MonoBehaviour
{
    public Transform player;
    public float maxOffsetPixels = 20f;
    public float responsiveness = 0.06f;
    public float smoothTime = 0.12f;
    public float pixelsPerUnit = 0f;

    float _startX;
    float _velX; // for SmoothDamp cache

    void Awake()
    {
        _startX = transform.position.x;

        if (pixelsPerUnit <= 0f)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr && sr.sprite) pixelsPerUnit = sr.sprite.pixelsPerUnit;
            // Fallback if still zero: many projects use 100 PPU
            if (pixelsPerUnit <= 0f) pixelsPerUnit = 100f;
        }
    }

    void LateUpdate()
    {
        if (!player) return;

        // Convert max pixel offset to world units
        float maxUnits = maxOffsetPixels / pixelsPerUnit;

        // Player delta relative to THIS OBJECT'S ORIGINAL X
        float delta = player.position.x - _startX;

        // Desired offset: player to right -> move object left (negative), and vice versa
        float desiredOffset = Mathf.Clamp(-delta * responsiveness, -maxUnits, maxUnits);

        float targetX = _startX + desiredOffset;

        // Smoothly approach target
        float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref _velX, smoothTime);

        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    // Optional: call this at runtime if you reposition the object and want to re-anchor the parallax origin.
    [ContextMenu("Reset Parallax Origin To Current X")]
    public void ResetOriginX()
    {
        _startX = transform.position.x;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (pixelsPerUnit <= 0f)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr && sr.sprite) pixelsPerUnit = sr.sprite.pixelsPerUnit;
            if (pixelsPerUnit <= 0f) pixelsPerUnit = 100f;
        }

        float maxUnits = maxOffsetPixels / pixelsPerUnit;
        float left = Application.isPlaying ? _startX - maxUnits : transform.position.x - maxUnits;
        float right = Application.isPlaying ? _startX + maxUnits : transform.position.x + maxUnits;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(left, transform.position.y - 0.25f, transform.position.z),
                        new Vector3(left, transform.position.y + 0.25f, transform.position.z));
        Gizmos.DrawLine(new Vector3(right, transform.position.y - 0.25f, transform.position.z),
                        new Vector3(right, transform.position.y + 0.25f, transform.position.z));
    }
#endif
}
