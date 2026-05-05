using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// One-click tool that applies the Environment layer + correct tags to all
/// relevant GameObjects already in the scene, and ensures the four perimeter
/// walls exist, are visible, and have solid BoxCollider2Ds.
public static class EnvironmentSetup
{
    const string EnvLayer    = "Environment";
    const string TagShelves  = "Shelves";
    const string TagDisplay  = "Display";
    const string TagWetFloor = "Wet Floor";

    [MenuItem("GroceryRush/Setup Environment Layer & Tags")]
    public static void Run()
    {
        int envLayerIndex = LayerMask.NameToLayer(EnvLayer);
        if (envLayerIndex < 0)
        {
            Debug.LogError("[EnvironmentSetup] 'Environment' layer not found in TagManager. " +
                           "Make sure it is added to Project Settings → Tags & Layers.");
            return;
        }

        int shelved = 0, wetted = 0, displayed = 0, walled = 0;

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            // ── Shelves ──────────────────────────────────────────────────────────
            if (go.name.StartsWith("Shelf_") || go.name == "Shelf")
            {
                go.tag   = TagShelves;
                go.layer = envLayerIndex;
                EnsureBoxCollider(go, isTrigger: false);
                shelved++;
            }
            // ── Wet-floor hazards ─────────────────────────────────────────────
            else if (go.name.StartsWith("WetFloor_") || go.name.StartsWith("DynamicSpill"))
            {
                try { go.tag = TagWetFloor; } catch { /* tag may not be registered yet */ }
                go.layer = envLayerIndex;
                EnsureAnyCollider(go);
                wetted++;
            }
            // ── Display fixtures ──────────────────────────────────────────────
            else if (go.name.StartsWith("Display_") || go.name == "Display")
            {
                go.tag   = TagDisplay;
                go.layer = envLayerIndex;
                EnsureBoxCollider(go, isTrigger: false);
                displayed++;
            }
            // ── Perimeter walls ───────────────────────────────────────────────
            else if (go.name.StartsWith("Wall_"))
            {
                go.layer = envLayerIndex;
                var bc = EnsureBoxCollider(go, isTrigger: false);

                // Make walls clearly visible: bright cream border colour, top sort order
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color        = new Color(0.20f, 0.18f, 0.14f, 1f); // dark charcoal
                    sr.sortingOrder = 6;
                }
                walled++;
            }
        }

        // ── Guarantee a complete wall loop ────────────────────────────────────
        EnsureWalls(envLayerIndex);

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[EnvironmentSetup] Done! " +
                  $"Shelves={shelved}, WetFloors={wetted}, Displays={displayed}, Walls={walled}. " +
                  $"Scene marked dirty — save to persist.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static BoxCollider2D EnsureBoxCollider(GameObject go, bool isTrigger)
    {
        var bc = go.GetComponent<BoxCollider2D>();
        if (bc == null) bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = isTrigger;
        return bc;
    }

    static void EnsureAnyCollider(GameObject go)
    {
        if (go.GetComponent<Collider2D>() == null)
            go.AddComponent<BoxCollider2D>();
    }

    /// Checks that all four perimeter walls exist and are correctly set up.
    /// If a wall is missing, it is created using the same white-square sprite
    /// the SceneBuilder uses.
    static void EnsureWalls(int envLayer)
    {
        var wallDefs = new (string name, Vector3 pos, Vector2 scale)[]
        {
            ("Wall_Top",    new Vector3( 0f,  10f, 0f), new Vector2(24f, 1.2f)),
            ("Wall_Bottom", new Vector3( 0f, -10f, 0f), new Vector2(24f, 1.2f)),
            ("Wall_Left",   new Vector3(-12f,  0f, 0f), new Vector2(1.2f, 22f)),
            ("Wall_Right",  new Vector3( 12f,  0f, 0f), new Vector2(1.2f, 22f)),
        };

        // Find or create the Walls parent under MAP
        Transform wallsParent = null;
        var mapGO = GameObject.Find("MAP") ?? GameObject.Find("--- MAP ---");
        if (mapGO != null)
        {
            var existing = mapGO.transform.Find("Walls");
            wallsParent = existing ?? new GameObject("Walls").transform;
            if (existing == null) wallsParent.SetParent(mapGO.transform, false);
        }

        Sprite ws = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/white_square.png");

        foreach (var (wname, pos, sz) in wallDefs)
        {
            if (GameObject.Find(wname) != null) continue; // already present

            var go = new GameObject(wname);
            if (wallsParent != null) go.transform.SetParent(wallsParent, false);
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(sz.x, sz.y, 1f);
            go.layer                = envLayer;

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = ws;
            sr.color        = new Color(0.20f, 0.18f, 0.14f, 1f);
            sr.sortingOrder = 6;

            var bc       = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = false;

            Debug.Log($"[EnvironmentSetup] Created missing wall: {wname}");
        }
    }
}
