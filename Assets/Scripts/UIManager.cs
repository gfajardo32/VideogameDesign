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
    public GameObject winScreen;
    public GameObject loseScreen;
    public GameObject hudPanel;

    private Dictionary<string, TextMeshProUGUI> listEntries = new();
    private Queue<(string msg, float duration)> notifyQueue  = new();
    private Coroutine notifyCoroutine;
    private Coroutine timerPulseCoroutine;
    private bool      lowTimeWarningShown = false;
    private Vector3   timerBaseScale;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (timerText) timerBaseScale = timerText.transform.localScale;

        if (winScreen)        winScreen.SetActive(false);
        if (loseScreen)       loseScreen.SetActive(false);
        if (registerBanner)   registerBanner.SetActive(false);
        if (notificationText) notificationText.gameObject.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimerUpdate       += UpdateTimer;
            GameManager.Instance.OnGameWin           += ShowWin;
            GameManager.Instance.OnGameLose          += ShowLose;
            GameManager.Instance.OnAllItemsCollected += ShowRegisterBanner;
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
        timerText.text = $"{m:00}:{s:00}";

        // Colour progression: white -> yellow -> orange -> red
        if (t < 15f)
            timerText.color = Color.red;
        else if (t < 25f)
            timerText.color = new Color(1f, 0.4f, 0f);   // orange
        else if (t < 45f)
            timerText.color = new Color(1f, 0.85f, 0f);  // yellow
        else
            timerText.color = Color.white;

        // Start pulse when under 15s
        if (t < 15f && timerPulseCoroutine == null)
            timerPulseCoroutine = StartCoroutine(PulseTimer());

        // One-time 30s warning
        if (!lowTimeWarningShown && t <= 30f && t > 0f)
        {
            lowTimeWarningShown = true;
            QueueNotification("⚠️ 30 SECONDS LEFT! HURRY!", 3f);
        }
    }

    IEnumerator PulseTimer()
    {
        while (GameManager.Instance != null &&
               GameManager.Instance.GetTimeRemaining() < 15f &&
               GameManager.Instance.gameActive)
        {
            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                timerText.transform.localScale = Vector3.Lerp(timerBaseScale, timerBaseScale * 1.25f, t / 0.15f);
                yield return null;
            }
            t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                timerText.transform.localScale = Vector3.Lerp(timerBaseScale * 1.25f, timerBaseScale, t / 0.15f);
                yield return null;
            }
        }
        if (timerText) timerText.transform.localScale = timerBaseScale;
        timerPulseCoroutine = null;
    }

    void OnItemCollected(string itemName)
    {
        if (listEntries.TryGetValue(itemName, out var txt))
            txt.text = $"<s>[✓] {itemName}</s>";
        UpdateItemsRemaining();
        if (registerBanner) registerBanner.SetActive(false);
    }

    void OnItemDropped(string itemName)
    {
        if (listEntries.TryGetValue(itemName, out var txt))
            txt.text = $"<color=red>[ ] {itemName} ⚠ dropped!</color>";
        UpdateItemsRemaining();
        if (registerBanner) registerBanner.SetActive(false);
        StartCoroutine(FlashListEntry(itemName));
    }

    IEnumerator FlashListEntry(string itemName)
    {
        if (!listEntries.TryGetValue(itemName, out var txt)) yield break;
        for (int i = 0; i < 4; i++)
        {
            txt.transform.localScale = Vector3.one * 1.2f;
            yield return new WaitForSeconds(0.1f);
            txt.transform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.1f);
        }
    }

    void UpdateItemsRemaining()
    {
        if (itemsRemainingText == null || ShoppingList.Instance == null) return;
        itemsRemainingText.text = $"Items Left: {ShoppingList.Instance.RemainingCount}";
    }

    void ShowRegisterBanner()
    {
        if (registerBanner) registerBanner.SetActive(true);
        QueueNotification("🛒 ALL ITEMS! Head to the Register!", 4f);
    }

    // Public entry point — routes through queue so nothing gets lost
    public void ShowNotification(string msg, float duration) => QueueNotification(msg, duration);

    void QueueNotification(string msg, float duration)
    {
        notifyQueue.Enqueue((msg, duration));
        if (notifyCoroutine == null)
            notifyCoroutine = StartCoroutine(ProcessNotifyQueue());
    }

    IEnumerator ProcessNotifyQueue()
    {
        while (notifyQueue.Count > 0)
        {
            var (msg, duration) = notifyQueue.Dequeue();
            yield return StartCoroutine(NotifyRoutine(msg, duration));
        }
        notifyCoroutine = null;
    }

    IEnumerator NotifyRoutine(string msg, float duration)
    {
        if (notificationText == null) yield break;
        notificationText.text = msg;
        notificationText.gameObject.SetActive(true);
        notificationText.alpha = 0f;
        notificationText.transform.localScale = Vector3.one * 0.8f;

        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float p = t / 0.2f;
            notificationText.alpha = p;
            notificationText.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, p);
            yield return null;
        }
        notificationText.alpha = 1f;
        notificationText.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(Mathf.Max(0, duration - 0.4f));

        t = 0f;
        while (t < 0.2f) { t += Time.deltaTime; notificationText.alpha = 1f - t / 0.2f; yield return null; }
        notificationText.gameObject.SetActive(false);
    }

    void ShowWin()
    {
        StopTimerPulse();
        if (registerBanner) registerBanner.SetActive(false);
        if (hudPanel)  hudPanel.SetActive(false);
        if (winScreen) winScreen.SetActive(true);
    }

    void ShowLose()
    {
        StopTimerPulse();
        if (hudPanel)   hudPanel.SetActive(false);
        if (loseScreen) loseScreen.SetActive(true);
    }

    void StopTimerPulse()
    {
        if (timerPulseCoroutine != null) { StopCoroutine(timerPulseCoroutine); timerPulseCoroutine = null; }
        if (timerText) timerText.transform.localScale = timerBaseScale;
    }

    public void OnRestartButton() => GameManager.Instance?.RestartGame();
}
