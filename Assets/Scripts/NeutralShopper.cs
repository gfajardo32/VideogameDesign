using UnityEngine;
using System.Collections;

/// A shopper who minds their own business — until you get too close.
/// Green dot while calm, matching ShopperAI — player can't tell them apart.
/// Bumping them: chance to get annoyed (still green) → chasing (purple).
[RequireComponent(typeof(Rigidbody2D))]
public class NeutralShopper : MonoBehaviour
{
    [Header("Movement")]
    public float patrolSpeed       = 1.3f;
    public float annoyedChaseSpeed = 2f;
    public float dirChangeInterval = 2.2f;

    [Header("Provocation")]
    public float provokeRadius   = 0.9f;
    [Range(0f, 1f)]
    public float provokeChance   = 0.35f;
    public float annoyedDuration = 6f;

    [Header("Detection while annoyed")]
    public float annoyedDetectionRadius    = 6f;
    public float annoyedLoseInterestRadius = 9f;
    [Range(0f, 1f)]
    public float loseInterestChance = 0.5f;

    [Header("Contact penalty")]
    public float slowDuration    = 0.8f;
    public float contactCooldown = 3f;

    [Header("Bounds")]
    public float boundX = 15f;
    public float boundY = 12f;

    private enum AIState { Patrol, Annoyed, Chasing }
    private AIState state = AIState.Patrol;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Transform      player;
    private Vector2        patrolDir;
    private float          dirTimer          = 0f;
    private bool           onCooldown        = false;
    private float          annoyedTimer      = 0f;
    private float          provokeCheckTimer = 0f;
    private float          loseInterestTimer = 0f;

    static readonly Color CalmColor   = new Color(0.15f, 0.80f, 0.25f); // green — same as ShopperAI
    static readonly Color ChaseColor  = new Color(0.55f, 0.10f, 0.85f); // purple when chasing

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearDamping  = 4f;
        gameObject.layer  = 8;
        sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = CalmColor;
        PickNewPatrolDir();
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        dirTimer = Random.Range(0.3f, dirChangeInterval);
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameActive) return;
        if (player == null) return;

        float dist = Vector2.Distance(rb.position, (Vector2)player.position);

        switch (state)
        {
            case AIState.Patrol:  HandlePatrolState(dist);  break;
            case AIState.Annoyed: HandleAnnoyedState(dist); break;
            case AIState.Chasing: HandleChasingState(dist); break;
        }

        if (!onCooldown && state != AIState.Patrol && dist < 0.8f)
            StartCoroutine(ContactPenalty());
    }

    void HandlePatrolState(float dist)
    {
        dirTimer -= Time.fixedDeltaTime;
        if (dirTimer <= 0f) PickNewPatrolDir();

        Vector2 pos = rb.position;
        if (pos.x < -boundX || pos.x > boundX) { patrolDir.x = -patrolDir.x; dirTimer = dirChangeInterval; }
        if (pos.y < -boundY || pos.y > boundY) { patrolDir.y = -patrolDir.y; dirTimer = dirChangeInterval; }

        rb.linearVelocity = patrolDir * patrolSpeed;
        if (sr) sr.color = CalmColor; // green — indistinguishable from ShopperAI

        if (dist < provokeRadius)
        {
            provokeCheckTimer += Time.fixedDeltaTime;
            if (provokeCheckTimer >= 1f)
            {
                provokeCheckTimer = 0f;
                if (Random.value < provokeChance)
                {
                    state = AIState.Annoyed;
                    annoyedTimer = annoyedDuration;
                    UIManager.Instance?.ShowNotification("Hey! Watch it!", 2f);
                }
            }
        }
        else
        {
            provokeCheckTimer = 0f;
        }
    }

    void HandleAnnoyedState(float dist)
    {
        annoyedTimer -= Time.fixedDeltaTime;
        if (annoyedTimer <= 0f) { state = AIState.Patrol; return; }

        // Still green while annoyed but not yet chasing — ambiguous!
        if (sr) sr.color = CalmColor;

        dirTimer -= Time.fixedDeltaTime;
        if (dirTimer <= 0f) PickNewPatrolDir();
        rb.linearVelocity = patrolDir * (patrolSpeed * 0.6f);

        if (dist < annoyedDetectionRadius)
        {
            state = AIState.Chasing;
            loseInterestTimer = 0f;
        }
    }

    void HandleChasingState(float dist)
    {
        if (sr) sr.color = ChaseColor; // purple — now you know they're after you

        if (dist < annoyedLoseInterestRadius)
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
                { state = AIState.Patrol; return; }
            }
        }

        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = dir * annoyedChaseSpeed;
    }

    IEnumerator ContactPenalty()
    {
        onCooldown = true;
        CartController cart = player?.GetComponent<CartController>();
        if (cart != null) cart.ApplySlow(slowDuration);
        UIManager.Instance?.ShowNotification("Excuse me! Watch where you're going!", 2f);
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
