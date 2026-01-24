using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
/// Sets the Sorting Layer of all child renderers while keeping their "Order in Layer" intact.
/// Attach to a parent GameObject that contains environmental child objects.
[ExecuteAlways]
public class ChildSortingLayerSetter2D : MonoBehaviour
{

    [SerializeField] private string targetSortingLayerName = "Default";

    [SerializeField] private bool includeSpriteRenderers = true;

    [SerializeField] private bool includeTilemapRenderers = true;

    [SerializeField] private bool includeInactive = true;

    [SerializeField] private bool includeSelf = false;

    [SerializeField] private bool autoApplyInEditor = true;

    [SerializeField] private bool logResults = false;

    /// <summary>
    /// Public method you can call from other scripts.
    /// </summary>
    public void Apply()
    {
        if (!SortingLayerExists(targetSortingLayerName))
        {
            Debug.LogWarning($"[{nameof(ChildSortingLayerSetter2D)}] Sorting Layer '{targetSortingLayerName}' does not exist. No changes applied.", this);
            return;
        }

        int updated = 0;

        // SpriteRenderers
        if (includeSpriteRenderers)
        {
            var sprites = includeSelf
                ? GetComponentsInChildren<SpriteRenderer>(includeInactive)
                : GetComponentsInChildren<SpriteRenderer>(includeInactive);

            for (int i = 0; i < sprites.Length; i++)
            {
                // If not including self, skip if it's on this GameObject.
                if (!includeSelf && sprites[i].gameObject == gameObject) continue;

                if (sprites[i].sortingLayerName != targetSortingLayerName)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) Undo.RecordObject(sprites[i], "Set Sorting Layer (SpriteRenderer)");
#endif
                    sprites[i].sortingLayerName = targetSortingLayerName;
                    // IMPORTANT: do NOT modify sprites[i].sortingOrder
                    updated++;
#if UNITY_EDITOR
                    if (!Application.isPlaying) EditorUtility.SetDirty(sprites[i]);
#endif
                }
            }
        }

        // TilemapRenderers (optional)
        if (includeTilemapRenderers)
        {
            var tilemaps = includeSelf
                ? GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>(includeInactive)
                : GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>(includeInactive);

            for (int i = 0; i < tilemaps.Length; i++)
            {
                if (!includeSelf && tilemaps[i].gameObject == gameObject) continue;

                if (tilemaps[i].sortingLayerName != targetSortingLayerName)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) Undo.RecordObject(tilemaps[i], "Set Sorting Layer (TilemapRenderer)");
#endif
                    tilemaps[i].sortingLayerName = targetSortingLayerName;
                    // IMPORTANT: do NOT modify tilemaps[i].sortingOrder
                    updated++;
#if UNITY_EDITOR
                    if (!Application.isPlaying) EditorUtility.SetDirty(tilemaps[i]);
#endif
                }
            }
        }

        if (logResults)
            Debug.Log($"[{nameof(ChildSortingLayerSetter2D)}] Updated {updated} renderer(s) to Sorting Layer '{targetSortingLayerName}'.", this);
    }

    private static bool SortingLayerExists(string layerName)
    {
        // SortingLayer.NameToID returns 0 for "Default" and also for invalid names in some cases,
        // so do an explicit check against all layers.
        var layers = SortingLayer.layers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].name == layerName) return true;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!autoApplyInEditor) return;
        // Avoid spamming during compilation/import.
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        Apply();
    }
#endif
}
