using UnityEngine;

[ExecuteAlways]
public class TriggerGizmo2D : MonoBehaviour
{
    public Color color = new Color(1f, 0.4f, 0f, 0.25f); // orange, semi-transparent
    public bool solid = true; // fill vs wire

    void OnDrawGizmos()
    {
        var c = GetComponent<Collider2D>();
        if (!c) return;

        Gizmos.color = color;

        if (c is BoxCollider2D box)
        {
            var m = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = m;
            var size = new Vector3(box.size.x, box.size.y, 0f);
            if (solid) Gizmos.DrawCube(box.offset, size);
            else Gizmos.DrawWireCube(box.offset, size);
        }
        else if (c is CircleCollider2D circle)
        {
            // approximate with a cube outline; Scene gizmos are for authoring clarity
            var m = Matrix4x4.TRS(transform.position + (Vector3)circle.offset, transform.rotation, Vector3.one);
            Gizmos.matrix = m;
            float d = circle.radius * 2f;
            var size = new Vector3(d, d, 0f);
            if (solid) Gizmos.DrawCube(Vector3.zero, size);
            else Gizmos.DrawWireCube(Vector3.zero, size);
        }
        else if (c is PolygonCollider2D poly)
        {
            var m = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = m;
            for (int p = 0; p < poly.pathCount; p++)
            {
                var path = poly.GetPath(p);
                for (int i = 0; i < path.Length; i++)
                {
                    var a = (Vector3)path[i];
                    var b = (Vector3)path[(i + 1) % path.Length];
                    Gizmos.DrawLine(a, b);
                }
            }
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
}