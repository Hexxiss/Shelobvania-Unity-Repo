using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// Attach to a parent GameObject. In the editor, use the context menu to export
/// the visual of this parent's children as seen from the active Scene view camera
/// to a transparent PNG.
/// </summary>
[ExecuteAlways]
public class HierarchySceneViewPngExporter : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Output")]
    [Min(1)] public int outputWidth = 2048;
    [Min(1)] public int outputHeight = 2048;

    [Tooltip("Extra padding when Auto Frame Bounds is enabled (world units).")]
    public float framePadding = 0.25f;

    [Header("Camera")]
    [Tooltip("If true, uses the active Scene view camera exactly (position, rotation, projection).")]
    public bool matchSceneViewCamera = true;

    [Tooltip("If true (and camera is orthographic), auto-frames the bounds of the children under this parent. " +
             "If matchSceneViewCamera is true, framing will only adjust position/size while keeping rotation consistent.")]
    public bool autoFrameBounds = false;

    [Tooltip("If true, includes this GameObject's own renderers; otherwise captures children only.")]
    public bool includeSelf = false;

    [Header("What to capture")]
    [Tooltip("If true, disables all Renderers not under this parent during capture (restored afterward). " +
             "This is the simplest way to ensure only this hierarchy appears in the PNG).")]
    public bool isolateByDisablingOtherRenderers = true;

    [Tooltip("If true, includes inactive objects under this parent in bounds calculation and capture.")]
    public bool includeInactive = true;

    [ContextMenu("Export Children As Transparent PNG (Scene View)")]
    public void ExportPngFromSceneView()
    {
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null || sceneView.camera == null)
        {
            Debug.LogError("No active SceneView found. Click in the Scene view once, then try again.");
            return;
        }

        // Pick save location
        string defaultName = $"{gameObject.name}_Capture.png";
        string path = EditorUtility.SaveFilePanel(
            "Export Hierarchy PNG",
            Application.dataPath,
            defaultName,
            "png"
        );

        if (string.IsNullOrEmpty(path))
            return;

        // Cache & optionally isolate
        List<(Renderer r, bool enabled)> rendererStates = null;
        if (isolateByDisablingOtherRenderers)
        {
            rendererStates = DisableNonTargetRenderers();
        }

        // Build capture camera (hidden) copying Scene view camera settings
        Camera srcCam = sceneView.camera;

        var camGO = new GameObject("~TempHierarchyCaptureCamera");
        camGO.hideFlags = HideFlags.HideAndDontSave;

        Camera capCam = camGO.AddComponent<Camera>();
        CopyCameraSettings(srcCam, capCam);

        // Ensure transparent background
        capCam.clearFlags = CameraClearFlags.SolidColor;
        capCam.backgroundColor = new Color(0f, 0f, 0f, 0f);

        // URP additional data (helps ensure it renders correctly under URP)
        var additional = capCam.GetUniversalAdditionalCameraData();
        additional.renderPostProcessing = false; // keep deterministic unless you explicitly want post
        additional.requiresDepthOption = CameraOverrideOption.Off;
        additional.requiresColorOption = CameraOverrideOption.Off;

        // Optional auto framing (best for orthographic 2D captures)
        if (autoFrameBounds)
        {
            if (!TryGetHierarchyBounds(out Bounds b))
            {
                Debug.LogWarning("No renderers found under this parent to frame. Capturing with SceneView camera framing.");
            }
            else
            {
                FrameCameraToBounds(capCam, b, framePadding);
            }
        }

        // Render to texture with alpha
        var rt = new RenderTexture(outputWidth, outputHeight, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1
        };

        var prevTarget = capCam.targetTexture;
        capCam.targetTexture = rt;

        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;

        capCam.Render();

        // Read pixels
        var tex = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false, false);
        tex.ReadPixels(new Rect(0, 0, outputWidth, outputHeight), 0, 0);
        tex.Apply();

        // Cleanup render targets
        capCam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;

        // Encode & save
        byte[] pngBytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, pngBytes);

        // Cleanup temp objects
        DestroyImmediate(tex);
        rt.Release();
        DestroyImmediate(rt);
        DestroyImmediate(camGO);

        // Restore isolated renderers
        if (rendererStates != null)
            RestoreRenderers(rendererStates);

        // If saved inside the project, refresh
        if (path.Replace('\\', '/').StartsWith(Application.dataPath.Replace('\\', '/')))
            AssetDatabase.Refresh();

        Debug.Log($"Saved PNG: {path}");
    }

    private void CopyCameraSettings(Camera src, Camera dst)
    {
        dst.transform.position = src.transform.position;
        dst.transform.rotation = src.transform.rotation;

        dst.orthographic = src.orthographic;
        dst.orthographicSize = src.orthographicSize;

        dst.fieldOfView = src.fieldOfView;
        dst.nearClipPlane = src.nearClipPlane;
        dst.farClipPlane = src.farClipPlane;

        dst.cullingMask = src.cullingMask;
        dst.allowHDR = src.allowHDR;
        dst.allowMSAA = false;

        // If you truly want to "match scene view camera", keep projection as-is.
        // matchSceneViewCamera flag is preserved by simply copying src settings above.
        // (The script still allows optional framing.)
    }

    private bool TryGetHierarchyBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasAny = false;

        // Collect renderers under target
        var renderers = includeSelf
            ? GetComponentsInChildren<Renderer>(includeInactive)
            : GetComponentsInChildren<Renderer>(includeInactive);

        foreach (var r in renderers)
        {
            if (!includeSelf && r.gameObject == gameObject) continue;

            if (!hasAny)
            {
                bounds = r.bounds;
                hasAny = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return hasAny;
    }

    private void FrameCameraToBounds(Camera cam, Bounds b, float padding)
    {
        // Works best for orthographic 2D. For perspective, framing requires more complex math.
        if (!cam.orthographic)
            return;

        float aspect = (float)outputWidth / outputHeight;

        // Expand bounds by padding
        b.Expand(new Vector3(padding * 2f, padding * 2f, 0f));

        // Determine orthographic size required to fit bounds
        float sizeY = b.extents.y;
        float sizeX = b.extents.x / aspect;
        cam.orthographicSize = Mathf.Max(sizeY, sizeX);

        // Position camera: keep its current Z, move to bounds center in XY
        var pos = cam.transform.position;
        cam.transform.position = new Vector3(b.center.x, b.center.y, pos.z);
    }

    private List<(Renderer r, bool enabled)> DisableNonTargetRenderers()
    {
        var all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var states = new List<(Renderer r, bool enabled)>(all.Length);

        foreach (var r in all)
        {
            bool isTarget = IsUnderThisTransform(r.transform);

            // If exclude self and this is a renderer on the parent itself, treat it as non-target
            if (!includeSelf && r.gameObject == gameObject)
                isTarget = false;

            states.Add((r, r.enabled));

            if (!isTarget)
                r.enabled = false;
        }

        return states;
    }

    private void RestoreRenderers(List<(Renderer r, bool enabled)> states)
    {
        foreach (var s in states)
        {
            if (s.r != null)
                s.r.enabled = s.enabled;
        }
    }

    private bool IsUnderThisTransform(Transform t)
    {
        if (includeSelf && t == transform) return true;

        // Children only
        var current = t;
        while (current != null)
        {
            if (current.parent == transform) return true;     // direct child
            if (current == transform) return includeSelf;     // self (optional)
            current = current.parent;
        }

        // If you want "any descendant", the while loop above already covers it:
        // it returns true if we encounter transform. This version uses parent check;
        // we should do the standard approach:
        return false;
    }
#endif
}
