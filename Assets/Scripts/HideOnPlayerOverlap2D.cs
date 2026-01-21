using UnityEngine;
using System.Linq;

/// Attach to the object whose sprite(s) should hide when the Player overlaps.
/// Works with Collider2D set as Trigger or non-Trigger (Collision).
[RequireComponent(typeof(Collider2D))]
public class HideOnPlayerOverlap2D : MonoBehaviour
{
    [Tooltip("The tag your player uses.")]
    public string playerTag = "Player";

    [Tooltip("Affects all SpriteRenderers on this GameObject and its children.")]
    public bool includeChildren = true;

    // Track how many Player colliders are currently overlapping (robust for multi-collider players)
    int playerOverlapCount = 0;
    SpriteRenderer[] renderers;

    void Awake()
    {
        renderers = includeChildren
            ? GetComponentsInChildren<SpriteRenderer>(true)
            : new[] { GetComponent<SpriteRenderer>() }.Where(r => r != null).ToArray();

        if (renderers.Length == 0)
            Debug.LogWarning($"[HideOnPlayerOverlap2D] No SpriteRenderer found on {name} (or children).");
    }

    void OnTriggerEnter2D(Collider2D other) { HandleEnter(other.gameObject); }
    void OnTriggerExit2D(Collider2D other) { HandleExit(other.gameObject); }
    void OnCollisionEnter2D(Collision2D col) { HandleEnter(col.collider.gameObject); }
    void OnCollisionExit2D(Collision2D col) { HandleExit(col.collider.gameObject); }

    void HandleEnter(GameObject other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerOverlapCount++;
        SetAlpha(0f);
    }

    void HandleExit(GameObject other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);
        if (playerOverlapCount == 0)
            SetAlpha(1f);
    }

    void SetAlpha(float a)
    {
        foreach (var r in renderers)
        {
            if (!r) continue;
            var c = r.color;
            c.a = a;
            r.color = c;
        }
    }
}
