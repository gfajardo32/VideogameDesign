using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class UIBuilder
{
    [MenuItem("GroceryRush/Build UI")]
    public static void BuildUI()
    {
        // ?????? Canvas ????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ?????? HUD Panel ???????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        var hudGO = new GameObject("HUD");
        hudGO.transform.SetParent(canvasGO.transform, false);
        var hudRect = hudGO.AddComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = hudRect.offsetMax = Vector2.zero;

        // Timer (top center)
        var timerGO   = new GameObject("TimerText");
        timerGO.transform.SetParent(hudGO.transform, false);
        var timerRect = timerGO.AddComponent<RectTransform>();
        timerRect.anchorMin        = new Vector2(0.5f, 1f);
        timerRect.anchorMax        = new Vector2(0.5f, 1f);
        timerRect.pivot            = new Vector2(0.5f, 1f);
        timerRect.anchoredPosition = new Vector2(0f, -20f);
        timerRect.sizeDelta        = new Vector2(200f, 60f);
        var timerTMP = timerGO.AddComponent<TextMeshProUGUI>();
        timerTMP.text      = "01:30";
        timerTMP.fontSize  = 48;
        timerTMP.alignment = TextAlignmentOptions.Center;
        timerTMP.color     = Color.white;

        // Items remaining (top left)
        var itemsRemainingGO   = new GameObject("ItemsRemainingText");
        itemsRemainingGO.transform.SetParent(hudGO.transform, false);
        var irRect = itemsRemainingGO.AddComponent<RectTransform>();
        irRect.anchorMin        = new Vector2(0f, 1f);
        irRect.anchorMax        = new Vector2(0f, 1f);
        irRect.pivot            = new Vector2(0f, 1f);
        irRect.anchoredPosition = new Vector2(20f, -20f);
        irRect.sizeDelta        = new Vector2(200f, 40f);
        var irTMP = itemsRemainingGO.AddComponent<TextMeshProUGUI>();
        irTMP.text     = "Items Left: 5";
        irTMP.fontSize = 24;
        irTMP.color    = Color.white;

        // Shopping list panel (right side)
        var listPanelGO   = new GameObject("ShoppingListPanel");
        listPanelGO.transform.SetParent(hudGO.transform, false);
        var listPanelRect = listPanelGO.AddComponent<RectTransform>();
        listPanelRect.anchorMin        = new Vector2(1f, 1f);
        listPanelRect.anchorMax        = new Vector2(1f, 1f);
        listPanelRect.pivot            = new Vector2(1f, 1f);
        listPanelRect.anchoredPosition = new Vector2(-20f, -20f);
        listPanelRect.sizeDelta        = new Vector2(180f, 200f);
        var listBG = listPanelGO.AddComponent<Image>();
        listBG.color = new Color(0f, 0f, 0f, 0.5f);

        // List title
        var listTitleGO = new GameObject("ListTitle");
        listTitleGO.transform.SetParent(listPanelGO.transform, false);
        var ltRect = listTitleGO.AddComponent<RectTransform>();
        ltRect.anchorMin = new Vector2(0f, 1f); ltRect.anchorMax = new Vector2(1f, 1f);
        ltRect.pivot     = new Vector2(0.5f, 1f);
        ltRect.anchoredPosition = new Vector2(0f, -8f);
        ltRect.sizeDelta = new Vector2(0f, 30f);
        var ltTMP = listTitleGO.AddComponent<TextMeshProUGUI>();
        ltTMP.text      = "Shopping List";
        ltTMP.fontSize  = 18;
        ltTMP.fontStyle = FontStyles.Bold;
        ltTMP.alignment = TextAlignmentOptions.Center;
        ltTMP.color     = Color.yellow;

        // Scroll content container for list items
        var listContainerGO   = new GameObject("ListContainer");
        listContainerGO.transform.SetParent(listPanelGO.transform, false);
        var lcRect = listContainerGO.AddComponent<RectTransform>();
        lcRect.anchorMin = new Vector2(0f, 0f); lcRect.anchorMax = new Vector2(1f, 1f);
        lcRect.offsetMin = new Vector2(8f, 8f);
        lcRect.offsetMax = new Vector2(-8f, -40f);
        var vlg = listContainerGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing             = 4f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight     = true;

        // List item prefab (just a TMP label, will be instantiated at runtime)
        var listItemPrefab = new GameObject("ListItemPrefab");
        listItemPrefab.transform.SetParent(listPanelGO.transform, false);
        listItemPrefab.SetActive(false);
        var lipRect = listItemPrefab.AddComponent<RectTransform>();
        lipRect.sizeDelta = new Vector2(0f, 24f);
        var lipTMP = listItemPrefab.AddComponent<TextMeshProUGUI>();
        lipTMP.fontSize  = 16;
        lipTMP.color     = Color.white;
        lipTMP.alignment = TextAlignmentOptions.Left;

        // ?????? Win Screen ????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        var winGO   = new GameObject("WinScreen");
        winGO.transform.SetParent(canvasGO.transform, false);
        var winRect = winGO.AddComponent<RectTransform>();
        winRect.anchorMin = Vector2.zero; winRect.anchorMax = Vector2.one;
        winRect.offsetMin = winRect.offsetMax = Vector2.zero;
        var winBG = winGO.AddComponent<Image>();
        winBG.color = new Color(0f, 0.5f, 0f, 0.85f);

        var winTitleGO = new GameObject("WinTitle");
        winTitleGO.transform.SetParent(winGO.transform, false);
        var wtRect = winTitleGO.AddComponent<RectTransform>();
        wtRect.anchoredPosition = new Vector2(0f, 60f);
        wtRect.sizeDelta        = new Vector2(500f, 80f);
        var wtTMP = winTitleGO.AddComponent<TextMeshProUGUI>();
        wtTMP.text      = "???? CHECKOUT!";
        wtTMP.fontSize  = 64;
        wtTMP.alignment = TextAlignmentOptions.Center;
        wtTMP.color     = Color.white;

        AddRestartButton(winGO.transform, new Vector2(0f, -40f));
        winGO.SetActive(false);

        // ?????? Lose Screen ?????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        var loseGO   = new GameObject("LoseScreen");
        loseGO.transform.SetParent(canvasGO.transform, false);
        var loseRect = loseGO.AddComponent<RectTransform>();
        loseRect.anchorMin = Vector2.zero; loseRect.anchorMax = Vector2.one;
        loseRect.offsetMin = loseRect.offsetMax = Vector2.zero;
        var loseBG = loseGO.AddComponent<Image>();
        loseBG.color = new Color(0.6f, 0f, 0f, 0.85f);

        var loseTitleGO = new GameObject("LoseTitle");
        loseTitleGO.transform.SetParent(loseGO.transform, false);
        var loseTRect = loseTitleGO.AddComponent<RectTransform>();
        loseTRect.anchoredPosition = new Vector2(0f, 60f);
        loseTRect.sizeDelta        = new Vector2(600f, 80f);
        var loseTMP = loseTitleGO.AddComponent<TextMeshProUGUI>();
        loseTMP.text      = "??? TIME'S UP!";
        loseTMP.fontSize  = 64;
        loseTMP.alignment = TextAlignmentOptions.Center;
        loseTMP.color     = Color.white;

        AddRestartButton(loseGO.transform, new Vector2(0f, -40f));
        loseGO.SetActive(false);

        // ?????? Wire UIManager ????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        var gmGO = GameObject.Find("GameManager");
        if (gmGO == null) gmGO = new GameObject("GameManager");

        var uiMgr = gmGO.AddComponent<UIManager>();
        uiMgr.timerText             = timerTMP;
        uiMgr.itemsRemainingText    = irTMP;
        uiMgr.shoppingListContainer = listContainerGO.transform;
        uiMgr.listItemPrefab        = listItemPrefab;
        uiMgr.hudPanel              = hudGO;
        uiMgr.winScreen             = winGO;
        uiMgr.loseScreen            = loseGO;

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[UIBuilder] UI built and wired!");
    }

    static void AddRestartButton(Transform parent, Vector2 pos)
    {
        var btnGO   = new GameObject("RestartButton");
        btnGO.transform.SetParent(parent, false);
        var btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta        = new Vector2(200f, 60f);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(1f, 1f, 1f, 0.9f);
        var btn = btnGO.AddComponent<Button>();

        var btnTxtGO = new GameObject("Text");
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        var btnTxtRect = btnTxtGO.AddComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero; btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.offsetMin = btnTxtRect.offsetMax = Vector2.zero;
        var btnTMP = btnTxtGO.AddComponent<TextMeshProUGUI>();
        btnTMP.text      = "Play Again";
        btnTMP.fontSize  = 28;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color     = Color.black;

        // Wire button to UIManager.OnRestartButton at runtime via persistent listener
        var gmGO = GameObject.Find("GameManager");
        if (gmGO != null)
        {
            var uiMgr = gmGO.GetComponent<UIManager>();
            if (uiMgr != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    btn.onClick,
                    uiMgr.OnRestartButton);
            }
        }
    }
}
