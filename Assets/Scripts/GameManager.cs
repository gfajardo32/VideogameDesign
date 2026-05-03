using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float timeLimit = 120f;

    [Header("State")]
    public bool gameActive = false;

    private float timeRemaining;

    public event System.Action         OnGameStart;
    public event System.Action         OnGameWin;
    public event System.Action         OnGameLose;
    public event System.Action<float>  OnTimerUpdate;
    public event System.Action         OnAllItemsCollected;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Layer 6 = Player, Layer 8 = NPC
        // NPCs should not physically push the player — contact is handled via triggers
        Physics2D.IgnoreLayerCollision(6, 8, true);
    }

    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        timeRemaining = timeLimit;
        gameActive    = true;
        OnGameStart?.Invoke();

        if (ShoppingList.Instance != null)
            ShoppingList.Instance.OnListComplete += HandleListComplete;
    }

    void HandleListComplete()
    {
        OnAllItemsCollected?.Invoke();
    }

    void Update()
    {
        if (!gameActive) return;
        timeRemaining -= Time.deltaTime;
        OnTimerUpdate?.Invoke(timeRemaining);
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
        OnGameWin?.Invoke();
    }

    public void LoseGame()
    {
        if (!gameActive) return;
        gameActive = false;
        OnGameLose?.Invoke();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public float GetTimeRemaining() => timeRemaining;
}
