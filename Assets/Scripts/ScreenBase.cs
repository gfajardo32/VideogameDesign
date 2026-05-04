using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class ScreenBase : MonoBehaviour
{
    protected GameObject canvasGo;

    protected static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    protected static GameObject CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    protected static Image AddBackground(Transform parent, string spriteName)
    {
        var go = new GameObject("Background");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = LoadSprite(spriteName);
        Stretch(img.rectTransform);
        return img;
    }

    protected static Button AddButton(Transform parent, string text, Vector2 anchor, Vector2 size, int fontSize, Color color, System.Action onClick)
    {
        var btnGo = new GameObject(text + "Button");
        btnGo.transform.SetParent(parent, false);
        var img = btnGo.AddComponent<Image>();
        img.color = color;
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.4f);
        colors.pressedColor     = Color.Lerp(color, Color.black, 0.3f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick?.Invoke());
        var rt = img.rectTransform;
        rt.anchorMin       = anchor;
        rt.anchorMax       = anchor;
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta       = size;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var lbl = labelGo.AddComponent<TextMeshProUGUI>();
        lbl.text      = text;
        lbl.fontSize  = fontSize;
        lbl.fontStyle = FontStyles.Bold;
        lbl.color     = Color.white;
        lbl.alignment = TextAlignmentOptions.Center;
        Stretch(lbl.rectTransform);

        return btn;
    }

    protected static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    protected static Sprite LoadSprite(string name)
    {
        var tex = Resources.Load<Texture2D>(name);
        if (tex == null)
        {
            Debug.LogWarning($"[ScreenBase] {name} not found in Assets/Resources/");
            return null;
        }
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
}
