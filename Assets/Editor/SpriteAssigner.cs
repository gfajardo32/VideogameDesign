using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// Run: GroceryRush / Assign Sprites
/// Fixes shelves, item sprites, character sprites all at once.
public static class SpriteAssigner
{
    // Load sprite from exact path, fixing import mode first
    static Sprite LoadExact(string assetPath)
    {
        if (!System.IO.File.Exists(
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), assetPath)))
        {
            Debug.LogWarning($"[SpriteAssigner] File not found: {assetPath}");
            return null;
        }

        var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp != null)
        {
            bool needsReimport = imp.textureType != TextureImporterType.Sprite;
            if (needsReimport)
            {
                imp.textureType         = TextureImporterType.Sprite;
                imp.spriteImportMode    = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100f;
                imp.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    // Load sprite AND set ppu so it renders at <=1 world unit naturally.
    // Then scale the GO to the desired size.
    static Sprite LoadNormalized(string assetPath, out float naturalW, out float naturalH)
    {
        naturalW = 1f; naturalH = 1f;

        if (!System.IO.File.Exists(
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), assetPath)))
        {
            Debug.LogWarning($"[SpriteAssigner] File not found: {assetPath}");
            return null;
        }

        var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp == null) return null;

        if (imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.SaveAndReimport();
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null) return null;

        int W = tex.width, H = tex.height;
        // ppu = largest dimension => sprite fits in 1x1 world-unit box
        float ppu = Mathf.Max(W, H);
        naturalW = W / ppu;
        naturalH = H / ppu;

        if (Mathf.Abs(imp.spritePixelsPerUnit - ppu) > 0.5f)
        {
            imp.spritePixelsPerUnit = ppu;
            imp.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    // item name => exact asset path (lowercase keys for case-insensitive matching)
    static readonly Dictionary<string, string> ItemPaths =
        new Dictionary<string, string>
    {
        // ---- Good items (shopping list targets) ----
        { "apples",        "Assets/Sprites/apples.png"      },
        { "bananas",       "Assets/Sprites/bananas.png"     },
        { "bread",         "Assets/Sprites/bread.png"       },
        { "butter",        "Assets/Sprites/Butter.png"      },
        { "cereal",        "Assets/Sprites/Cereal.png"      },
        { "cheese",        "Assets/Sprites/cheese.png"      },
        { "eggs",          "Assets/Sprites/eggs.png"        },
        { "milk",          "Assets/Sprites/Milk.png"        },
        { "orange juice",  "Assets/Sprites/OrangeJuice.png" },
        { "orangejuice",   "Assets/Sprites/OrangeJuice.png" },
        { "yogurt",        "Assets/Sprites/Yogurt.png"      },
        // ---- Variant / bad-item names (map to nearest available sprite) ----
        { "juice",         "Assets/Sprites/OrangeJuice.png" },
        { "rye bread",     "Assets/Sprites/bread.png"       },
        { "skim milk",     "Assets/Sprites/Milk.png"        },
        { "diet soda",     "Assets/Sprites/OrangeJuice.png" },
    };

    [MenuItem("GroceryRush/Assign Sprites")]
    static void AssignAll()
    {
        // ---- Character sprites (exact paths, ppu=100) ----
        Sprite pIdleDown  = LoadExact("Assets/Sprites/player_facing_screen.png");
        Sprite pIdleUp    = LoadExact("Assets/Sprites/player_facing_away.png");
        Sprite pIdleRight = LoadExact("Assets/Sprites/player_facing_right.png");
        Sprite pWalkRight = LoadExact("Assets/Sprites/Player_walking_right.png");
        Sprite pWalkUp    = LoadExact("Assets/Sprites/Player_facing_away_walking.png");

        Sprite nWalkRight = LoadExact("Assets/Sprites/Normal_npc_facing_right_walking.png");
        Sprite nIdleDown  = LoadExact("Assets/Sprites/Normal_npc_facing_screen.png");
        Sprite nIdleUp    = LoadExact("Assets/Sprites/Normal_npc_back_facing_screen.png");
        Sprite mWalkRight = LoadExact("Assets/Sprites/Mad_npc_facing_right_walking.png");
        Sprite mIdleDown  = LoadExact("Assets/Sprites/Mad_npc_facing_screen.png");

        Sprite gWalkRight = LoadExact("Assets/Sprites/Security_guard_facing_rigth_walking.png");
        Sprite gIdleDown  = LoadExact("Assets/Sprites/Security_guard_facing_screen.png");
        Sprite gIdleUp    = LoadExact("Assets/Sprites/Security_guard_facing_away.png");

        Sprite kWalkRight = LoadExact("Assets/Sprites/Running_kid_facing_right_walking.png");
        Sprite kIdleDown  = LoadExact("Assets/Sprites/Running_kid_facing_screen.png");
        Sprite kIdleUp    = LoadExact("Assets/Sprites/Running_kid_facing_away.png");

        // ---- Shelf sprite (normalized so GO scale controls world size) ----
        float shelfNatW, shelfNatH;
        Sprite shelfSprite = LoadNormalized("Assets/Sprites/Shelf.png", out shelfNatW, out shelfNatH);

        float targetShelfW = 8f;
        float targetShelfH = 1.4f;
        float shelfScaleX  = shelfNatW > 0 ? targetShelfW / shelfNatW : 8f;
        float shelfScaleY  = shelfNatH > 0 ? targetShelfH / shelfNatH : 1.4f;

        Debug.Log($"[SpriteAssigner] Shelf natural={shelfNatW:F2}x{shelfNatH:F2} => scale ({shelfScaleX:F2},{shelfScaleY:F2})");

        // ---- Wet floor sprite ----
        Sprite wetFloor = LoadExact("Assets/Sprites/wet_floor.png");

        int changed = 0;

        // ---- Player ----
        foreach (var c in Object.FindObjectsByType<CartController>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(c, "Assign player sprites");
            c.spriteIdleDown  = pIdleDown;
            c.spriteIdleUp    = pIdleUp;
            c.spriteIdleRight = pIdleRight;
            c.spriteWalkRight = pWalkRight;
            c.spriteWalkUp    = pWalkUp;
            var sr = c.GetComponent<SpriteRenderer>();
            if (sr)
            {
                Undo.RecordObject(sr, "Set player sprite");
                sr.sprite = pIdleDown;
                sr.color  = Color.white;
                sr.flipX  = false;
            }
            // Reset rotation ??? sprite system handles direction
            var t = c.transform;
            Undo.RecordObject(t, "Reset player rotation");
            t.rotation = Quaternion.identity;
            EditorUtility.SetDirty(c);
            changed++;
        }

        // ---- ShopperAI ----
        foreach (var s in Object.FindObjectsByType<ShopperAI>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(s, "Assign shopper sprites");
            s.spritePatrolRight = nWalkRight; s.spritePatrolDown = nIdleDown; s.spritePatrolUp = nIdleUp;
            s.spriteChaseRight  = mWalkRight; s.spriteChaseDown  = mIdleDown;
            var sr = s.GetComponent<SpriteRenderer>();
            if (sr) { Undo.RecordObject(sr, "Set shopper sprite"); sr.sprite = nIdleDown; sr.color = Color.white; }
            EditorUtility.SetDirty(s); changed++;
        }

        // ---- NeutralShopper ----
        foreach (var n in Object.FindObjectsByType<NeutralShopper>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(n, "Assign neutral sprites");
            n.spriteCalmRight = nWalkRight; n.spriteCalmDown = nIdleDown; n.spriteCalmUp = nIdleUp;
            n.spriteMadRight  = mWalkRight; n.spriteMadDown  = mIdleDown;
            var sr = n.GetComponent<SpriteRenderer>();
            if (sr) { Undo.RecordObject(sr, "Set neutral sprite"); sr.sprite = nIdleDown; sr.color = Color.white; }
            EditorUtility.SetDirty(n); changed++;
        }

        // ---- SecurityGuard ----
        foreach (var g in Object.FindObjectsByType<SecurityGuard>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(g, "Assign guard sprites");
            g.spriteWalkRight = gWalkRight; g.spriteIdleDown = gIdleDown; g.spriteIdleUp = gIdleUp;
            var sr = g.GetComponent<SpriteRenderer>();
            if (sr) { Undo.RecordObject(sr, "Set guard sprite"); sr.sprite = gIdleDown; sr.color = Color.white; }
            EditorUtility.SetDirty(g); changed++;
        }

        // ---- KidAI ----
        foreach (var k in Object.FindObjectsByType<KidAI>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(k, "Assign kid sprites");
            k.spriteWalkRight = kWalkRight; k.spriteIdleDown = kIdleDown; k.spriteIdleUp = kIdleUp;
            var sr = k.GetComponent<SpriteRenderer>();
            if (sr) { Undo.RecordObject(sr, "Set kid sprite"); sr.sprite = kIdleDown; sr.color = Color.white; }
            EditorUtility.SetDirty(k); changed++;
        }

        // ---- Shelves ----
        var shelvesParent = GameObject.Find("Shelves");
        if (shelvesParent != null && shelfSprite != null)
        {
            foreach (Transform child in shelvesParent.transform)
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                Undo.RecordObject(sr, "Set shelf sprite");
                Undo.RecordObject(child, "Set shelf scale");
                sr.sprite = shelfSprite;
                sr.color  = Color.white;
                child.localScale = new Vector3(shelfScaleX, shelfScaleY, 1f);
                EditorUtility.SetDirty(sr);
                EditorUtility.SetDirty(child.gameObject);
                changed++;
            }
        }
        else
        {
            if (shelvesParent == null) Debug.LogWarning("[SpriteAssigner] Could not find 'Shelves' parent GameObject!");
            if (shelfSprite == null)  Debug.LogWarning("[SpriteAssigner] Shelf sprite is null ??? check Assets/Sprites/Shelf.png");
        }

        // ---- Items ----
        // Pre-load all item sprites normalized (fits in <=1x1 world-unit box)
        var itemSpriteCache = new Dictionary<string, Sprite>();
        foreach (var kv in ItemPaths)
        {
            if (itemSpriteCache.ContainsKey(kv.Key)) continue;
            float nw, nh;
            var sp = LoadNormalized(kv.Value, out nw, out nh);
            if (sp != null) itemSpriteCache[kv.Key] = sp;
        }

        foreach (var item in Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
        {
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            Undo.RecordObject(sr, "Assign item sprite");
            Undo.RecordObject(item.transform, "Set item scale");

            string key = item.itemName.ToLower().Trim();
            if (itemSpriteCache.TryGetValue(key, out Sprite sp))
            {
                sr.sprite = sp;
                sr.color  = Color.white;
                // Sprites normalized to <=1 unit => scale to visible size
                item.transform.localScale = Vector3.one * 1.2f;
                Debug.Log($"[SpriteAssigner] OK {item.itemName} => {sp.name}");
            }
            else
            {
                Debug.LogWarning($"[SpriteAssigner] No sprite mapping for item: '{item.itemName}'");
            }
            EditorUtility.SetDirty(sr);
            changed++;
        }

        // ---- HazardZone ----
        if (wetFloor != null)
        {
            foreach (var h in Object.FindObjectsByType<HazardZone>(FindObjectsSortMode.None))
            {
                var sr = h.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                Undo.RecordObject(sr, "Set wet floor sprite");
                sr.sprite = wetFloor;
                sr.color  = new Color(1f, 1f, 1f, 0.8f);
                EditorUtility.SetDirty(sr);
                changed++;
            }
        }

        AssetDatabase.Refresh();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[SpriteAssigner] Done ??? {changed} objects updated. Save with Ctrl+S.");
    }
}
