using UnityEngine;
using System.Collections;

/// Pac-Man ghost style security guard.
/// Always shown as a blue dot — most persistent chaser in the store.
/// Freezes the player on contact.
[RequireComponent(typeof(Rigidbody2D))]
public class SecurityGuard : MonoBehaviour
{
    [Header("Movement")]
    public float patrolSpeed       = 1.2f;
    public float baseChaseSpeed    = 2.8f;
    public float chaseSpeedPerItem = 0.3f;
    public float dirChangeInterval = 1.5f;

    [Header("Grace Period")]
    public float gracePeriod = 8f;

    [Header("Detection - Pac-Man style")]
    public float detectionRadius    = 6f;
    public float loseInterestRadius = 10f;
    [Range(0f, 1f)]
    public float loseInterestChance = 0.15f; // very stubborn

    [Header("Contact")]
    public float freezeDuration  = 3f;
    public float contactRadius   = 0.8f;
    public float contactCooldown = 6f;

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
    private float          graceTimer        = 0f;
    private bool           graceOver         = false;

    static readonly Color GuardColor = new Color(0.15f, 0.35f, 1f); // blue dot always

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearDamping  = 5f;
        gameObject.layer  = 8;
        sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = GuardColor;
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

        if (graceOver)
        {
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
        }

        // Always blue — no color change, but you'll know it's the guard
        if (sr) sr.color = GuardColor;

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

        if (!onCooldown && dist < contactRadius)
            StartCoroutine(TriggerContact());
    }

    IEnumerator TriggerContact()
    {
        onCooldown = true;
        state = AIState.Patrol;
        PickNewPatrolDir();

        CartController cart = player?.GetComponent<CartController>();
        if (cart != null) cart.ApplyFreeze(freezeDuration);

        UIManager.Instance?.ShowNotification($"Busted! Frozen for {(int)freezeDuration}s!", 3f);

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
