using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ---- Level progression (static: survives scene reload) ----
    public static int CurrentLevel { get; private set; } = 1;
    public const  int MaxLevel = 2;

    [Header("Game Settings ??? Level 1")]
    public float timeLimit = 120f;

    [Header("State")]
    public bool gameActive = false;

    private float timeRemaining;
    private bool  warned30s = false;

    // Static flags survive scene reload
    private static bool s_isRestart         = false;
    private static bool s_isLevelTransition = false;

    public event System.Action         OnGameStart;
    public event System.Action         OnGameWin;
    public event System.Action         OnGameLose;
    public event System.Action<float>  OnTimerUpdate;
    public event System.Action         OnAllItemsCollected;
    public event System.Action<int>    OnLevelComplete;   // fired with the level that was just beaten

    // ---- Per-level config ----
    static readonly float[] LevelTimeLimits = { 0f, 120f, 90f }; // index 0 unused

    static readonly List<string>[] LevelShoppingLists =
    {
        null, // index 0 unused
        new List<string> { "Milk", "Bread", "Eggs", "Cheese", "Apples",
                           "Juice", "Butter", "Yogurt", "Cereal", "Bananas" },
        new List<string> { "Milk", "Bread", "Eggs", "Cheese", "Apples",
                           "Orange Juice", "Butter", "Yogurt", "Cereal", "Bananas",
                           "Skim Milk", "Rye Bread" },
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (ShoppingList.Instance != null)
            ShoppingList.Instance.OnListComplete += HandleListComplete;

        if (s_isRestart)
        {
            s_isRestart  = false;
            CurrentLevel = 1;
            BootstrapEndScreen();
            ShowHUD();
            ApplyLevelSettings();
            StartGame();
            return;
        }

        if (s_isLevelTransition)
        {
            s_isLevelTransition = false;
            BootstrapEndScreen();
            ShowHUD();
            ApplyLevelSettings();
            StartGame();
            return;
        }
    }

    // Apply time limit and shopping list for CurrentLevel
    void ApplyLevelSettings()
    {
        if (CurrentLevel >= 1 && CurrentLevel <= MaxLevel)
            timeLimit = LevelTimeLimits[CurrentLevel];

        // Tell ShoppingList to use the correct item set for this level
        if (ShoppingList.Instance != null && CurrentLevel >= 1 && CurrentLevel <= MaxLevel)
            ShoppingList.Instance.SetItems(LevelShoppingLists[CurrentLevel]);
    }

    void BootstrapEndScreen()
    {
        new GameObject("EndScreenListener").AddComponent<EndScreen>();
    }

    void ShowHUD()
    {
        if (UIManager.Instance != null && UIManager.Instance.hudPanel != null)
            UIManager.Instance.hudPanel.SetActive(true);
    }

    public void StartGame()
    {
        timeRemaining = timeLimit;
        gameActive    = true;
        warned30s     = false;
        OnGameStart?.Invoke();
    }

    void HandleListComplete() => OnAllItemsCollected?.Invoke();

    void Update()
    {
        if (!gameActive) return;

        timeRemaining -= Time.deltaTime;
        OnTimerUpdate?.Invoke(timeRemaining);

        if (!warned30s && timeRemaining <= 30f)
        {
            warned30s = true;
            SfxPlayer.Play("30s-warning");
            UIManager.Instance?.ShowNotification("??? 30 seconds left!", 2.5f);
        }

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            LoseGame();
        }
    }

    public void WinGame()
    {
        if (!gameActive) return;
        gameActive = false;
        SfxPlayer.Play("register-checkout");

        if (CurrentLevel < MaxLevel)
        {
            // Level beaten ??? fire event so EndScreen can show "Level Complete"
            OnLevelComplete?.Invoke(CurrentLevel);
        }
        else
        {
            // All levels beaten ??? real win
            OnGameWin?.Invoke();
        }
    }

    public void LoseGame()
    {
        if (!gameActive) return;
        gameActive = false;
        OnGameLose?.Invoke();
    }

    public void LoadNextLevel()
    {
        CurrentLevel++;
        s_isLevelTransition = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RestartGame()
    {
        s_isRestart = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public float GetTimeRemaining() => timeRemaining;
}
