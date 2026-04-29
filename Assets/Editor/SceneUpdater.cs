using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// Adds register, extra obstacles, resizes objects, wires new UI elements.
public static class SceneUpdater
{
    static Sprite WhiteSprite  => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/white_square.png");
    static Sprite CircleSprite => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/circle.png");

    static GameObject MakeRect(string n, Transform parent, Vector3 pos, Vector2 size,
                                Color col, bool collider = true, bool trigger = false, int order = 0)
    {
        var go = new GameObject(n);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = WhiteSprite;
        sr.color        = col;
        sr.sortingOrder = order;
        if (collider) go.AddComponent<BoxCollider2D>().isTrigger = trigger;
        return go;
    }

    [MenuItem("GroceryRush/Update Scene (Register + Obstacles + Resize)")]
    public static void UpdateScene()
    {
        ResizeObjects();
        AddRegister();
        AddExtraObstacles();
        AddDropObstacleToNPCs();
        AddUIElements();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[SceneUpdater] Done!");
    }

    // ?????? Resize ????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
    static void ResizeObjects()
    {
        // Player
        var player = GameObject.Find("Player");
        if (player != null) player.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        // Items
        var itemsGO = GameObject.Find("--- ITEMS ---");
        if (itemsGO != null)
            foreach (Transform t in itemsGO.transform)
                t.localScale = new Vector3(0.9f, 0.9f, 1f);

        // Shoppers
        var npcsGO = GameObject.Find("--- NPCS ---");
        if (npcsGO != null)
            foreach (Transform t in npcsGO.transform)
            {
                bool isKid = t.name.StartsWith("Kid");
                t.localScale = isKid ? new Vector3(0.65f, 0.65f, 1f) : new Vector3(1.1f, 1.1f, 1f);
            }
    }

    // ?????? Register ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
    static void AddRegister()
    {
        if (GameObject.Find("Register") != null) return; // already exists

        var mapGO = GameObject.Find("--- MAP ---");

        // Counter body
        var reg = MakeRect("Register", mapGO != null ? mapGO.transform : null,
                           new Vector3(0f, -7.8f, 0f), new Vector2(5f, 1.2f),
                           new Color(0.15f, 0.55f, 0.25f), true, true, 2);
        reg.AddComponent<Register>();

        // Yellow stripe across top
        var stripe = MakeRect("Register_Stripe", reg.transform,
                              new Vector3(0f, 0.42f, 0f), new Vector2(1f, 0.15f),
                              new Color(1f, 0.85f, 0f), false, false, 3);

        // "REGISTER" label
        var lbl = new GameObject("Register_Label");
        lbl.transform.SetParent(reg.transform, false);
        lbl.transform.localPosition = new Vector3(0f, 0f, 0f);
        lbl.transform.localScale    = new Vector3(0.25f, 0.25f, 1f);
        var tmp = lbl.AddComponent<TextMeshPro>();
        tmp.text         = "REGISTER";
        tmp.fontSize     = 8f;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.color        = Color.white;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.sortingOrder = 10;

        // Arrow pointing down to register
        var arrow = new GameObject("Register_Arrow");
        arrow.transform.SetParent(reg.transform, false);
        arrow.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        arrow.transform.localScale    = new Vector3(0.3f, 0.3f, 1f);
        var arrowTMP = arrow.AddComponent<TextMeshPro>();
        arrowTMP.text         = "??? CHECKOUT ???";
        arrowTMP.fontSize     = 7f;
        arrowTMP.alignment    = TextAlignmentOptions.Center;
        arrowTMP.color        = new Color(1f, 0.85f, 0f);
        arrowTMP.fontStyle    = FontStyles.Bold;
        arrowTMP.sortingOrder = 10;

        Debug.Log("[SceneUpdater] Register added.");
    }

    // ?????? Extra obstacles ?????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
    static void AddExtraObstacles()
    {
        var npcsGO = GameObject.Find("--- NPCS ---");
        if (npcsGO == null) return;
        Transform np = npcsGO.transform;

        // 2 extra patrolling shoppers
        var extraShoppers = new (float x, float y, float wp1x, float wp1y, float wp2x, float wp2y)[]
        {
            (-1.5f, 5f,  -1.5f, 7f,  -1.5f, -1f),
            ( 1.5f, -4f,  1.5f, 6f,   1.5f, -6f),
        };
        int si = 4;
        foreach (var s in extraShoppers)
        {
            if (GameObject.Find("Shopper_" + si) != null) { si++; continue; }
            var go = MakeRect("Shopper_" + si, np,
                              new Vector3(s.x, s.y, 0f), new Vector2(1.1f, 1.1f),
                              new Color(0.55f, 0.55f, 0.60f), true, false, 4);
            go.GetComponent<SpriteRenderer>().sprite = CircleSprite;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            var ai = go.AddComponent<ShopperAI>();
            var w1 = new GameObject("WP1"); w1.transform.SetParent(go.transform, false);
            w1.transform.position = new Vector3(s.wp1x, s.wp1y, 0f);
            var w2 = new GameObject("WP2"); w2.transform.SetParent(go.transform, false);
            w2.transform.position = new Vector3(s.wp2x, s.wp2y, 0f);
            ai.waypoints.Add(w1.transform);
            ai.waypoints.Add(w2.transform);
            go.AddComponent<DropObstacle>();
            si++;
        }

        // 2 display stands (stationary, blocking)
        var stands = new (string name, float x, float y)[]
        {
            ("DisplayStand_1", -1.2f,  3.8f),
            ("DisplayStand_2",  1.2f, -1.8f),
        };
        var mapGO = GameObject.Find("--- MAP ---");
        foreach (var s in stands)
        {
            if (GameObject.Find(s.name) != null) continue;
            var go = MakeRect(s.name, mapGO != null ? mapGO.transform : null,
                              new Vector3(s.x, s.y, 0f), new Vector2(1.0f, 1.0f),
                              new Color(0.7f, 0.4f, 0.1f), true, false, 2);
            go.AddComponent<DropObstacle>();

            // Label
            var lbl = new GameObject("Label");
            lbl.transform.SetParent(go.transform, false);
            lbl.transform.localPosition = Vector3.zero;
            lbl.transform.localScale    = new Vector3(0.3f, 0.3f, 1f);
            var tmp = lbl.AddComponent<TextMeshPro>();
            tmp.text         = "DISPLAY";
            tmp.fontSize     = 5f;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.color        = Color.white;
            tmp.sortingOrder = 10;
        }
    }

    // ?????? Add DropObstacle to existing NPCs ???????????????????????????????????????????????????????????????????????????????????????
    static void AddDropObstacleToNPCs()
    {
        var npcsGO = GameObject.Find("--- NPCS ---");
        if (npcsGO == null) return;
        foreach (Transform t in npcsGO.transform)
            if (t.GetComponent<DropObstacle>() == null)
                t.gameObject.AddComponent<DropObstacle>();
    }

    // ?????? Add missing UI elements ?????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
    static void AddUIElements()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        var uiMgr = GameObject.Find("GameManager")?.GetComponent<UIManager>();
        if (uiMgr == null) return;

        // Notification text (center screen)
        if (uiMgr.notificationText == null)
        {
            var notifGO = new GameObject("NotificationText");
            notifGO.transform.SetParent(canvas.transform, false);
            var rect = notifGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.4f);
            rect.anchorMax = new Vector2(0.9f, 0.65f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var tmp = notifGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = "";
            tmp.fontSize  = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.yellow;
            tmp.fontStyle = FontStyles.Bold;
            // Add outline
            var outline = notifGO.AddComponent<Outline>();
            outline.effectColor    = Color.black;
            outline.effectDistance = new Vector2(2f, -2f);
            uiMgr.notificationText = tmp;
            notifGO.SetActive(false);
        }

        // Register banner
        if (uiMgr.registerBanner == null)
        {
            var bannerGO = new GameObject("RegisterBanner");
            bannerGO.transform.SetParent(canvas.transform, false);
            var rect = bannerGO.AddComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0f, 0f);
            rect.anchorMax        = new Vector2(1f, 0f);
            rect.pivot            = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 20f);
            rect.sizeDelta        = new Vector2(0f, 70f);
            var img = bannerGO.AddComponent<Image>();
            img.color = new Color(0.15f, 0.55f, 0.25f, 0.92f);

            var txt = new GameObject("BannerText");
            txt.transform.SetParent(bannerGO.transform, false);
            var tr = txt.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            var tmp = txt.AddComponent<TextMeshProUGUI>();
            tmp.text      = "??? ALL ITEMS COLLECTED!  Head to the Register ???";
            tmp.fontSize  = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            uiMgr.registerBanner = bannerGO;
            bannerGO.SetActive(false);
        }

        EditorUtility.SetDirty(uiMgr);
    }
}
