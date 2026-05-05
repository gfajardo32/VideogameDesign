using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI itemsRemainingText;
    public Transform       shoppingListContainer;
    public GameObject      listItemPrefab;

    [Header("Notification")]
    public TextMeshProUGUI notificationText;

    [Header("Register Banner")]
    public GameObject registerBanner;

    [Header("Screens")]
    // Win/Lose are now handled by EndScreen (RuntimeInitialize bootstrap).
    // These fields are kept for backward compatibility but left unassigned.
    public GameObject winScreen;
    public GameObject loseScreen;
    public GameObject hudPanel;

    private Dictionary<string, TextMeshProUGUI> listEntries = new();
    private Coroutine notifyCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // HUD starts hidden ??? StartScreen shows it when PLAY is pressed
        if (hudPanel)        hudPanel.SetActive(false);
        if (winScreen)       winScreen.SetActive(false);
        if (loseScreen)      loseScreen.SetActive(false);
        if (registerBanner)  registerBanner.SetActive(false);
        if (notificationText) notificationText.gameObject.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimerUpdate       += UpdateTimer;
            GameManager.Instance.OnAllItemsCollected += ShowRegisterBanner;
            // Win/Lose handled by EndScreen ??? no subscription needed here
        }

        if (ShoppingList.Instance != null)
        {
            ShoppingList.Instance.OnItemCollected += OnItemCollected;
            ShoppingList.Instance.OnItemDropped   += OnItemDropped;
            BuildShoppingListUI();
        }
    }

    void BuildShoppingListUI()
    {
        if (shoppingListContainer == null || listItemPrefab == null) return;
        foreach (string item in ShoppingList.Instance.itemNames)
            AddListEntry(item);
        UpdateItemsRemaining();
    }

    void AddListEntry(string item)
    {
        if (shoppingListContainer == null || listItemPrefab == null) return;
        if (listEntries.ContainsKey(item)) return;
        var entry = Instantiate(listItemPrefab, shoppingListContainer);
        entry.SetActive(true);
        var txt = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) { txt.text = "[ ] " + item; listEntries[item] = txt; }
    }

    void UpdateTimer(float t)
    {
        if (timerText == null) return;
        int m = Mathf.FloorToInt(t / 60f), s = Mathf.FloorToInt(t % 60f);
        timerText.text  = $"{m:00}:{s:00}";
        timerText.color = t < 30f ? Color.red : Color.white;
    }

    void OnItemCollected(string itemName)
    {
        if (listEntries.TryGetValue(itemName, out var txt))
            txt.text = $"<s>[???] {itemName}</s>";
        UpdateItemsRemaining();
        if (registerBanner && ShoppingList.Instance.RemainingCount > 0)
            registerBanner.SetActive(false);
    }

    void OnItemDropped(string itemName)
    {
        if (listEntries.TryGetValue(itemName, out var txt))
            txt.text = $"<color=red>[ ] {itemName} ??? dropped!</color>";
        UpdateItemsRemaining();
        if (registerBanner) registerBanner.SetActive(false);
    }

    void UpdateItemsRemaining()
    {
        if (itemsRemainingText == null || ShoppingList.Instance == null) return;
        itemsRemainingText.text = $"Items Left: {ShoppingList.Instance.RemainingCount}";
    }

    void ShowRegisterBanner()
    {
        if (registerBanner) registerBanner.SetActive(true);
        ShowNotification("ALL ITEMS COLLECTED! Head to the Register! ????", 4f);
    }

    public void ShowNotification(string msg, float duration)
    {
        if (notificationText == null) return;
        if (notifyCoroutine != null) StopCoroutine(notifyCoroutine);
        notifyCoroutine = StartCoroutine(NotifyRoutine(msg, duration));
    }

    IEnumerator NotifyRoutine(string msg, float duration)
    {
        notificationText.text = msg;
        notificationText.gameObject.SetActive(true);
        notificationText.alpha = 0f;
        float t = 0f;
        while (t < 0.25f) { t += Time.deltaTime; notificationText.alpha = t / 0.25f; yield return null; }
        notificationText.alpha = 1f;
        yield return new WaitForSeconds(Mathf.Max(0, duration - 0.5f));
        t = 0f;
        while (t < 0.25f) { t += Time.deltaTime; notificationText.alpha = 1f - t / 0.25f; yield return null; }
        notificationText.gameObject.SetActive(false);
    }

    // Legacy ??? kept so old scene references don't break, but EndScreen handles this now
    public void OnRestartButton() => GameManager.Instance?.RestartGame();
}
