using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// When the player collides with this obstacle, 1-2 of their collected
/// items are knocked loose and respawn at their original positions.
public class DropObstacle : MonoBehaviour
{
    [Header("Settings")]
    public float cooldown     = 3f;
    public int   maxDropCount = 2;

    private bool         onCooldown = false;
    private SpriteRenderer sr;

    void Start() => sr = GetComponent<SpriteRenderer>();

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.gameObject.CompareTag("Player")) return;
        if (onCooldown) return;
        if (GameManager.Instance == null || !GameManager.Instance.gameActive) return;
        if (ShoppingList.Instance == null) return;

        List<string> carried = ShoppingList.Instance.GetCollectedItems();
        if (carried.Count == 0) return;

        // Shuffle
        for (int i = carried.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (carried[i], carried[j]) = (carried[j], carried[i]);
        }

        int toDrop = Mathf.Min(maxDropCount, carried.Count);
        for (int i = 0; i < toDrop; i++)
        {
            string item = carried[i];
            ShoppingList.Instance.DropItem(item);

            if (ItemPickup.Registry.TryGetValue(item, out ItemPickup pickup))
                pickup.Respawn();

            UIManager.Instance?.ShowNotification($"???? Dropped {item}!\nGo pick it up!", 3f);
        }

        StartCoroutine(CooldownRoutine());
        StartCoroutine(FlashRoutine());
    }

    IEnumerator CooldownRoutine()
    {
        onCooldown = true;
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }

    IEnumerator FlashRoutine()
    {
        if (sr == null) yield break;
        Color orig = sr.color;
        for (int i = 0; i < 3; i++)
        {
            sr.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            sr.color = orig;
            yield return new WaitForSeconds(0.08f);
        }
    }
}
