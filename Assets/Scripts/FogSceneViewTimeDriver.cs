using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Drives _FogTime / _UseFogTime so the fog animates in Scene View (Edit Mode) and Play Mode.
/// Attach to the same GameObject as the fog SpriteRenderer (or a parent).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class FogSceneViewTimeDriver : MonoBehaviour
{
    private static readonly int FogTimeId = Shader.PropertyToID("_FogTime");
    private static readonly int UseFogTimeId = Shader.PropertyToID("_UseFogTime");

    [Tooltip("If assigned, only this renderer is driven. Otherwise, drives all child SpriteRenderers.")]
    public Renderer targetRenderer;

    [Tooltip("Speed multiplier for fog animation.")]
    public float timeScale = 1f;

    private readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();

#if UNITY_EDITOR
    private void OnEnable()
    {
        EditorApplication.update -= EditorTick;
        EditorApplication.update += EditorTick;
    }

    private void OnDisable()
    {
        EditorApplication.update -= EditorTick;
    }

    private void EditorTick()
    {
        if (Application.isPlaying) return;

        DriveTime();
        SceneView.RepaintAll();
    }
#endif

    private void Update()
    {
        DriveTime();
    }

    private void DriveTime()
    {
        float t;
        float use = 1f;

        if (Application.isPlaying)
        {
            t = Time.time * timeScale;
        }
        else
        {
#if UNITY_EDITOR
            t = (float)EditorApplication.timeSinceStartup * timeScale;
#else
            t = 0f;
            use = 0f;
#endif
        }

        if (targetRenderer != null)
        {
            ApplyToRenderer(targetRenderer, t, use);
            return;
        }

        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            ApplyToRenderer(renderers[i], t, use);
        }
    }

    private void ApplyToRenderer(Renderer r, float t, float useFogTime)
    {
        if (r == null) return;

        r.GetPropertyBlock(_mpb);
        _mpb.SetFloat(FogTimeId, t);
        _mpb.SetFloat(UseFogTimeId, useFogTime);
        r.SetPropertyBlock(_mpb);
    }
}
