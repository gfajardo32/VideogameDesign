using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemPickup : MonoBehaviour
{
    public string itemName = "Item";

    // Static registry so DropObstacle can find any item by name
    public static Dictionary<string, ItemPickup> Registry = new Dictionary<string, ItemPickup>();

    private bool      collected       = false;
    private Vector3   originalPosition;
    private SpriteRenderer sr;

    void Awake()
    {
        originalPosition = transform.position;
        sr = GetComponent<SpriteRenderer>();
        Registry[itemName] = this;
    }

    void OnDestroy()
    {
        if (Registry.TryGetValue(itemName, out var existing) && existing == this)
            Registry.Remove(itemName);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;
        Collect();
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;
        ShoppingList.Instance?.CollectItem(itemName);
        gameObject.SetActive(false);
    }

    /// Called by DropObstacle ??? re-enables item at its original spawn position
    public void Respawn()
    {
        collected = false;
        transform.position = originalPosition;
        gameObject.SetActive(true);
        StartCoroutine(SpawnFlash());
    }

    IEnumerator SpawnFlash()
    {
        if (sr == null) yield break;
        Color orig = sr.color;
        for (int i = 0; i < 5; i++)
        {
            sr.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            sr.color = orig;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
