using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class KidAI : MonoBehaviour
{
    public enum ChaseStyle { Direct, Ahead, Flank, Shy }

    [Header("Movement")]
    public ChaseStyle chaseStyle         = ChaseStyle.Direct;
    public float patrolSpeed             = 1.5f;
    public float baseChaseSpeed          = 2.5f;
    public float chaseSpeedPerItem       = 0.4f;
    public float detectionRadius         = 6f;
    public float directionChangeInterval = 0.8f;

    [Header("Separation from other kids")]
    public float separationRadius = 2.5f;
    public float separationForce  = 3f;

    [Header("Contact")]
    public float contactRadius   = 0.7f;
    public float contactCooldown = 2f;

    [Header("Bounds")]
    public float boundX = 14f;
    public float boundY = 12f;

    private Rigidbody2D rb;
    private Transform   player;
    private Vector2     currentDirection;
    private float       dirTimer   = 0f;
    private bool        isChasing  = false;
    private bool        onCooldown = false;

    // Used by Flank style to pick a consistent side
    private float flankSide;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearDamping  = 3f;
        gameObject.layer  = 8;
        flankSide         = Random.value > 0.5f ? 1f : -1f;
        PickNewDirection();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameActive) return;
        if (player == null) return;

        float dist = Vector2.Distance(rb.position, player.position);
        isChasing = dist < detectionRadius;

        float chaseSpeed = baseChaseSpeed
            + (ShoppingList.Instance != null ? ShoppingList.Instance.CollectedCount * chaseSpeedPerItem : 0f);

        Vector2 moveVelocity;

        if (isChasing)
        {
            Vector2 target = GetChaseTarget(chaseSpeed);
            Vector2 dir = (target - rb.position).normalized;
            moveVelocity = dir * chaseSpeed;
        }
        else
        {
            dirTimer -= Time.fixedDeltaTime;
            if (dirTimer <= 0f) PickNewDirection();

            Vector2 pos = rb.position;
            if (Mathf.Abs(pos.x) > boundX || Mathf.Abs(pos.y) > boundY)
            {
                currentDirection = -currentDirection;
                dirTimer = directionChangeInterval;
            }

            moveVelocity = currentDirection * patrolSpeed;
        }

        // Separation: steer away from nearby KidAI instances so they spread out
        Vector2 separation = Vector2.zero;
        foreach (var other in FindObjectsByType<KidAI>(FindObjectsSortMode.None))
        {
            if (other == this) continue;
            float d = Vector2.Distance(rb.position, other.rb.position);
            if (d < separationRadius && d > 0.01f)
            {
                separation += (rb.position - other.rb.position).normalized * (separationRadius - d);
            }
        }
        moveVelocity += separation * separationForce * Time.fixedDeltaTime;

        rb.linearVelocity = moveVelocity;

        // Proximity contact check
        if (!onCooldown && dist < contactRadius)
            StartCoroutine(TriggerContact());
    }

    Vector2 GetChaseTarget(float chaseSpeed)
    {
        Vector2 playerPos = player.position;

        switch (chaseStyle)
        {
            case ChaseStyle.Ahead:
                // Target 3 units ahead of where the player is moving
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                Vector2 playerVel = playerRb != null ? playerRb.linearVelocity : Vector2.zero;
                return playerPos + playerVel.normalized * 3f;

            case ChaseStyle.Flank:
                // Target a point perpendicular to the player-to-me direction
                Vector2 toPlayer = (playerPos - rb.position).normalized;
                Vector2 perp     = new Vector2(-toPlayer.y, toPlayer.x) * flankSide;
                return playerPos + perp * 2.5f;

            case ChaseStyle.Shy:
                // Like Clyde: chase when far, wander away when close
                float d = Vector2.Distance(rb.position, playerPos);
                if (d > 4f)
                    return playerPos;
                else
                    return rb.position + (rb.position - playerPos).normalized * 3f; // run away
                    
            case ChaseStyle.Direct:
            default:
                return playerPos;
        }
    }

    IEnumerator TriggerContact()
    {
        onCooldown = true;
        PickNewDirection();

        CartController cart = player.GetComponent<CartController>();
        if (cart != null) cart.ApplySlow(1.5f);

        if (ShoppingList.Instance != null)
        {
            List<string> carried = ShoppingList.Instance.GetCollectedItems();
            if (carried.Count > 0)
            {
                string item = carried[Random.Range(0, carried.Count)];
                ShoppingList.Instance.DropItem(item);
                if (ItemPickup.Registry.TryGetValue(item, out ItemPickup pickup))
                    pickup.Respawn();
                UIManager.Instance?.ShowNotification($"⚠️ A kid bumped you! Dropped {item}!", 3f);
            }
        }

        yield return new WaitForSeconds(contactCooldown);
        onCooldown = false;
    }

    void PickNewDirection()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        currentDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        dirTimer = directionChangeInterval + Random.Range(-0.2f, 0.3f);
    }

    void OnCollisionEnter2D(Collision2D col) => PickNewDirection();
}
