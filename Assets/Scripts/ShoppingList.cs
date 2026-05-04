using UnityEngine;
using System.Collections.Generic;

public class ShoppingList : MonoBehaviour
{
    public static ShoppingList Instance { get; private set; }

    [Header("Items")]
    public List<string> itemNames = new List<string>
    {
        "Milk", "Bread", "Eggs", "Cheese", "Apples",
        "Juice", "Butter", "Yogurt", "Cereal", "Bananas"
    };

    [Header("Cart")]
    public int cartCapacity = 4;

    private HashSet<string> inCart  = new HashSet<string>(); // currently carrying
    private HashSet<string> banked  = new HashSet<string>(); // safely deposited at register

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
        if (inCart.Contains(itemName) || banked.Contains(itemName)) return;
        if (!itemNames.Contains(itemName)) return;

        if (inCart.Count >= cartCapacity)
        {
            UIManager.Instance?.ShowNotification($"Cart is full! ({cartCapacity}/{cartCapacity}) Drop off at the register first!", 2.5f);
            return;
        }

        inCart.Add(itemName);
        OnItemCollected?.Invoke(itemName);
        Debug.Log($"[ShoppingList] In cart: {itemName} ({inCart.Count}/{cartCapacity})");
    }

    /// Called by Register — moves everything in cart to banked
    public void BankItems()
    {
        foreach (var item in inCart)
            banked.Add(item);
        inCart.Clear();
        Debug.Log($"[ShoppingList] Banked. Total banked: {banked.Count}/{itemNames.Count}");
        if (banked.Count >= itemNames.Count)
            OnListComplete?.Invoke();
    }

    public void DropItem(string itemName)
    {
        if (!inCart.Contains(itemName)) return;
        inCart.Remove(itemName);
        OnItemDropped?.Invoke(itemName);
        Debug.Log($"[ShoppingList] Dropped: {itemName}");
    }

    public List<string> GetCollectedItems() => new List<string>(inCart);
    public bool IsCollected(string itemName)  => inCart.Contains(itemName) || banked.Contains(itemName);
    public bool IsBanked(string itemName)     => banked.Contains(itemName);
    public int  CollectedCount  => inCart.Count + banked.Count;
    public int  InCartCount     => inCart.Count;
    public int  BankedCount     => banked.Count;
    public int  TotalCount      => itemNames.Count;
    public int  RemainingCount  => itemNames.Count - inCart.Count - banked.Count; // still on shelves
    public bool IsComplete      => banked.Count >= itemNames.Count;
}
