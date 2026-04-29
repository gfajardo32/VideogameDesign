using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class ShopperAI : MonoBehaviour
{
    [Header("Patrol")]
    public List<Transform> waypoints = new List<Transform>();
    public float moveSpeed = 2f;
    public float waypointReachDistance = 0.2f;

    [Header("Pause at waypoints")]
    public float pauseMin = 0.5f;
    public float pauseMax = 2f;

    private Rigidbody2D rb;
    private int currentWaypoint = 0;
    private bool pausing = false;
    private float pauseTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 8f;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameActive) return;
        if (waypoints == null || waypoints.Count == 0) return;

        if (pausing)
        {
            rb.linearVelocity = Vector2.zero;
            pauseTimer -= Time.fixedDeltaTime;
            if (pauseTimer <= 0f) pausing = false;
            return;
        }

        Transform target = waypoints[currentWaypoint];
        Vector2 dir = ((Vector2)target.position - rb.position).normalized;
        rb.linearVelocity = dir * moveSpeed;

        if (Vector2.Distance(rb.position, target.position) < waypointReachDistance)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Count;
            pausing = true;
            pauseTimer = Random.Range(pauseMin, pauseMax);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            // Nudge the player slightly on collision for chaos
            Rigidbody2D playerRb = col.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 pushDir = (col.transform.position - transform.position).normalized;
                playerRb.AddForce(pushDir * 3f, ForceMode2D.Impulse);
            }
        }
    }
}
