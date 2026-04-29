using UnityEngine;

public class HazardZone : MonoBehaviour
{
    [Header("Wet Floor Settings")]
    public float slowDuration = 2f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        CartController cart = other.GetComponent<CartController>();
        if (cart != null)
            cart.ApplySlow(slowDuration);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        CartController cart = other.GetComponent<CartController>();
        if (cart != null)
            cart.ApplySlow(slowDuration);
    }
}
