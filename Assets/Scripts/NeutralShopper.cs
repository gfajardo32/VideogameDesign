using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class NeutralShopper : MonoBehaviour
{
    [Header("Movement")]
    public float patrolSpeed       = 1.3f;
    public float annoyedChaseSpeed = 2f;
    public float fleeSpeed         = 2.2f;
    public float fleeDuration      = 5f;
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

    [Header("Sprites ??? assign via GroceryRush/Assign Sprites")]
    public Sprite spriteCalmRight;
    public Sprite spriteCalmDown;
    public Sprite spriteCalmUp;
    public Sprite spriteMadRight;
    public Sprite spriteMadDown;

    private enum AIState { Patrol, Annoyed, Chasing, Flee }
    private AIState state = AIState.Patrol;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Transform      player;
    private Vector2        patrolDir;
    private Vector2        fleeDir;
    private float          fleeTimer         = 0f;
    private float          dirTimer          = 0f;
    private bool           onCooldown        = false;
    private float          annoyedTimer      = 0f;
    private float          provokeCheckTimer = 0f;
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
        dirTimer = Random.Range(0.3f, dirChangeInterval);
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
                UpdateSprite(rb.linearVelocity, false);
                return;
            }
        }

        Vector2 vel  = Vector2.zero;
        bool    isMad = false;

        switch (state)
        {
            case AIState.Patrol:  vel = HandlePatrolState(dist);  isMad = false; break;
            case AIState.Annoyed: vel = HandleAnnoyedState(dist); isMad = false; break;
            case AIState.Chasing: vel = HandleChasingState(dist); isMad = true;  break;
        }

        rb.linearVelocity = vel;
        UpdateSprite(vel, isMad);

        if (!onCooldown && state != AIState.Patrol && dist < 0.8f)
            StartCoroutine(ContactPenalty());
    }

    Vector2 HandlePatrolState(float dist)
    {
        dirTimer -= Time.fixedDeltaTime;
        if (dirTimer <= 0f) PickNewPatrolDir();

        Vector2 pos = rb.position;
        if (pos.x < -boundX || pos.x > boundX) { patrolDir.x = -patrolDir.x; dirTimer = dirChangeInterval; }
        if (pos.y < -boundY || pos.y > boundY) { patrolDir.y = -patrolDir.y; dirTimer = dirChangeInterval; }

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
        else provokeCheckTimer = 0f;

        return patrolDir * patrolSpeed;
    }

    Vector2 HandleAnnoyedState(float dist)
    {
        annoyedTimer -= Time.fixedDeltaTime;
        if (annoyedTimer <= 0f) { state = AIState.Patrol; return Vector2.zero; }

        dirTimer -= Time.fixedDeltaTime;
        if (dirTimer <= 0f) PickNewPatrolDir();

        if (dist < annoyedDetectionRadius)
        {
            state = AIState.Chasing;
            loseInterestTimer = 0f;
        }

        return patrolDir * (patrolSpeed * 0.6f);
    }

    Vector2 HandleChasingState(float dist)
    {
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
                if (Random.value < loseInterestChance) { state = AIState.Patrol; return Vector2.zero; }
            }
        }

        return ((Vector2)player.position - rb.position).normalized * annoyedChaseSpeed;
    }

    void UpdateSprite(Vector2 vel, bool isMad)
    {
        if (sr == null) return;
        bool moving = vel.sqrMagnitude > 0.05f;

        if (!moving)
        {
            Sprite idle = isMad ? spriteMadDown : spriteCalmDown;
            if (idle) { sr.sprite = idle; sr.flipX = false; }
            return;
        }

        if (Mathf.Abs(vel.x) >= Mathf.Abs(vel.y))
        {
            Sprite s = isMad ? spriteMadRight : spriteCalmRight;
            if (s) { sr.sprite = s; sr.flipX = vel.x < 0; }
        }
        else if (vel.y > 0)
        {
            if (spriteCalmUp) { sr.sprite = spriteCalmUp; sr.flipX = false; }
        }
        else
        {
            Sprite s = isMad ? spriteMadDown : spriteCalmDown;
            if (s) { sr.sprite = s; sr.flipX = false; }
        }
    }

    IEnumerator ContactPenalty()
    {
        onCooldown = true;

        CartController cart = player?.GetComponent<CartController>();
        if (cart != null) cart.ApplySlow(slowDuration);
        UIManager.Instance?.ShowNotification("Excuse me! Watch where you're going!", 2f);

        // Walk away for fleeDuration seconds
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
        dirTimer  = dirChangeInterval + Random.Range(-0.5f, 1.2f);
    }
}
