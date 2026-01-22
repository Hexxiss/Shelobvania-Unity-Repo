using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[ExecuteAlways]
public class EnvironmentGroupTint : MonoBehaviour
{
 
    public Color tint = Color.white;
    [Range(0f, 1f)]
    public float alpha = 1f;
    public bool includeInactive = true;
    public bool affectUI = false;
    public bool affectTilemaps = true;

    // Cached targets + their original colors (god I fucking hope)
    private readonly List<SpriteRenderer> _sprites = new();
    private readonly List<Color> _spriteOriginalColors = new();

    private readonly List<Tilemap> _tilemaps = new();
    private readonly List<Color> _tilemapOriginalColors = new();

    private readonly List<Graphic> _graphics = new();
    private readonly List<Color> _graphicOriginalColors = new();

    // For URP property block path (SpriteRenderer supports it well)
    private MaterialPropertyBlock _mpb;

    // Common property names used by URP shaders.
    // Sprites: _Color is standard.
    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    private void OnEnable()
    {
        CacheChildren();
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // please god just work... 
        if (!isActiveAndEnabled) return;

        if (_sprites.Count == 0 && _tilemaps.Count == 0 && _graphics.Count == 0)
            CacheChildren();

        Apply();
    }
#endif

    [ContextMenu("Rebuild Cache")]
    public void CacheChildren()
    {
        _sprites.Clear();
        _spriteOriginalColors.Clear();

        _tilemaps.Clear();
        _tilemapOriginalColors.Clear();

        _graphics.Clear();
        _graphicOriginalColors.Clear();

        _mpb ??= new MaterialPropertyBlock();

        // SpriteRenderers
        var sprites = GetComponentsInChildren<SpriteRenderer>(includeInactive);
        foreach (var sr in sprites)
        {
            _sprites.Add(sr);
            _spriteOriginalColors.Add(sr.color);
        }

        // Tilemaps (optional)
        if (affectTilemaps)
        {
            var tilemaps = GetComponentsInChildren<Tilemap>(includeInactive);
            foreach (var tm in tilemaps)
            {
                _tilemaps.Add(tm);
                _tilemapOriginalColors.Add(tm.color);
            }
        }

        // UI Graphics (optional)
        if (affectUI)
        {
            var graphics = GetComponentsInChildren<Graphic>(includeInactive);
            foreach (var g in graphics)
            {
                _graphics.Add(g);
                _graphicOriginalColors.Add(g.color);
            }
        }

        Apply();
    }

    [ContextMenu("Apply Now")]
    public void Apply()
    {
        for (int i = 0; i < _sprites.Count; i++)
        {
            var sr = _sprites[i];
            if (sr == null) continue;

            var baseCol = _spriteOriginalColors[i];
            var final = MultiplyColorPreserveAlpha(baseCol, tint, alpha);

            // Use MPB so we don't create per-renderer materials.
            sr.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorProp, final);
            sr.SetPropertyBlock(_mpb);
        }

  
        for (int i = 0; i < _tilemaps.Count; i++)
        {
            var tm = _tilemaps[i];
            if (tm == null) continue;

            var baseCol = _tilemapOriginalColors[i];
            tm.color = MultiplyColorPreserveAlpha(baseCol, tint, alpha);
        }

        // UI Graphics (if enabled)
        for (int i = 0; i < _graphics.Count; i++)
        {
            var g = _graphics[i];
            if (g == null) continue;

            var baseCol = _graphicOriginalColors[i];
            g.color = MultiplyColorPreserveAlpha(baseCol, tint, alpha);
        }
    }

    [ContextMenu("Reset To Originals")]
    public void ResetToOriginals()
    {
        // Sprites
        for (int i = 0; i < _sprites.Count; i++)
        {
            var sr = _sprites[i];
            if (sr == null) continue;

            var original = _spriteOriginalColors[i];
            sr.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorProp, original);
            sr.SetPropertyBlock(_mpb);
        }

        // Tilemaps
        for (int i = 0; i < _tilemaps.Count; i++)
        {
            var tm = _tilemaps[i];
            if (tm == null) continue;

            tm.color = _tilemapOriginalColors[i];
        }

        // UI
        for (int i = 0; i < _graphics.Count; i++)
        {
            var g = _graphics[i];
            if (g == null) continue;

            g.color = _graphicOriginalColors[i];
        }
    }

    private static Color MultiplyColorPreserveAlpha(Color baseColor, Color tintColor, float groupAlpha)
    {
        return new Color(
            baseColor.r * tintColor.r,
            baseColor.g * tintColor.g,
            baseColor.b * tintColor.b,
            baseColor.a * tintColor.a * groupAlpha
        );
    }
}
