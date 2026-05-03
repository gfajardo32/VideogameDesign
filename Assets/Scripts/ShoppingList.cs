using UnityEngine;
using System.Collections.Generic;

public class ShoppingList : MonoBehaviour
{
    public static ShoppingList Instance { get; private set; }

    [Header("Starting items")]
    public List<string> itemNames = new List<string>
    {
        "Milk", "Bread", "Eggs", "Cheese", "Apples",
        "Juice", "Butter", "Yogurt", "Cereal", "Bananas"
    };

    private HashSet<string> collectedItems = new HashSet<string>();

    public event System.Action<string> OnItemCollected;
    public event System.Action<string> OnItemDropped;
    public event System.Action         OnListComplete;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void CollectItem(string itemName)
    {
        if (collectedItems.Contains(itemName)) return;
        if (!itemNames.Contains(itemName)) return;

        collectedItems.Add(itemName);
        OnItemCollected?.Invoke(itemName);
        Debug.Log($"[ShoppingList] Collected: {itemName} ({collectedItems.Count}/{itemNames.Count})");

        if (collectedItems.Count >= itemNames.Count)
            OnListComplete?.Invoke();
    }

    public void DropItem(string itemName)
    {
        if (!collectedItems.Contains(itemName)) return;
        collectedItems.Remove(itemName);
        OnItemDropped?.Invoke(itemName);
        Debug.Log($"[ShoppingList] Dropped: {itemName} ({collectedItems.Count}/{itemNames.Count})");
    }

    public List<string> GetCollectedItems() => new List<string>(collectedItems);

    public bool IsCollected(string itemName) => collectedItems.Contains(itemName);
    public int CollectedCount => collectedItems.Count;
    public int TotalCount     => itemNames.Count;
    public int RemainingCount => itemNames.Count - collectedItems.Count;
}
