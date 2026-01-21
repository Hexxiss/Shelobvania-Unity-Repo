using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FaceMoveDirection2D : MonoBehaviour
{
    public Rigidbody2D rb;                 // optional: assign if you want to use velocity
    public bool useVelocity = false;       // true = use rb.velocity.x, false = use input
    public KeyCode left = KeyCode.A;
    public KeyCode right = KeyCode.D;

    SpriteRenderer sr;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        float x = useVelocity && rb ? rb.linearVelocity.x :
                  (Input.GetKey(left) ? 1f : 0f) + (Input.GetKey(right) ? -1f : 0f);

        if (x > 0.01f) sr.flipX = false;  // facing right
        else if (x < -0.01f) sr.flipX = true; // facing left
    }
}
