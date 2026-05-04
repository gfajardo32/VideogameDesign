using UnityEngine;

public class HowToPlayScreen : ScreenBase
{
    void Start()
    {
        EnsureEventSystem();
        canvasGo = CreateCanvas("HowToPlayCanvas", 110);
        AddBackground(canvasGo.transform, "how-to-play");

        AddButton(canvasGo.transform, "BACK",
            anchor:   new Vector2(0.5f, 0.06f),
            size:     new Vector2(280, 100),
            fontSize: 56,
            color:    new Color(0.3f, 0.3f, 0.3f),
            onClick:  OnBackClicked);
    }

    void OnBackClicked()
    {
        Destroy(canvasGo);
        Destroy(gameObject);
    }
}
