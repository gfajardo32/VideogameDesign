using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class ShopperAI : MonoBehaviour
{
    [Header("Patrol")]
    public List<Transform> waypoints = new List<Transform>();
    public float moveSpeed = 2.5f;
    public float waypointReachDistance = 0.2f;

    [Header("Pause at waypoints")]
    public float pauseMin = 0.5f;
    public float pauseMax = 1.5f;

    [Header("Contact")]
    public float contactRadius   = 0.7f;
    public float contactCooldown = 2f;

    private Rigidbody2D rb;
    private Transform   player;
    private int         currentWaypoint = 0;
    private bool        pausing         = false;
    private float       pauseTimer      = 0f;
    private bool        onCooldown      = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        rb.linearDamping  = 8f;
        gameObject.layer  = 8; // NPC layer
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameActive) return;

        // Waypoint patrol
        if (waypoints != null && waypoints.Count > 0)
        {
            if (pausing)
            {
                rb.linearVelocity = Vector2.zero;
                pauseTimer -= Time.fixedDeltaTime;
                if (pauseTimer <= 0f) pausing = false;
            }
            else
            {
                Transform target = waypoints[currentWaypoint];
                Vector2 dir = ((Vector2)target.position - rb.position).normalized;
                rb.linearVelocity = dir * moveSpeed;

                if (Vector2.Distance(rb.position, target.position) < waypointReachDistance)
                {
                    currentWaypoint = (currentWaypoint + 1) % waypoints.Count;
                    pausing    = true;
                    pauseTimer = Random.Range(pauseMin, pauseMax);
                }
            }
        }

        // Proximity-based contact
        if (!onCooldown && player != null)
        {
            float dist = Vector2.Distance(rb.position, player.position);
            if (dist < contactRadius)
                StartCoroutine(TriggerContact());
        }
    }

    IEnumerator TriggerContact()
    {
        onCooldown = true;

        CartController cart = player?.GetComponent<CartController>();
        if (cart != null) cart.ApplySlow(1f);

        if (ShoppingList.Instance != null)
        {
            List<string> carried = ShoppingList.Instance.GetCollectedItems();
            if (carried.Count > 0)
            {
                string item = carried[Random.Range(0, carried.Count)];
                ShoppingList.Instance.DropItem(item);
                if (ItemPickup.Registry.TryGetValue(item, out ItemPickup pickup))
                    pickup.Respawn();
                UIManager.Instance?.ShowNotification($"⚠️ Shopper bumped you! Dropped {item}!", 3f);
            }
        }

        yield return new WaitForSeconds(contactCooldown);
        onCooldown = false;
    }
}
