using UnityEngine;

[RequireComponent(typeof(Transform))]
public class ArrowKeyCameraMover : MonoBehaviour
{
    
    public float moveSpeed = 8f;
    public bool useRigidbody2D = false;
    public KeyCode boostKey = KeyCode.LeftShift;
    public float boostMultiplier = 2f;

    Rigidbody2D rb;

    void Awake()
    {
        if (useRigidbody2D) rb = GetComponent<Rigidbody2D>();
        // Optional: if you're using a Camera, set orthographic to avoid perspective parallax
        var cam = GetComponent<Camera>();
        if (cam) cam.orthographic = true;
    }

    void Update()
    {
        if (!useRigidbody2D)
        {
            Vector2 input = ReadArrowInput();
            float speed = moveSpeed * (Input.GetKey(boostKey) ? boostMultiplier : 1f);
            Vector3 delta = new Vector3(input.x, input.y, 0f) * speed * Time.deltaTime;
            transform.position += delta; // XY only
        }
    }

    void FixedUpdate()
    {
        if (useRigidbody2D && rb != null)
        {
            Vector2 input = ReadArrowInput();
            float speed = moveSpeed * (Input.GetKey(boostKey) ? boostMultiplier : 1f);
            Vector2 targetPos = rb.position + input * speed * Time.fixedDeltaTime;
            rb.MovePosition(targetPos); // physics-friendly movement
        }
    }

    static Vector2 ReadArrowInput()
    {
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.UpArrow)) y += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) y -= 1f;
        Vector2 v = new Vector2(x, y);
        if (v.sqrMagnitude > 1f) v.Normalize();
        return v;
    }
}
