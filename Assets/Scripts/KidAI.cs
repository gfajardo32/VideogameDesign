using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KidAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float directionChangeInterval = 0.8f;

    [Header("Bounds - keep kid inside the store")]
    public float boundX = 8f;
    public float boundY = 8f;

    private Rigidbody2D rb;
    private Vector2 currentDirection;
    private float dirTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 2f;
        PickNewDirection();
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameActive) return;

        dirTimer -= Time.fixedDeltaTime;
        if (dirTimer <= 0f) PickNewDirection();

        // Bounce off bounds
        Vector2 pos = rb.position;
        if (Mathf.Abs(pos.x) > boundX || Mathf.Abs(pos.y) > boundY)
        {
            currentDirection = -currentDirection;
            dirTimer = directionChangeInterval;
        }

        rb.linearVelocity = currentDirection * moveSpeed;
    }

    void PickNewDirection()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        currentDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        dirTimer = directionChangeInterval + Random.Range(-0.2f, 0.3f);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        PickNewDirection(); // bounce on any collision

        if (col.gameObject.CompareTag("Player"))
        {
            Rigidbody2D playerRb = col.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 pushDir = (col.transform.position - transform.position).normalized;
                playerRb.AddForce(pushDir * 5f, ForceMode2D.Impulse);
            }
        }
    }
}
