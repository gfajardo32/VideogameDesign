using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class ShopperAI : MonoBehaviour
{
    [Header("Movement")]
    public float patrolSpeed       = 1.4f;
    public float baseChaseSpeed    = 2.2f;
    public float fleeSpeed         = 2.5f;
    public float fleeDuration      = 5f;
    public float dirChangeInterval = 2f;

    [Header("Detection")]
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

    [Header("Sprites ??? assign via GroceryRush/Assign Sprites")]
    public Sprite spritePatrolRight;
    public Sprite spritePatrolDown;
    public Sprite spritePatrolUp;
    public Sprite spriteChaseRight;
    public Sprite spriteChaseDown;

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
        dirTimer = Random.Range(0.5f, dirChangeInterval);
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameActive) return;
        if (player == null) return;

        float dist = Vector2.Distance(rb.position, (Vector2)player.position);
        Vector2 moveVelocity;

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
                // Gradually steer away from player to handle map edges
                Vector2 awayFromPlayer = (rb.position - (Vector2)player.position).normalized;
                fleeDir = Vector2.Lerp(fleeDir, awayFromPlayer, 3f * Time.fixedDeltaTime).normalized;
                rb.linearVelocity = fleeDir * fleeSpeed;
                UpdateSprite(rb.linearVelocity, false);
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

        if (state == AIState.Chase)
        {
            Vector2 dir = ((Vector2)player.position - rb.position).normalized;
            moveVelocity = dir * baseChaseSpeed;
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
        UpdateSprite(moveVelocity, state == AIState.Chase);

        if (!onCooldown && dist < contactRadius)
            StartCoroutine(TriggerContact());
    }

    void UpdateSprite(Vector2 vel, bool isMad)
    {
        if (sr == null) return;
        bool moving = vel.sqrMagnitude > 0.05f;

        if (!moving)
        {
            Sprite idle = isMad ? spriteChaseDown : spritePatrolDown;
            if (idle) { sr.sprite = idle; sr.flipX = false; }
            return;
        }

        if (Mathf.Abs(vel.x) >= Mathf.Abs(vel.y))
        {
            Sprite s = isMad ? spriteChaseRight : spritePatrolRight;
            if (s) { sr.sprite = s; sr.flipX = vel.x < 0; }
        }
        else if (vel.y > 0)
        {
            if (spritePatrolUp) { sr.sprite = spritePatrolUp; sr.flipX = false; }
        }
        else
        {
            Sprite s = isMad ? spriteChaseDown : spritePatrolDown;
            if (s) { sr.sprite = s; sr.flipX = false; }
        }
    }

    IEnumerator TriggerContact()
    {
        onCooldown = true;

        // Apply penalty to player
        CartController cart = player?.GetComponent<CartController>();
        if (cart != null) cart.ApplySlow(slowDuration);
        DropObstacle drop = GetComponent<DropObstacle>();
        if (drop != null) drop.TriggerDrop();
        SfxPlayer.Play("cart-bump");
        UIManager.Instance?.ShowNotification("Watch where you're going!", 2f);

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
