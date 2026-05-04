using UnityEngine;

public class Register : MonoBehaviour
{
    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggered) return;
        if (GameManager.Instance == null || !GameManager.Instance.gameActive) return;

        ShoppingList list = ShoppingList.Instance;
        if (list == null) return;

        if (list.InCartCount > 0)
        {
            int justBanked = list.InCartCount;
            list.BankItems();
            triggered = true;

            if (list.IsComplete)
            {
                GameManager.Instance.WinGame();
            }
            else
            {
                UIManager.Instance?.ShowNotification(
                    $"Dropped off {justBanked} item{(justBanked > 1 ? "s" : "")}! Still need {list.RemainingCount} more.", 3f);
            }
        }
        else if (list.IsComplete)
        {
            triggered = true;
            GameManager.Instance.WinGame();
        }
        else
        {
            UIManager.Instance?.ShowNotification(
                $"Cart is empty! Grab {list.RemainingCount} more item{(list.RemainingCount > 1 ? "s" : "")}.", 2f);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            triggered = false;
    }
}
