using UnityEngine;

public class Parallax3Layers : MonoBehaviour
{
    //Camera that follows the player. If empty, uses Camera.main.
    public Transform targetCamera;

    //Parallax Layers (back -> front)
    public Transform Parallax1; // furthest, slowest
    public Transform Parallax2; // mid
    public Transform Parallax3; // closest, fastest

    //maller = slower movement (appears farther away)
    public Vector2 parallax1Multiplier = new Vector2(0.15f, 0.00f);
    public Vector2 parallax2Multiplier = new Vector2(0.35f, 0.00f);
    public Vector2 parallax3Multiplier = new Vector2(0.60f, 0.00f);


    //Apply parallax on the Y axis too (useful for vertical levels)
    public bool parallaxY = false;

    //Do the offset in LateUpdate (after camera moves) to reduce jitter
    public bool useLateUpdate = true;

    //Round to pixels to reduce sub-pixel shimmer (for pixel art). Set to 0 to disable.
    public float pixelPerUnit = 0f;

    Vector3 prevCamPos;

    void Reset()
    {
        // Sensible defaults if dropped onto an empty GameObject in scene
        var cam = Camera.main;
        if (cam) targetCamera = cam.transform;
    }

    void OnEnable()
    {
        if (!targetCamera)
        {
            var cam = Camera.main;
            if (cam) targetCamera = cam.transform;
        }
        prevCamPos = targetCamera ? targetCamera.position : Vector3.zero;
    }

    void Update()
    {
        if (!useLateUpdate) DoParallax();
    }

    void LateUpdate()
    {
        if (useLateUpdate) DoParallax();
    }

    void DoParallax()
    {
        if (!targetCamera) return;

        Vector3 camPos = targetCamera.position;
        Vector3 delta = camPos - prevCamPos;

        // Zero out Y if we don't want vertical parallax
        if (!parallaxY) delta.y = 0f;

        // Apply to each layer (keep original Z)
        Apply(Parallax1, delta, parallax1Multiplier);
        Apply(Parallax2, delta, parallax2Multiplier);
        Apply(Parallax3, delta, parallax3Multiplier);

        prevCamPos = camPos;
    }

    void Apply(Transform layer, Vector3 delta, Vector2 mult)
    {
        if (!layer) return;

        Vector3 pos = layer.position;
        pos += new Vector3(delta.x * mult.x, delta.y * mult.y, 0f);

        if (pixelPerUnit > 0f)
            pos = PixelSnap(pos, pixelPerUnit);

        layer.position = pos;
    }

    static Vector3 PixelSnap(Vector3 worldPos, float ppu)
    {
        // Rounds X/Y to the nearest pixel grid to reduce shimmer in pixel art
        worldPos.x = Mathf.Round(worldPos.x * ppu) / ppu;
        worldPos.y = Mathf.Round(worldPos.y * ppu) / ppu;
        return worldPos;
    }
}
