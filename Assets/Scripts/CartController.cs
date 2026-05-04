using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CartController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed     = 5f;
    public float slowMultiplier = 0.4f;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Vector2        moveInput;
    private bool           isSlowed  = false;
    private float          slowTimer = 0f;
    private bool           isFrozen  = false;
    private float          freezeTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearDamping  = 8f;
        gameObject.layer  = 6;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameActive)
        {
            moveInput = Vector2.zero;
            return;
        }

        // Freeze overrides slow
        if (isFrozen)
        {
            freezeTimer -= Time.deltaTime;
            moveInput = Vector2.zero;
            if (freezeTimer <= 0f)
            {
                isFrozen = false;
                if (sr) sr.color = Color.white;
            }
            return;
        }

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f) isSlowed = false;
        }
    }

    void FixedUpdate()
    {
        if (isFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float speed = isSlowed ? moveSpeed * slowMultiplier : moveSpeed;
        rb.linearVelocity = moveInput * speed;

        if (moveInput != Vector2.zero)
        {
            float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = Mathf.LerpAngle(rb.rotation, angle, 0.2f);
        }
    }

    public void ApplySlow(float duration)
    {
        if (isFrozen) return;
        isSlowed  = true;
        slowTimer = duration;
    }

    public void ApplyFreeze(float duration)
    {
        isFrozen    = true;
        freezeTimer = duration;
        isSlowed    = false;
        if (sr) sr.color = new Color(0.4f, 0.7f, 1f); // flash blue when frozen
    }
}
