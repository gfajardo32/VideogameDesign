using UnityEngine;
using System.Collections;

/// Every spawnInterval seconds, creates a new temporary wet floor zone
/// at a random location in the store to keep the player on their toes.
public class SpillManager : MonoBehaviour
{
    [Header("Settings")]
    public float spawnInterval = 25f;
    public float spillLifetime = 35f;
    public int   maxActiveSpills = 5;

    [Header("Map bounds")]
    public float minX = -13f;
    public float maxX =  13f;
    public float minY = -11f;
    public float maxY =  11f;

    private int   activeSpills = 0;
    private float timer        = 10f; // first spill after 10s

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.gameActive) return;

        timer -= Time.deltaTime;
        if (timer <= 0f && activeSpills < maxActiveSpills)
        {
            StartCoroutine(SpawnSpill());
            timer = spawnInterval;
        }
    }

    IEnumerator SpawnSpill()
    {
        activeSpills++;

        // Create a simple spill zone
        var go = new GameObject("DynamicSpill");
        go.layer = 0;

        // Visual
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.3f, 0.6f, 1f, 0.65f);
        go.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

        // Use a circle sprite (Unity default)
        sr.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

        // Collider
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        // Hazard behaviour
        go.AddComponent<HazardZone>();

        // Random position
        go.transform.position = new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            0f);

        int aisle = Random.Range(1, 9);
        UIManager.Instance?.ShowNotification($"Spill on aisle {aisle}! Watch your step!", 2.5f);

        yield return new WaitForSeconds(spillLifetime);

        if (go != null) Destroy(go);
        activeSpills--;
    }
}
