using UnityEngine;
using System.Collections;

/// Pac-Man ghost style adult shopper NPC.
/// Green dot while patrolling. Turns purple when chasing.
/// Looks identical to NeutralShopper while calm — player can't tell them apart.
[RequireComponent(typeof(Rigidbody2D))]
public class ShopperAI : MonoBehaviour
{
    [Header("Movement")]
    public float patrolSpeed       = 1.4f;
    public float baseChaseSpeed    = 2.2f;
    public float dirChangeInterval = 2f;

    [Header("Detection - Pac-Man style")]
    public float detectionRadius    = 5f;
    public float loseInterestRadius = 8.5f;
    [Range(0f, 1f)]
    public float loseInterestChance = 0.4f;

    [Header("Contact")]
    public float slowDuration    = 1f;
    public float contactRadius   = 0.8f;
    public float contactCooldown = 2.5f;

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

    static readonly Color PatrolColor = new Color(0.15f, 0.80f, 0.25f); // green
    static readonly Color ChaseColor  = new Color(0.55f, 0.10f, 0.85f); // purple

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearDamping  = 4f;
        gameObject.layer  = 8;
        sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = PatrolColor;
        PickNewPatrolDir();
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        dirTimer = Random.Range(0.5f, dirChangeInterval);
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

        Vector2 moveVelocity;

        if (state == AIState.Chase)
        {
            Vector2 dir = ((Vector2)player.position - rb.position).normalized;
            moveVelocity = dir * baseChaseSpeed;
            if (sr) sr.color = ChaseColor; // purple when chasing
        }
        else
        {
            dirTimer -= Time.fixedDeltaTime;
            if (dirTimer <= 0f) PickNewPatrolDir();

            Vector2 pos = rb.position;
            if (pos.x < -boundX || pos.x > boundX) { patrolDir.x = -patrolDir.x; dirTimer = dirChangeInterval; }
            if (pos.y < -boundY || pos.y > boundY) { patrolDir.y = -patrolDir.y; dirTimer = dirChangeInterval; }

            moveVelocity = patrolDir * patrolSpeed;
            if (sr) sr.color = PatrolColor; // green while calm
        }

        rb.linearVelocity = moveVelocity;

        if (!onCooldown && dist < contactRadius)
            StartCoroutine(TriggerContact());
    }

    IEnumerator TriggerContact()
    {
        onCooldown = true;
        CartController cart = player?.GetComponent<CartController>();
        if (cart != null) cart.ApplySlow(slowDuration);
        DropObstacle drop = GetComponent<DropObstacle>();
        if (drop != null) drop.TriggerDrop();
        SfxPlayer.Play("cart-bump");
        UIManager.Instance?.ShowNotification("Watch where you're going!", 2f);
        yield return new WaitForSeconds(contactCooldown);
        onCooldown = false;
    }

    void PickNewPatrolDir()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        patrolDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        dirTimer  = dirChangeInterval + Random.Range(-0.5f, 1.2f);
    }
}
