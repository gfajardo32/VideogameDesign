using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// Destroys all old thin wall objects and replaces them with thick,
/// clearly-visible two-layer walls that look like real building walls:
///
///   [dark outside void]  ──►  [charcoal wall body 2.5u]  ──►  [bright inner face 0.4u]  ──►  [store floor]
///
/// Run:  GroceryRush / Rebuild Visible Walls
public static class WallBuilder
{
    // ── Store floor bounds (must match SceneBuilder floor: 22 × 20 centred at origin) ──
    const float FloorHalfW = 11f;   // x: –11 … +11
    const float FloorHalfH = 10f;   // y: –10 … +10

    // ── Wall geometry ──────────────────────────────────────────────────────────────────
    const float WallThick  = 2.5f;  // total wall depth (world units)
    const float FaceThick  = 0.40f; // interior-face strip thickness

    // ── Wall colours ──────────────────────────────────────────────────────────────────
    static readonly Color ColWall  = new Color(0.22f, 0.19f, 0.15f, 1f); // dark charcoal body
    static readonly Color ColFace  = new Color(0.93f, 0.91f, 0.87f, 1f); // bright off-white face

    // ── Sorting orders ────────────────────────────────────────────────────────────────
    const int SortWall = 8;   // above shelves (7) and floor (–10)
    const int SortFace = 9;   // on top of the wall body

    [MenuItem("GroceryRush/Rebuild Visible Walls")]
    public static void Rebuild()
    {
        int envLayer = LayerMask.NameToLayer("Environment");
        if (envLayer < 0)
        {
            Debug.LogWarning("[WallBuilder] 'Environment' layer missing — walls will use Default layer. " +
                             "Run 'Setup Environment Layer & Tags' first.");
            envLayer = 0;
        }

        Sprite ws = GetWhiteSprite();

        // ── 1. Remove old wall objects ─────────────────────────────────────────────
        string[] oldNames = { "Wall_Top","Wall_Bottom","Wall_Left","Wall_Right",
                              "Wall_Top_Face","Wall_Bottom_Face","Wall_Left_Face","Wall_Right_Face",
                              "Walls" };
        foreach (var n in oldNames)
        {
            var existing = GameObject.Find(n);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        // ── 2. Find / create parent container ─────────────────────────────────────
        var mapGO = GameObject.Find("MAP") ?? GameObject.Find("--- MAP ---") ?? new GameObject("MAP");
        var wallsParent = new GameObject("Walls");
        wallsParent.transform.SetParent(mapGO.transform, false);

        // ── 3. Build four walls ───────────────────────────────────────────────────
        //
        // Each wall = BODY (dark, solid collider) + FACE (light, visual only)
        //
        // Horizontal walls extend past FloorHalfW so they fully cover the corners.
        float hw  = FloorHalfW;
        float hh  = FloorHalfH;
        float wt  = WallThick;
        float ft  = FaceThick;
        float ext = wt + 0.5f;               // how far H-walls overhang left/right

        // Centres of the wall bodies (outer edge is flush with / beyond the floor)
        //   Top:    bottom edge at y = +hh  →  centre y = hh + wt/2
        //   Bottom: top edge   at y = –hh  →  centre y = –(hh + wt/2)
        //   Left:   right edge at x = –hw  →  centre x = –(hw + wt/2)
        //   Right:  left edge  at x = +hw  →  centre x = hw + wt/2

        float HWallWidth = (hw + ext) * 2f;  // full width for top/bottom (covers corners)
        float VWallHeight = hh * 2f;          // height for left/right (floor span only)

        // Body positions & sizes
        var bodies = new (string name, Vector3 pos, Vector2 sz)[]
        {
            ("Wall_Top",    new Vector3(0f,    hh + wt * 0.5f, 0f), new Vector2(HWallWidth, wt)),
            ("Wall_Bottom", new Vector3(0f,  -(hh + wt * 0.5f),0f), new Vector2(HWallWidth, wt)),
            ("Wall_Left",   new Vector3(-(hw + wt * 0.5f), 0f, 0f), new Vector2(wt, VWallHeight + wt * 2f)),
            ("Wall_Right",  new Vector3( hw + wt * 0.5f,  0f, 0f),  new Vector2(wt, VWallHeight + wt * 2f)),
        };

        // Interior-face strip positions & sizes (hugs the inside edge of the body)
        var faces = new (string name, Vector3 pos, Vector2 sz)[]
        {
            ("Wall_Top_Face",    new Vector3(0f,   hh + ft * 0.5f, 0f), new Vector2(hw * 2f, ft)),
            ("Wall_Bottom_Face", new Vector3(0f, -(hh + ft * 0.5f),0f), new Vector2(hw * 2f, ft)),
            ("Wall_Left_Face",   new Vector3(-(hw + ft * 0.5f), 0f,0f), new Vector2(ft, hh * 2f)),
            ("Wall_Right_Face",  new Vector3( hw + ft * 0.5f,  0f,0f),  new Vector2(ft, hh * 2f)),
        };

        foreach (var (nm, pos, sz) in bodies)
            MakeWallSegment(ws, nm, wallsParent.transform, pos, sz,
                            ColWall, SortWall, envLayer, hasCollider: true);

        foreach (var (nm, pos, sz) in faces)
            MakeWallSegment(ws, nm, wallsParent.transform, pos, sz,
                            ColFace, SortFace, envLayer, hasCollider: false);

        // ── 4. Save ───────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[WallBuilder] Done! Thick visible walls rebuilt. " +
                  "Run File → Save to persist.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────────

    static GameObject MakeWallSegment(Sprite ws, string name, Transform parent,
                                      Vector3 pos, Vector2 sz,
                                      Color col, int sortOrder,
                                      int layer, bool hasCollider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(sz.x, sz.y, 1f);
        go.layer                = layer;

        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = ws;
        sr.color        = col;
        sr.sortingOrder = sortOrder;

        if (hasCollider)
        {
            var bc       = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = false;
        }
        return go;
    }

    static Sprite GetWhiteSprite()
    {
        const string path = "Assets/white_square.png";
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;

        // Fallback: create on the fly if missing
        var tex = new Texture2D(4, 4);
        var px  = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        System.IO.File.WriteAllBytes(Application.dataPath + "/white_square.png", tex.EncodeToPNG());
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
