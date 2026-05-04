using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemPickup : MonoBehaviour
{
    public string itemName = "Item";
    public bool   isDecoy  = false;  // if true, picking this up wastes time

    public static Dictionary<string, ItemPickup> Registry = new Dictionary<string, ItemPickup>();

    private bool           collected = false;
    private Vector3        originalPosition;
    private SpriteRenderer sr;

    void Awake()
    {
        originalPosition = transform.position;
        sr = GetComponent<SpriteRenderer>();
        if (!isDecoy)
            Registry[itemName] = this;
    }

    void OnDestroy()
    {
        if (!isDecoy && Registry.TryGetValue(itemName, out var existing) && existing == this)
            Registry.Remove(itemName);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        if (isDecoy)
        {
            UIManager.Instance?.ShowNotification($"Wrong item! \"{itemName}\" is not on your list!", 2.5f);
            CartController cart = other.GetComponent<CartController>();
            if (cart != null) cart.ApplySlow(1.5f);
            StartCoroutine(DecoyFlash());
            return;
        }

        Collect();
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;
        ShoppingList.Instance?.CollectItem(itemName);
        gameObject.SetActive(false);
    }

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

    IEnumerator DecoyFlash()
    {
        if (sr == null) yield break;
        Color orig = sr.color;
        for (int i = 0; i < 4; i++)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            sr.color = orig;
            yield return new WaitForSeconds(0.08f);
        }
    }
}
