using UnityEngine;

public class EndScreen : ScreenBase
{
    bool shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("EndScreenListener");
        go.AddComponent<EndScreen>();
    }

    void Start()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGameWin  += () => Show("you-won");
        GameManager.Instance.OnGameLose += () => Show("you-lost");
    }

    void Show(string spriteName)
    {
        if (shown) return;
        shown = true;

        EnsureEventSystem();
        canvasGo = CreateCanvas("EndScreenCanvas", 200);
        AddBackground(canvasGo.transform, spriteName);

        AddButton(canvasGo.transform, "RESTART",
            anchor:   new Vector2(0.5f, 0.07f),
            size:     new Vector2(360, 120),
            fontSize: 64,
            color:    new Color(0.85f, 0.18f, 0.18f),
            onClick:  () => GameManager.Instance?.RestartGame());
    }
}
