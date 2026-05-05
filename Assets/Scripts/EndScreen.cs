using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        GameManager.Instance.OnGameWin     += () => ShowFinalWin();
        GameManager.Instance.OnGameLose    += () => ShowLose();
        GameManager.Instance.OnLevelComplete += levelNum => ShowLevelComplete(levelNum);
    }

    // ------------------------------------------------------------------ screens

    void ShowLevelComplete(int levelBeaten)
    {
        if (shown) return;
        shown = true;

        EnsureEventSystem();
        canvasGo = CreateCanvas("EndScreenCanvas", 200);

        // Dark semi-transparent backdrop
        var bg = new GameObject("BG");
        bg.transform.SetParent(canvasGo.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.75f);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        // "LEVEL COMPLETE!" label
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(canvasGo.transform, false);
        var titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = $"LEVEL {levelBeaten}\nCOMPLETE!";
        titleTxt.fontSize  = 90;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color     = new Color(1f, 0.85f, 0.1f);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.55f);
        titleRect.anchorMax = new Vector2(1f, 0.85f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        // "NEXT LEVEL" button
        AddButton(canvasGo.transform, $"LEVEL {levelBeaten + 1} ???",
            anchor:   new Vector2(0.5f, 0.38f),
            size:     new Vector2(440, 130),
            fontSize: 62,
            color:    new Color(0.1f, 0.7f, 0.2f),
            onClick:  () => GameManager.Instance?.LoadNextLevel());

        // Restart button (smaller, below)
        AddButton(canvasGo.transform, "RESTART",
            anchor:   new Vector2(0.5f, 0.18f),
            size:     new Vector2(300, 90),
            fontSize: 46,
            color:    new Color(0.55f, 0.55f, 0.55f),
            onClick:  () => GameManager.Instance?.RestartGame());
    }

    void ShowFinalWin()
    {
        if (shown) return;
        shown = true;

        EnsureEventSystem();
        canvasGo = CreateCanvas("EndScreenCanvas", 200);

        // Gold backdrop
        var bg = new GameObject("BG");
        bg.transform.SetParent(canvasGo.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.75f);
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        // "YOU WIN!" label
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(canvasGo.transform, false);
        var titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "YOU WIN!";
        titleTxt.fontSize  = 110;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color     = new Color(1f, 0.85f, 0.1f);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.55f);
        titleRect.anchorMax = new Vector2(1f, 0.85f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        AddButton(canvasGo.transform, "PLAY AGAIN",
            anchor:   new Vector2(0.5f, 0.3f),
            size:     new Vector2(400, 130),
            fontSize: 64,
            color:    new Color(0.85f, 0.18f, 0.18f),
            onClick:  () => GameManager.Instance?.RestartGame());
    }

    void ShowLose()
    {
        if (shown) return;
        shown = true;

        EnsureEventSystem();
        canvasGo = CreateCanvas("EndScreenCanvas", 200);
        AddBackground(canvasGo.transform, "you-lost");

        AddButton(canvasGo.transform, "RESTART",
            anchor:   new Vector2(0.5f, 0.07f),
            size:     new Vector2(360, 120),
            fontSize: 64,
            color:    new Color(0.85f, 0.18f, 0.18f),
            onClick:  () => GameManager.Instance?.RestartGame());
    }
}
