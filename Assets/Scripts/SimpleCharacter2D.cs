using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SimpleCharacter2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;          // Horizontal speed
    public float acceleration = 100f;     // How quickly we reach target speed
    public float deceleration = 100f;     // How quickly we slow to zero

    [Header("Jump")]
    public float jumpForce = 14f;         // Initial jump velocity
    public float gravityScale = 3.5f;     // Override for consistent feel
    public bool variableJump = true;      // Release jump early to cut height
    public float jumpCutMultiplier = 0.5f;

    [Header("Ground Check")]
    public Transform groundCheck;         // Empty child at feet
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayers;

    [Header("Jump Assist (optional)")]
    public float coyoteTime = 0.1f;       // Allow jump shortly after leaving edge
    public float jumpBufferTime = 0.1f;   // Queue jump shortly before landing

    Rigidbody2D rb;
    float inputX;
    bool jumpPressed;
    bool jumpReleased;

    float lastGroundedTime;
    float lastJumpPressedTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.freezeRotation = true; // Prevent tipping over
    }

    void Update()
    {
        // --- INPUT ---
        inputX = 0f;
        if (Input.GetKey(KeyCode.A)) inputX -= 1f;
        if (Input.GetKey(KeyCode.D)) inputX += 1f;

        if (Input.GetKeyDown(KeyCode.Space))
            lastJumpPressedTime = jumpBufferTime; // start/refresh jump buffer

        jumpPressed = Input.GetKeyDown(KeyCode.Space);
        jumpReleased = Input.GetKeyUp(KeyCode.Space);

        // --- TIMERS ---
        bool groundedNow = IsGrounded();
        if (groundedNow) lastGroundedTime = coyoteTime;

        // Countdown
        if (lastGroundedTime > 0f) lastGroundedTime -= Time.deltaTime;
        if (lastJumpPressedTime > 0f) lastJumpPressedTime -= Time.deltaTime;

        // --- JUMP TRIGGER (in Update to be responsive) ---
        bool canCoyoteJump = lastGroundedTime > 0f;
        bool bufferedJump = lastJumpPressedTime > 0f;
        if (bufferedJump && canCoyoteJump)
        {
            DoJump();
            // consume both buffers
            lastJumpPressedTime = 0f;
            lastGroundedTime = 0f;
        }

        // Variable jump height (cut velocity when releasing jump)
        if (variableJump && jumpReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        // Smooth horizontal movement
        float targetSpeed = inputX * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accel = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = accel * speedDiff * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    void FixedUpdate()
    {
        // Smooth horizontal movement
        //float targetSpeed = inputX * moveSpeed;
        //float speedDiff = targetSpeed - rb.linearVelocity.x;
        //float accel = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        //float movement = accel * speedDiff * Time.fixedDeltaTime;
       //rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    void DoJump()
    {
        // Set y velocity directly for a crisp jump
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    bool IsGrounded()
    {
        if (!groundCheck) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayers) != null;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
