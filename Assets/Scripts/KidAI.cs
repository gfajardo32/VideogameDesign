using UnityEngine;
using System.Collections;

public enum ChaseStyle { Direct, Ahead, Flank, Shy }

/// Pac-Man ghost style kid NPC.
/// Always shown as a small red dot — dangerous on contact.
[RequireComponent(typeof(Rigidbody2D))]
public class KidAI : MonoBehaviour
{
    [Header("Personality")]
    public ChaseStyle chaseStyle = ChaseStyle.Direct;

    [Header("Movement")]
    public float patrolSpeed       = 1.6f;
    public float baseChaseSpeed    = 2.4f;
    public float chaseSpeedPerItem = 0.35f;
    public float dirChangeInterval = 1.8f;

    [Header("Detection - Pac-Man style")]
    public float detectionRadius    = 5.5f;
    public float loseInterestRadius = 9f;
    [Range(0f, 1f)]
    public float loseInterestChance = 0.35f;

    [Header("Separation")]
    public float separationRadius = 2.5f;
    public float separationForce  = 3f;

    [Header("Contact")]
    public float contactRadius   = 0.7f;
    public float contactCooldown = 2f;

    [Header("Bounds")]
    public float boundX = 15f;
    public float boundY = 12f;

    private enum AIState { Patrol, Chase }
    private AIState state = AIState.Patrol;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Transform      player;
    private Vector2        patrolDir;
    private float          dirTimer          = 0f;
    private bool           onCooldown        = false;
    private float          loseInterestTimer = 0f;

    static readonly Color KidColor = new Color(0.95f, 0.1f, 0.1f); // red dot always

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearDamping  = 4f;
        gameObject.layer  = 8;
        sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = KidColor;
        PickNewPatrolDir();
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameActive) return;
        if (player == null) return;

        float dist = Vector2.Distance(rb.position, (Vector2)player.position);

        if (state == AIState.Patrol)
        {
            if (dist < detectionRadius)
            {
                state = AIState.Chase;
                loseInterestTimer = 0f;
            }
        }
        else
        {
            if (dist < loseInterestRadius)
            {
                loseInterestTimer = 0f;
            }
            else
            {
                loseInterestTimer += Time.fixedDeltaTime;
                if (loseInterestTimer >= 1f)
                {
                    loseInterestTimer = 0f;
                    if (Random.value < loseInterestChance)
                        state = AIState.Patrol;
                }
            }
        }

        // Always red — no color change between states
        if (sr) sr.color = KidColor;

        Vector2 moveVelocity;

        if (state == AIState.Chase)
        {
            int items = ShoppingList.Instance != null ? ShoppingList.Instance.CollectedCount : 0;
            float speed = baseChaseSpeed + items * chaseSpeedPerItem;
            moveVelocity = (GetChaseTarget() - rb.position).normalized * speed;
        }
        else
        {
            dirTimer -= Time.fixedDeltaTime;
            if (dirTimer <= 0f) PickNewPatrolDir();

            Vector2 pos = rb.position;
            if (pos.x < -boundX || pos.x > boundX) { patrolDir.x = -patrolDir.x; dirTimer = dirChangeInterval; }
            if (pos.y < -boundY || pos.y > boundY) { patrolDir.y = -patrolDir.y; dirTimer = dirChangeInterval; }

            moveVelocity = patrolDir * patrolSpeed;
        }

        // Separation steering
        Vector2 separation = Vector2.zero;
        foreach (var other in FindObjectsByType<KidAI>(FindObjectsSortMode.None))
        {
            if (other == this) continue;
            float d = Vector2.Distance(rb.position, (Vector2)other.transform.position);
            if (d < separationRadius && d > 0.01f)
                separation += (rb.position - (Vector2)other.transform.position).normalized * (separationRadius - d);
        }
        moveVelocity += separation * separationForce * Time.fixedDeltaTime;

        rb.linearVelocity = moveVelocity;

        if (!onCooldown && dist < contactRadius)
            StartCoroutine(TriggerContact());
    }

    Vector2 GetChaseTarget()
    {
        Vector2 playerPos = (Vector2)player.position;
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Vector2 playerVel = playerRb != null ? playerRb.linearVelocity : Vector2.zero;

        switch (chaseStyle)
        {
            case ChaseStyle.Ahead:
                return playerPos + playerVel.normalized * 3f;
            case ChaseStyle.Flank:
                Vector2 toPlayer = playerPos - rb.position;
                Vector2 perp = new Vector2(-toPlayer.y, toPlayer.x).normalized;
                return playerPos + perp * 2.5f;
            case ChaseStyle.Shy:
                if (Vector2.Distance(rb.position, playerPos) < 3f)
                    return rb.position - (playerPos - rb.position).normalized * 2f;
                return playerPos;
            default:
                return playerPos;
        }
    }

    IEnumerator TriggerContact()
    {
        onCooldown = true;
        DropObstacle drop = GetComponent<DropObstacle>();
        if (drop != null) drop.TriggerDrop();
        UIManager.Instance?.ShowNotification("A kid crashed into your cart!", 2f);
        yield return new WaitForSeconds(contactCooldown);
        onCooldown = false;
    }

    void PickNewPatrolDir()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        patrolDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        dirTimer  = dirChangeInterval + Random.Range(-0.4f, 1f);
    }
}
