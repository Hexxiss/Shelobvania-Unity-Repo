using UnityEngine;

[DisallowMultipleComponent]
public class InfiniteSpriteScroll : MonoBehaviour
{
    public enum ScrollDirection { Left, Right }

    [Header("Scroll")]
    [SerializeField] private ScrollDirection direction = ScrollDirection.Left;
    [SerializeField] private float speedUnitsPerSecond = 2f;

    [Header("Wrap (World X Positions)")]
    [Tooltip("When moving Left: if X <= endX, wrap. When moving Right: if X >= endX, wrap.")]
    [SerializeField] private float endX = -20f;

    [Tooltip("Position to teleport back to after reaching endX.")]
    [SerializeField] private float restartX = 20f;

    [Header("Optional: Auto-calculate RestartX from Sprite Width")]
    [Tooltip("If enabled and restartX is left at 0, restartX will be computed as endX +/- sprite width.")]
    [SerializeField] private bool autoRestartFromSpriteWidth = false;

    [Tooltip("Extra spacing added when auto-calculating restartX (world units).")]
    [SerializeField] private float extraGap = 0f;

    private float _sign;
    private SpriteRenderer _sr;
    private bool _autoRestartValid;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sign = (direction == ScrollDirection.Left) ? -1f : 1f;

        // Optional auto-calc restart point from sprite width
        if (autoRestartFromSpriteWidth)
        {
            if (_sr != null && _sr.sprite != null)
            {
                float width = GetSpriteWorldWidth(_sr);
                // If restartX is exactly 0, treat it as "not configured" and auto-calc it.
                if (Mathf.Approximately(restartX, 0f))
                {
                    // If moving left, restart should be to the right of endX, and vice versa.
                    restartX = endX + (direction == ScrollDirection.Left ? (width + extraGap) : -(width + extraGap));
                }
                _autoRestartValid = true;
            }
            else
            {
                Debug.LogWarning($"{nameof(InfiniteSpriteScroll)}: autoRestartFromSpriteWidth enabled, but no SpriteRenderer/sprite found. Using manual restartX.");
                _autoRestartValid = false;
            }
        }
    }

    private void OnValidate()
    {
        // Keep sign updated when direction changes in Inspector
        _sign = (direction == ScrollDirection.Left) ? -1f : 1f;

        speedUnitsPerSecond = Mathf.Max(0f, speedUnitsPerSecond);
    }

    private void Update()
    {
        // Move
        Vector3 pos = transform.position;
        pos.x += _sign * speedUnitsPerSecond * Time.deltaTime;
        transform.position = pos;

        // Wrap logic based on direction
        if (direction == ScrollDirection.Left)
        {
            if (transform.position.x <= endX)
            {
                TeleportToRestart();
            }
        }
        else // Right
        {
            if (transform.position.x >= endX)
            {
                TeleportToRestart();
            }
        }
    }

    private void TeleportToRestart()
    {
        Vector3 pos = transform.position;
        pos.x = restartX;
        transform.position = pos;
    }

    private static float GetSpriteWorldWidth(SpriteRenderer sr)
    {
        // Sprite bounds are already in world units after scale is applied in renderer.bounds.
        return sr.bounds.size.x;
    }
}
