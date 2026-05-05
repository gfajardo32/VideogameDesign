using UnityEngine;
using System.Collections;

public enum ChaseStyle { Direct, Ahead, Flank, Shy }

[RequireComponent(typeof(Rigidbody2D))]
public class KidAI : MonoBehaviour
{
    [Header("Personality")]
    public ChaseStyle chaseStyle = ChaseStyle.Direct;

    [Header("Movement")]
    public float patrolSpeed       = 1.6f;
    public float baseChaseSpeed    = 2.4f;
    public float chaseSpeedPerItem = 0.35f;
    public float fleeSpeed         = 3f;
    public float fleeDuration      = 5f;
    public float dirChangeInterval = 1.8f;

    [Header("Detection")]
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

    [Header("Sprites ??? assign via GroceryRush/Assign Sprites")]
    public Sprite spriteWalkRight;
    public Sprite spriteIdleDown;
    public Sprite spriteIdleUp;

    private enum AIState { Patrol, Chase, Flee }
    private AIState state = AIState.Patrol;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Transform      player;
    private Vector2        patrolDir;
    private Vector2        fleeDir;
    private float          fleeTimer         = 0f;
    private float          dirTimer          = 0f;
    private bool           onCooldown        = false;
    private float          loseInterestTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearDamping  = 4f;
        gameObject.layer  = 8;
        sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = Color.white;
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

        // ---- Flee overrides everything ----
        if (state == AIState.Flee)
        {
            fleeTimer -= Time.fixedDeltaTime;
            if (fleeTimer <= 0f)
            {
                state = AIState.Patrol;
                PickNewPatrolDir();
            }
            else
            {
                Vector2 awayFromPlayer = (rb.position - (Vector2)player.position).normalized;
                fleeDir = Vector2.Lerp(fleeDir, awayFromPlayer, 3f * Time.fixedDeltaTime).normalized;
                rb.linearVelocity = fleeDir * fleeSpeed;
                UpdateSprite(rb.linearVelocity);
                return;
            }
        }

        if (state == AIState.Patrol)
        {
            if (dist < detectionRadius) { state = AIState.Chase; loseInterestTimer = 0f; }
        }
        else if (state == AIState.Chase)
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
                    if (Random.value < loseInterestChance) state = AIState.Patrol;
                }
            }
        }

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

        // Separation steering (skip during flee ??? already handled above)
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
        UpdateSprite(moveVelocity);

        if (!onCooldown && dist < contactRadius)
            StartCoroutine(TriggerContact());
    }

    void UpdateSprite(Vector2 vel)
    {
        if (sr == null) return;
        bool moving = vel.sqrMagnitude > 0.05f;

        if (!moving)
        {
            if (spriteIdleDown) { sr.sprite = spriteIdleDown; sr.flipX = false; }
            return;
        }

        if (Mathf.Abs(vel.x) >= Mathf.Abs(vel.y))
        {
            if (spriteWalkRight) { sr.sprite = spriteWalkRight; sr.flipX = vel.x < 0; }
        }
        else if (vel.y > 0)
        {
            if (spriteIdleUp) { sr.sprite = spriteIdleUp; sr.flipX = false; }
        }
        else
        {
            if (spriteIdleDown) { sr.sprite = spriteIdleDown; sr.flipX = false; }
        }
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
        SfxPlayer.Play("cart-bump");
        UIManager.Instance?.ShowNotification("A kid crashed into your cart!", 2f);

        // Flee away from the player for fleeDuration seconds
        fleeDir   = (rb.position - (Vector2)player.position).normalized;
        fleeTimer = fleeDuration;
        state     = AIState.Flee;

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
