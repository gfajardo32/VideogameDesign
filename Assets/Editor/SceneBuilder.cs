using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class SceneBuilder
{
    // ?????? Shared helpers ????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
    static Sprite GetWhiteSprite()
    {
        string spritePath = "Assets/white_square.png";
        if (!File.Exists(Application.dataPath + "/white_square.png"))
        {
            Texture2D tex = new Texture2D(32, 32);
            Color[] px = new Color[1024];
            for (int i = 0; i < 1024; i++) px[i] = Color.white;
            tex.SetPixels(px);
            File.WriteAllBytes(Application.dataPath + "/white_square.png", tex.EncodeToPNG());
            AssetDatabase.Refresh();
        }
        TextureImporter imp = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType      = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    static GameObject MakeRect(Sprite ws, string n, Transform parent, Vector3 pos,
                                Vector2 size, Color col, bool hasCollider = true,
                                bool isTrigger = false, int sortOrder = 0)
    {
        var go = new GameObject(n);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = ws;
        sr.color        = col;
        sr.sortingOrder = sortOrder;
        if (hasCollider)
        {
            var bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = isTrigger;
        }
        return go;
    }

    // ?????? Build Scene ?????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
    [MenuItem("GroceryRush/Build Scene")]
    public static void BuildScene()
    {
        Sprite ws = GetWhiteSprite();

        // MANAGERS
        var managersGO = new GameObject("--- MANAGERS ---");
        var gmGO       = new GameObject("GameManager");
        gmGO.transform.SetParent(managersGO.transform, false);
        gmGO.AddComponent<GameManager>();
        gmGO.AddComponent<ShoppingList>();

        // MAP
        var mapGO = new GameObject("--- MAP ---");
        MakeRect(ws, "Floor", mapGO.transform, Vector3.zero, new Vector2(22f, 20f),
                 new Color(0.94f, 0.90f, 0.80f), false, false, -10);

        var wallsGO = new GameObject("Walls");
        wallsGO.transform.SetParent(mapGO.transform, false);
        Color wc = new Color(0.25f, 0.22f, 0.18f);
        MakeRect(ws, "Wall_Top",    wallsGO.transform, new Vector3( 0f,  9.5f, 0f), new Vector2(22f, 1f), wc, true, false, 2);
        MakeRect(ws, "Wall_Bottom", wallsGO.transform, new Vector3( 0f, -9.5f, 0f), new Vector2(22f, 1f), wc, true, false, 2);
        MakeRect(ws, "Wall_Left",   wallsGO.transform, new Vector3(-11f,  0f,  0f), new Vector2(1f, 20f), wc, true, false, 2);
        MakeRect(ws, "Wall_Right",  wallsGO.transform, new Vector3( 11f,  0f,  0f), new Vector2(1f, 20f), wc, true, false, 2);

        var shelvesGO = new GameObject("Shelves");
        shelvesGO.transform.SetParent(mapGO.transform, false);
        Color sc = new Color(0.55f, 0.35f, 0.15f);
        float sw2 = 6f, sh2 = 0.9f, lx = -4.5f, rx = 4.5f;
        float[] rows = { 5.5f, 2.5f, -0.5f, -3.5f };
        int si = 1;
        foreach (float ry in rows)
        {
            MakeRect(ws, "Shelf_" + si++, shelvesGO.transform, new Vector3(lx, ry, 0f), new Vector2(sw2, sh2), sc, true, false, 1);
            MakeRect(ws, "Shelf_" + si++, shelvesGO.transform, new Vector3(rx, ry, 0f), new Vector2(sw2, sh2), sc, true, false, 1);
        }

        // PLAYER
        var playerParent = new GameObject("--- PLAYER ---");
        var player = MakeRect(ws, "Player", playerParent.transform,
                              new Vector3(0f, -6.5f, 0f), new Vector2(0.8f, 0.8f),
                              new Color(0.2f, 0.6f, 1f), true, false, 5);
        player.tag = "Player";
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale  = 0f;
        rb.constraints   = RigidbodyConstraints2D.FreezeRotation;
        rb.linearDamping = 8f;
        player.AddComponent<CartController>();

        var cam = GameObject.FindWithTag("MainCamera");
        if (cam != null)
        {
            var cf    = cam.AddComponent<CameraFollow>();
            cf.target = player.transform;
        }

        new GameObject("--- ITEMS ---");
        new GameObject("--- NPCS ---");
        new GameObject("--- HAZARDS ---");

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[SceneBuilder] Scene built!");
    }

    // ?????? Build Items ?????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
    [MenuItem("GroceryRush/Build Items")]
    public static void BuildItems()
    {
        Sprite ws = GetWhiteSprite();

        var itemsParent = GameObject.Find("--- ITEMS ---");
        if (itemsParent == null)
        {
            Debug.LogError("[SceneBuilder] Run 'Build Scene' first!");
            return;
        }
        Transform ip = itemsParent.transform;

        // (name, x, y, color)
        var items = new (string name, float x, float y, Color col)[]
        {
            ("Milk",   -8f,  4f,  new Color(0.95f, 0.95f, 1.00f)),  // pale blue
            ("Bread",   8f,  4f,  new Color(0.85f, 0.65f, 0.30f)),  // golden
            ("Eggs",    0f,  1f,  new Color(1.00f, 0.95f, 0.75f)),  // cream
            ("Cheese", -8f, -2f,  new Color(1.00f, 0.85f, 0.10f)),  // yellow
            ("Apples",  8f, -2f,  new Color(0.85f, 0.15f, 0.15f)),  // red
        };

        foreach (var item in items)
        {
            var go = MakeRect(ws, item.name, ip,
                              new Vector3(item.x, item.y, 0f),
                              new Vector2(0.5f, 0.5f),
                              item.col, true, true, 3);
            var pickup = go.AddComponent<ItemPickup>();
            pickup.itemName = item.name;
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[SceneBuilder] Items placed!");
    }

    // ?????? Build NPCs & Hazards ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????
    [MenuItem("GroceryRush/Build NPCs and Hazards")]
    public static void BuildNPCsAndHazards()
    {
        Sprite ws = GetWhiteSprite();

        var npcsParent    = GameObject.Find("--- NPCS ---");
        var hazardsParent = GameObject.Find("--- HAZARDS ---");
        if (npcsParent == null || hazardsParent == null)
        {
            Debug.LogError("[SceneBuilder] Run 'Build Scene' first!");
            return;
        }

        // ?????? Shoppers (gray, patrol between 2 waypoints each) ??????????????????????????????
        Color shopperCol = new Color(0.6f, 0.6f, 0.65f);
        var shopperData = new (float x, float y, float wp1x, float wp1y, float wp2x, float wp2y)[]
        {
            (-8f,  4f,  -8f,  6f,   -8f, -3f),   // left aisle, patrols up-down
            ( 8f,  1f,   8f,  5f,    8f, -3f),   // right aisle, patrols up-down
            ( 0f,  3f,   0f,  6f,    0f,  0f),   // center aisle, short patrol
        };

        int si = 1;
        foreach (var s in shopperData)
        {
            var shopperGO = MakeRect(ws, "Shopper_" + si, npcsParent.transform,
                                     new Vector3(s.x, s.y, 0f), new Vector2(0.7f, 0.7f),
                                     shopperCol, true, false, 4);
            var rb = shopperGO.AddComponent<Rigidbody2D>();
            rb.gravityScale  = 0f;
            rb.constraints   = RigidbodyConstraints2D.FreezeRotation;
            rb.linearDamping = 8f;

            // Waypoints as child empty GOs
            var ai = shopperGO.AddComponent<ShopperAI>();
            var wp1 = new GameObject("WP1"); wp1.transform.SetParent(shopperGO.transform, false);
            wp1.transform.position = new Vector3(s.wp1x, s.wp1y, 0f);
            var wp2 = new GameObject("WP2"); wp2.transform.SetParent(shopperGO.transform, false);
            wp2.transform.position = new Vector3(s.wp2x, s.wp2y, 0f);
            ai.waypoints.Add(wp1.transform);
            ai.waypoints.Add(wp2.transform);
            si++;
        }

        // ?????? Kids (pink, erratic) ??????????????????????????????????????????????????????????????????????????????????????????????????????????????????
        Color kidCol = new Color(1f, 0.5f, 0.7f);
        var kidPositions = new Vector2[] { new Vector2(-3f, 0f), new Vector2(3f, -5f) };
        int ki = 1;
        foreach (var kp in kidPositions)
        {
            var kidGO = MakeRect(ws, "Kid_" + ki, npcsParent.transform,
                                  new Vector3(kp.x, kp.y, 0f), new Vector2(0.45f, 0.45f),
                                  kidCol, true, false, 4);
            var rb = kidGO.AddComponent<Rigidbody2D>();
            rb.gravityScale  = 0f;
            rb.constraints   = RigidbodyConstraints2D.FreezeRotation;
            rb.linearDamping = 2f;
            kidGO.AddComponent<KidAI>();
            ki++;
        }

        // ?????? Wet floor hazards (cyan, trigger) ???????????????????????????????????????????????????????????????????????????
        Color wetCol = new Color(0.5f, 0.85f, 1f, 0.6f);
        var wetPositions = new Vector2[] { new Vector2(0f, -1.5f), new Vector2(-8f, 1f) };
        int wi = 1;
        foreach (var wp in wetPositions)
        {
            var wetGO = MakeRect(ws, "WetFloor_" + wi, hazardsParent.transform,
                                  new Vector3(wp.x, wp.y, 0f), new Vector2(1.5f, 1.5f),
                                  wetCol, true, true, 0);
            wetGO.AddComponent<HazardZone>();
            wi++;
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[SceneBuilder] NPCs & Hazards placed!");
    }
}
