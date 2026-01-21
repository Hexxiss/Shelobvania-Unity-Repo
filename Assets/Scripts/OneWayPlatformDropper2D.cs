using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class OneWayPlatformDropper2D : MonoBehaviour
{
    [Header("Input")]
    public KeyCode downKey = KeyCode.S;
    public KeyCode actionKey = KeyCode.Space; // hold S + tap Space to drop
    public KeyCode debugDropKey = KeyCode.K;  // optional single-key test

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.18f;
    public LayerMask groundMask; // MUST include OneWayPlatform

    [Header("Timing")]
    public float dropDuration = 0.35f;
    public float nudgeDownVelocity = -8f;

    [Header("Layer Names (optional belt-and-suspenders)")]
    public string playerLayerName = "Player";
    public string playerNoOneWayLayerName = "PlayerNoOneWay";
    public string oneWayPlatformLayerName = "OneWayPlatform";
    public bool alsoSwapLayersDuringDrop = true;

    Rigidbody2D _rb;
    Collider2D[] _playerColliders;
    int _playerLayer;
    int _playerNoOneWayLayer;
    int _oneWayPlatformLayer;
    bool _isDropping;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        // Grab ALL colliders on this object + children
        _playerColliders = GetComponentsInChildren<Collider2D>(includeInactive: false);

        _playerLayer = LayerMask.NameToLayer(playerLayerName);
        _playerNoOneWayLayer = LayerMask.NameToLayer(playerNoOneWayLayerName);
        _oneWayPlatformLayer = LayerMask.NameToLayer(oneWayPlatformLayerName);

        if (_oneWayPlatformLayer < 0)
            Debug.LogError("[Dropper] OneWayPlatform layer not found. Check names.");
    }

    void Update()
    {
        if (_isDropping) return;

        bool requested =
            (Input.GetKey(downKey) && Input.GetKeyDown(actionKey)) ||
            Input.GetKeyDown(debugDropKey); // allows single-key test (K)

        if (requested && IsStandingOnOneWayPlatform(out var platformCols))
            StartCoroutine(DropThrough(platformCols));
    }

    bool IsStandingOnOneWayPlatform(out List<Collider2D> platformCols)
    {
        platformCols = new List<Collider2D>();
        if (!groundCheck) return false;

        int mask = 1 << _oneWayPlatformLayer;
        var hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, mask);
        if (hits == null || hits.Length == 0) return false;

        // Collect actual colliders we�ll ignore
        foreach (var h in hits)
            if (h) platformCols.Add(h);

        return platformCols.Count > 0;
    }

    IEnumerator DropThrough(List<Collider2D> platformCols)
    {
        _isDropping = true;

        int originalLayer = gameObject.layer;

        // Optionally swap the entire hierarchy layer so all child colliders stop colliding too
        if (alsoSwapLayersDuringDrop && _playerNoOneWayLayer >= 0)
            SetLayerRecursively(gameObject, _playerNoOneWayLayer);

        // Ignore collisions between EVERY player collider and the platform colliders
        foreach (var pc in platformCols)
        {
            if (!pc) continue;
            foreach (var myCol in _playerColliders)
            {
                if (!myCol || !myCol.enabled) continue;
                Physics2D.IgnoreCollision(myCol, pc, true);
            }
        }

        // Strong downward nudge + sync so we separate this frame
        var v = _rb.linearVelocity;
        if (v.y > nudgeDownVelocity) v.y = nudgeDownVelocity;
        _rb.linearVelocity = v;
        Physics2D.SyncTransforms();

        yield return new WaitForSeconds(dropDuration);

        // Restore collisions
        foreach (var pc in platformCols)
        {
            if (!pc) continue;
            foreach (var myCol in _playerColliders)
            {
                if (!myCol) continue;
                Physics2D.IgnoreCollision(myCol, pc, false);
            }
        }

        if (alsoSwapLayersDuringDrop)
            SetLayerRecursively(gameObject, originalLayer);

        _isDropping = false;
    }

    void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
            SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
#endif
}
