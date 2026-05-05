using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class SecurityGuard : MonoBehaviour
{
    [Header("Movement")]
    public float patrolSpeed       = 1.2f;
    public float baseChaseSpeed    = 2.8f;
    public float chaseSpeedPerItem = 0.3f;
    public float fleeSpeed         = 1.8f;
    public float fleeDuration      = 5f;
    public float dirChangeInterval = 1.5f;

    [Header("Grace Period")]
    public float gracePeriod = 8f;

    [Header("Detection")]
    public float detectionRadius    = 6f;
    public float loseInterestRadius = 10f;
    [Range(0f, 1f)]
    public float loseInterestChance = 0.15f;

    [Header("Contact")]
    public float freezeDuration  = 3f;
    public float contactRadius   = 0.8f;
    public float contactCooldown = 6f;

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
    private float          graceTimer        = 0f;
    private bool           graceOver         = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearDamping  = 5f;
        gameObject.layer  = 8;
        sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = Color.white;
        PickNewPatrolDir();
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        graceTimer = gracePeriod;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameActive) return;
        if (player == null) return;

        if (!graceOver)
        {
            graceTimer -= Time.fixedDeltaTime;
            if (graceTimer <= 0f) graceOver = true;
        }

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

        if (graceOver)
        {
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
        }

        Vector2 moveVelocity;

        if (state == AIState.Chase && graceOver)
        {
            int items = ShoppingList.Instance != null ? ShoppingList.Instance.CollectedCount : 0;
            float speed = baseChaseSpeed + items * chaseSpeedPerItem;
            Vector2 dir = ((Vector2)player.position - rb.position).normalized;
            moveVelocity = dir * speed;
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

    IEnumerator TriggerContact()
    {
        onCooldown = true;

        CartController cart = player?.GetComponent<CartController>();
        if (cart != null) cart.ApplyFreeze(freezeDuration);
        UIManager.Instance?.ShowNotification($"Busted! Frozen for {(int)freezeDuration}s!", 3f);

        // Walk away from the player for fleeDuration seconds
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
        dirTimer  = dirChangeInterval + Random.Range(-0.3f, 0.8f);
    }
}
