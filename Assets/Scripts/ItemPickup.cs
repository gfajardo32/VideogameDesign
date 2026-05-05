using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemPickup : MonoBehaviour
{
    public string itemName = "Item";

    public static Dictionary<string, ItemPickup> Registry = new Dictionary<string, ItemPickup>();

    private bool           collected;
    private Vector3        originalPosition;
    private SpriteRenderer sr;

    void Awake()
    {
        // Registry wired in Awake so other scripts can look items up immediately.
        sr                 = GetComponent<SpriteRenderer>();
        Registry[itemName] = this;
    }

    void Start()
    {
        // Capture AFTER ItemSpawnRandomizer.Awake() may have moved us.
        originalPosition = transform.position;
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
        SfxPlayer.Play("item-pickup");
        ShoppingList.Instance?.CollectItem(itemName);
        gameObject.SetActive(false);
    }

    public void Respawn()
    {
        collected          = false;
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
