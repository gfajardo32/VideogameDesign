using UnityEngine;

public class StartScreen : ScreenBase
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("StartScreen");
        go.AddComponent<StartScreen>();
    }

    void Start()
    {
        EnsureEventSystem();
        canvasGo = CreateCanvas("StartScreenCanvas", 100);
        AddBackground(canvasGo.transform, "title-screen");

        AddButton(canvasGo.transform, "PLAY",
            anchor:   new Vector2(0.5f, 0.35f),
            size:     new Vector2(400, 140),
            fontSize: 90,
            color:    new Color(0.85f, 0.18f, 0.18f),
            onClick:  OnPlayClicked);

        AddButton(canvasGo.transform, "HOW TO PLAY",
            anchor:   new Vector2(0.5f, 0.17f),
            size:     new Vector2(400, 100),
            fontSize: 48,
            color:    new Color(0.2f, 0.45f, 0.8f),
            onClick:  OnHowToPlayClicked);

        if (UIManager.Instance != null && UIManager.Instance.hudPanel != null)
            UIManager.Instance.hudPanel.SetActive(false);
    }

    void OnPlayClicked()
    {
        if (UIManager.Instance != null && UIManager.Instance.hudPanel != null)
            UIManager.Instance.hudPanel.SetActive(true);
        if (GameManager.Instance != null && !GameManager.Instance.gameActive)
            GameManager.Instance.StartGame();
        Destroy(canvasGo);
        Destroy(gameObject);
    }

    void OnHowToPlayClicked()
    {
        var go = new GameObject("HowToPlayScreen");
        go.AddComponent<HowToPlayScreen>();
    }
}
