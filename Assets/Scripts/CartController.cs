using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CartController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed      = 5f;
    public float slowMultiplier = 0.4f;

    [Header("Footsteps")]
    public float footstepInterval = 0.35f;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;

    [Header("Sprites ??? assign via GroceryRush/Assign Sprites")]
    public Sprite spriteIdleDown;
    public Sprite spriteIdleUp;
    public Sprite spriteIdleRight;
    public Sprite spriteWalkRight;   // flipped for walking left
    public Sprite spriteWalkUp;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Vector2        moveInput;
    private bool           isSlowed    = false;
    private float          slowTimer   = 0f;
    private bool           isFrozen    = false;
    private float          freezeTimer = 0f;
    private float          footstepTimer = 0f;
    private Vector2        lastDir     = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale  = 0f;
        rb.constraints   = RigidbodyConstraints2D.FreezeRotation;
        rb.linearDamping = 8f;
        gameObject.layer = 6;
        sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = Color.white;

        // Ensure transform rotation is identity ??? no tilt ever
        transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameActive)
        {
            moveInput = Vector2.zero;
            UpdateSprite(Vector2.zero);
            return;
        }

        if (isFrozen)
        {
            freezeTimer -= Time.deltaTime;
            moveInput = Vector2.zero;
            if (freezeTimer <= 0f)
            {
                isFrozen = false;
                if (sr) sr.color = Color.white;
            }
            UpdateSprite(Vector2.zero);
            return;
        }

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f) isSlowed = false;
        }

        if (moveInput != Vector2.zero)
        {
            lastDir = moveInput;
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                SfxPlayer.Play("footstep", footstepVolume);
                footstepTimer = isSlowed ? footstepInterval / slowMultiplier : footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        UpdateSprite(moveInput);
    }

    void FixedUpdate()
    {
        if (isFrozen) { rb.linearVelocity = Vector2.zero; return; }
        float speed = isSlowed ? moveSpeed * slowMultiplier : moveSpeed;
        rb.linearVelocity = moveInput * speed;
        // No rotation ??? sprite direction is handled by UpdateSprite() + flipX
    }

    void UpdateSprite(Vector2 input)
    {
        if (sr == null) return;

        bool moving = input.sqrMagnitude > 0.01f;
        Vector2 dir = moving ? input : lastDir;

        if (!moving)
        {
            // Idle: pick directional idle frame
            if (dir.y > 0.3f && spriteIdleUp != null)
            { sr.sprite = spriteIdleUp; sr.flipX = false; }
            else if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y) && spriteIdleRight != null)
            { sr.sprite = spriteIdleRight; sr.flipX = dir.x < 0; }
            else if (spriteIdleDown != null)
            { sr.sprite = spriteIdleDown; sr.flipX = false; }
            return;
        }

        // Walking
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            // Horizontal ??? flip for left
            if (spriteWalkRight != null) { sr.sprite = spriteWalkRight; sr.flipX = dir.x < 0; }
        }
        else if (dir.y > 0)
        {
            // Moving up / away from camera
            if (spriteWalkUp != null) { sr.sprite = spriteWalkUp; sr.flipX = false; }
        }
        else
        {
            // Moving down / toward camera
            if (spriteIdleDown != null) { sr.sprite = spriteIdleDown; sr.flipX = false; }
        }
    }

    public void ApplySlow(float duration)
    {
        if (isFrozen) return;
        isSlowed  = true;
        slowTimer = duration;
    }

    public void ApplyFreeze(float duration)
    {
        isFrozen    = true;
        freezeTimer = duration;
        isSlowed    = false;
        if (sr) sr.color = new Color(0.4f, 0.7f, 1f);
    }
}
