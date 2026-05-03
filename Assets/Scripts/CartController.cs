using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CartController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float slowMultiplier = 0.4f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isSlowed = false;
    private float slowTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearDamping  = 8f;
        gameObject.layer  = 6; // Player layer — physics collision with NPCs (layer 8) is ignored
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameActive)
        {
            moveInput = Vector2.zero;
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
        isSlowed  = true;
        slowTimer = duration;
    }
}
