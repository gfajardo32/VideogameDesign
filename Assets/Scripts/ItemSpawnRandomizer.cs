using UnityEngine;
using System.Collections.Generic;

/// Runs in Awake() ??? before ItemPickup.Start() captures originalPosition ???
/// and shuffles every ItemPickup into a unique random slot drawn from a pool
/// of candidate aisle positions. Each run produces a different item layout.
///
/// CLEAR-SPACE CHECK
/// Before assigning any position the system validates it with two passes:
///   1. Physics2D.OverlapBox against the "Environment" LayerMask (walls, shelves, displays).
///   2. Tag-based sweep for "Shelves", "Display", and "Wet Floor" objects ??? acts as a
///      graceful fallback if some objects are not yet on the Environment layer.
/// Positions that fail either check are skipped; items with no remaining slot stay put.
public class ItemSpawnRandomizer : MonoBehaviour
{
    // Half-extents for the overlap box (items are ~0.5 ?? 0.5 units; 0.28 gives a small margin)
    static readonly Vector2 CheckHalfExtents = new Vector2(0.28f, 0.28f);

    // Tags treated as blocked zones; must match the tags defined in TagManager
    static readonly string[] BlockedTags = { "Shelves", "Display", "Wet Floor" };

    // Candidate spawn spots spread across four horizontal aisle rows.
    // Some edge positions (e.g. x = ??11) will be pruned automatically by the physics check.
    static readonly Vector2[] SpawnPool = new Vector2[]
    {
        // Top aisle row  (y ???  9)
        new Vector2(-11f,  9f), new Vector2(-7f,  9f), new Vector2(-3f,  9f),
        new Vector2(  1f,  9f), new Vector2( 5f,  9f), new Vector2( 9f,  9f),

        // Upper-mid row  (y ???  4)
        new Vector2(-11f,  4f), new Vector2(-7f,  4f), new Vector2(-3f,  4f),
        new Vector2(  1f,  4f), new Vector2( 5f,  4f), new Vector2( 9f,  4f),

        // Lower-mid row  (y ??? -3)
        new Vector2(-11f, -3f), new Vector2(-7f, -3f), new Vector2(-3f, -3f),
        new Vector2(  1f, -3f), new Vector2( 5f, -3f), new Vector2( 9f, -3f),

        // Bottom aisle row (y ??? -9)
        new Vector2(-11f, -9f), new Vector2(-7f, -9f), new Vector2(-3f, -9f),
        new Vector2(  1f, -9f), new Vector2( 5f, -9f), new Vector2( 9f, -9f),
    };

    void Awake()
    {
        // Resolve the "Environment" layer for the primary physics check
        int envLayerIndex = LayerMask.NameToLayer("Environment");
        LayerMask envMask = envLayerIndex >= 0 ? (1 << envLayerIndex) : 0;

        if (envLayerIndex < 0)
            Debug.LogWarning("[ItemSpawnRandomizer] 'Environment' layer not found. " +
                             "Tag-based fallback will still block Shelves / Display / Wet Floor.");

        // Collect every ItemPickup currently in the scene
        var items = new List<ItemPickup>(
            FindObjectsByType<ItemPickup>(FindObjectsSortMode.None));
        if (items.Count == 0) return;

        // Fisher-Yates shuffle of the candidate pool
        var pool = new List<Vector2>(SpawnPool);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        // Filter pool: keep only positions that pass the clear-space check
        var validSlots = new List<Vector2>();
        foreach (Vector2 candidate in pool)
        {
            if (IsSpawnClear(candidate, envMask))
                validSlots.Add(candidate);
        }

        if (validSlots.Count == 0)
        {
            Debug.LogWarning("[ItemSpawnRandomizer] No clear spawn slots found ??? " +
                             "items kept at their original positions.");
            return;
        }

        // Assign each item a unique validated slot
        int placed = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (i >= validSlots.Count)
            {
                Debug.LogWarning($"[ItemSpawnRandomizer] Ran out of valid slots ??? " +
                                 $"'{items[i].name}' stays in place.");
                break;
            }
            items[i].transform.position = new Vector3(validSlots[i].x, validSlots[i].y, 0f);
            placed++;
        }

        Debug.Log($"[ItemSpawnRandomizer] Placed {placed}/{items.Count} item(s) " +
                  $"across {validSlots.Count} valid slot(s) " +
                  $"(filtered from {SpawnPool.Length} candidates).");
    }

    /// <summary>
    /// Returns true only if <paramref name="point"/> is free of blocking colliders.
    /// </summary>
    bool IsSpawnClear(Vector2 point, LayerMask envMask)
    {
        Vector2 boxSize = CheckHalfExtents * 2f;

        // Pass 1 ??? Environment layer (walls, shelves, display fixtures)
        if (envMask.value != 0)
        {
            Collider2D hit = Physics2D.OverlapBox(point, boxSize, 0f, envMask);
            if (hit != null)
                return false;
        }

        // Pass 2 ??? tag sweep (covers objects not yet on the Environment layer)
        Collider2D[] nearby = Physics2D.OverlapBoxAll(point, boxSize, 0f);
        foreach (Collider2D col in nearby)
        {
            foreach (string blocked in BlockedTags)
            {
                if (col.CompareTag(blocked))
                    return false;
            }
        }

        return true;
    }
}
