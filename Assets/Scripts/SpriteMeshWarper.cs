using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpriteMeshWarper : MonoBehaviour
{
    [Header("Source Sprite")]
    [SerializeField] private Sprite sprite;

    [Header("Mesh Subdivision")]
    [Range(2, 200)][SerializeField] private int gridX = 40;
    [Range(2, 200)][SerializeField] private int gridY = 40;

    [Header("Warp")]
    [SerializeField] private float amplitude = 0.15f;     // world units
    [SerializeField] private float frequency = 2.0f;      // cycles across mesh
    [SerializeField] private float speed = 1.0f;          // time multiplier
    [SerializeField] private bool warpX = false;          // if false, warp Y

    private Mesh _mesh;
    private Vector3[] _baseVerts;
    private Vector3[] _deformedVerts;

    private void Awake()
    {
        if (sprite == null)
        {
            Debug.LogError($"{nameof(SpriteMeshWarper)}: Assign a Sprite.");
            enabled = false;
            return;
        }

        BuildMesh();
        SetupMaterial();
    }

    private void Update()
    {
        if (_mesh == null) return;

        float t = Time.time * speed;

        for (int i = 0; i < _deformedVerts.Length; i++)
        {
            Vector3 v = _baseVerts[i];

            // Normalize to 0..1 across the plane using UV-ish mapping from position
            // (Assumes mesh spans -0.5..+0.5 in local space)
            float nx = v.x + 0.5f;
            float ny = v.y + 0.5f;

            // A simple wave; you can replace with Perlin noise, multiple octaves, etc.
            float wave = Mathf.Sin((warpX ? ny : nx) * Mathf.PI * 2f * frequency + t) * amplitude;

            if (warpX) v.x += wave;
            else v.y += wave;

            _deformedVerts[i] = v;
        }

        _mesh.vertices = _deformedVerts;
        _mesh.RecalculateBounds();
        // Normals not strictly required for unlit sprites; omit for speed.
    }

    private void BuildMesh()
    {
        _mesh = new Mesh();
        _mesh.name = "WarpedSpriteMesh";

        int vertCount = (gridX + 1) * (gridY + 1);
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] tris = new int[gridX * gridY * 6];

        int v = 0;
        for (int y = 0; y <= gridY; y++)
        {
            float fy = (float)y / gridY;           // 0..1
            float py = fy - 0.5f;                  // -0.5..0.5

            for (int x = 0; x <= gridX; x++)
            {
                float fx = (float)x / gridX;       // 0..1
                float px = fx - 0.5f;              // -0.5..0.5

                verts[v] = new Vector3(px, py, 0f);
                uvs[v] = new Vector2(fx, fy);
                v++;
            }
        }

        int t = 0;
        for (int y = 0; y < gridY; y++)
        {
            for (int x = 0; x < gridX; x++)
            {
                int i0 = y * (gridX + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (gridX + 1);
                int i3 = i2 + 1;

                // two triangles per cell
                tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                tris[t++] = i1; tris[t++] = i2; tris[t++] = i3;
            }
        }

        _mesh.vertices = verts;
        _mesh.uv = uvs;
        _mesh.triangles = tris;
        _mesh.RecalculateBounds();

        var mf = GetComponent<MeshFilter>();
        mf.sharedMesh = _mesh;

        _baseVerts = _mesh.vertices;
        _deformedVerts = new Vector3[_baseVerts.Length];
    }

    private void SetupMaterial()
    {
        var mr = GetComponent<MeshRenderer>();

        // Use URP Unlit by default (works for most 2D cases).
        // If you want 2D lighting interaction, you'd use a Sprite-Lit shader instead.
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null)
        {
            Debug.LogError("URP Unlit shader not found. Ensure URP is installed and active.");
            enabled = false;
            return;
        }

        Material mat = new Material(s);
        mat.mainTexture = sprite.texture;
        mr.sharedMaterial = mat;

        // Scale the object so that 1 unit roughly matches sprite size in world units
        // based on sprite pixels per unit. This preserves the sprite’s aspect ratio.
        float ppu = sprite.pixelsPerUnit;
        Vector2 sizePx = sprite.rect.size;
        Vector2 sizeWorld = sizePx / ppu;
        transform.localScale = new Vector3(sizeWorld.x, sizeWorld.y, 1f);
    }
}
