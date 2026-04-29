using UnityEngine;

public class Register : MonoBehaviour
{
    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggered) return;
        if (GameManager.Instance == null || !GameManager.Instance.gameActive) return;

        int remaining = ShoppingList.Instance != null ? ShoppingList.Instance.RemainingCount : 1;

        if (remaining == 0)
        {
            triggered = true;
            GameManager.Instance.WinGame();
        }
        else
        {
            UIManager.Instance?.ShowNotification($"Still need {remaining} item{(remaining > 1 ? "s" : "")}!", 2f);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            triggered = false; // allow re-entry after collecting more items
    }
}
