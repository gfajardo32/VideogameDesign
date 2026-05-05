using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// Builds Level 2 - "Warehouse Club" - entirely from the Assets/Prefabs library.
///
/// LAYOUT CONCEPT
/// ──────────────
///  • Four VERTICAL shelf columns (rotated 90 deg), each split into a top and
///    bottom half with a 1-unit crossing gap at y=0. Creates four tall aisles
///    that run top-to-bottom between the columns — Costco / warehouse feel.
///
///    Column x-positions:  -7,  -3,  +1,  +5
///    Aisle x-centres:   -10, -5, -1, +3, +8  (left outer, 3 mid, right outer)
///
///  • Floor colour: Costco-style light polished-concrete gray.
///  • Register bottom-right. Player spawns bottom-left.
///  • 4 Shoppers (one per mid-aisle), 3 Kids, 1 Security Guard, 2 Neutral.
///  • 5 wet-floor hazards spread across every aisle.
///
/// Run:  GroceryRush / Build Level 2 - Warehouse Club
public static class Level2Builder
{
    const string PrefabRoot   = "Assets/Prefabs";
    const string ScenesFolder = "Assets/Scenes";
    const string SceneName    = "Level2_WarehouseClub";

    // ── Shelf column x-positions (4 vertical lanes) ───────────────────────────
    static readonly float[] LaneX = { -7f, -3f, 1f, 5f };

    // Each lane has two shelf units (top half and bottom half of the store).
    // After a 90-degree Z rotation the prefab becomes 0.9 wide × 6 tall.
    const float ShelfTopY    =  3.5f;   // centre of top-half shelf unit
    const float ShelfBottomY = -3.5f;   // centre of bottom-half shelf unit

    // Costco-style polished-concrete floor color
    static readonly Color FloorColor = new Color(0.82f, 0.82f, 0.82f, 1f);

    [MenuItem("GroceryRush/Build Level 2 - Warehouse Club")]
    public static void Build()
    {
        // ── 1. New empty scene ────────────────────────────────────────────────
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── 2. Global 2D light ────────────────────────────────────────────────
        var lightGO = new GameObject("Global Light 2D");
        var light   = lightGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
        light.intensity = 1f;
        light.color     = Color.white;

        // ── 3. Camera ─────────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 9f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.08f, 0.08f, 0.10f);
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camGO.AddComponent<AudioListener>();
        if (camGO.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() == null)
            camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        var follow = camGO.AddComponent<CameraFollow>();
        follow.useBounds = true;
        follow.minX = -9f;  follow.maxX = 9f;
        follow.minY = -8f;  follow.maxY = 8f;

        // ── 4. Hierarchy organisers ───────────────────────────────────────────
        var managersRoot = Folder("MANAGERS");
        var mapRoot      = Folder("MAP");
        var shelvesRoot  = Folder("Shelves",  mapRoot.transform);
        var displaysRoot = Folder("Displays", mapRoot.transform);
        var itemsRoot    = Folder("ITEMS");
        var npcsRoot     = Folder("NPCS");
        var hazardsRoot  = Folder("HAZARDS");
        var playerRoot   = Folder("PLAYER");

        // ── 5. Game Manager ───────────────────────────────────────────────────
        Place("Managers/GameManager", managersRoot.transform, Vector3.zero).name = "GameManager";

        // ── 6. Floor (light gray) — built fresh so scale is predictable ──────────
        // WallBuilder confirmed white_square at localScale = world units 1:1.
        // Store interior: 22 wide × 20 tall, centred at origin.
        var floorGO = new GameObject("Floor");
        floorGO.transform.SetParent(mapRoot.transform, false);
        floorGO.transform.position   = Vector3.zero;
        floorGO.transform.localScale = new Vector3(22f, 20f, 1f);
        var whiteSp2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/white_square.png");
        var floorSR  = floorGO.AddComponent<SpriteRenderer>();
        floorSR.sprite       = whiteSp2;
        floorSR.color        = FloorColor;
        floorSR.sortingOrder = -10;   // draw below everything

        // ── 7. Perimeter walls ────────────────────────────────────────────────
        Place("Environment/StoreWalls", mapRoot.transform, Vector3.zero).name = "Walls";

        // ── 8. Shelves — 4 vertical columns, each split top + bottom ──────────
        // Rotated 90 deg on Z: shelf becomes 0.9 wide × 6 tall.
        // A 1-unit gap at y = 0 lets the player cut across every column.
        int si = 1;
        foreach (float lx in LaneX)
        {
            foreach (float ly in new[] { ShelfTopY, ShelfBottomY })
            {
                var s = Place("Environment/Shelf", shelvesRoot.transform,
                              new Vector3(lx, ly, 0f));
                s.transform.eulerAngles = new Vector3(0f, 0f, 90f);  // rotate vertical
                s.name = $"Shelf_{si++}";
            }
        }

        // ── 9. Display stands — placed in the crossing gaps ───────────────────
        // One at the centre crossing gap of lane 2, and at top / bottom of
        // the two outer aisles to add obstacle variety.
        var dispDefs = new (string name, Vector3 pos)[]
        {
            ("DisplayStand_1", new Vector3(-5f,  7.5f, 0f)),  // top of aisle 1-2
            ("DisplayStand_2", new Vector3( 3f, -7.5f, 0f)),  // bottom of aisle 3-4
            ("DisplayStand_3", new Vector3(-1f,  0f,   0f)),  // aisle 2-3 crossing
        };
        foreach (var (dn, dp) in dispDefs)
            Place("Environment/DisplayStand", displaysRoot.transform, dp).name = dn;

        // ── 10. Register — bottom-right ───────────────────────────────────────
        Place("Environment/Register", mapRoot.transform,
              new Vector3(8f, -8.5f, 0f)).name = "Register";

        // ── 11. Items ─────────────────────────────────────────────────────────
        // Placed in editor-neutral positions; ItemSpawnRandomizer moves them at runtime.
        var itemNames = new[]
        {
            "Milk","Bread","Eggs","Cheese","Apples",
            "Juice","Cereal","Yogurt","SkimMilk","RyeBread","DietSoda"
        };
        var itemSlots = new Vector3[]
        {
            new(-9f,  7f, 0f), new(-5f,  7f, 0f), new(-1f,  7f, 0f),
            new( 3f,  7f, 0f), new( 8f,  7f, 0f),
            new(-9f, -7f, 0f), new(-5f, -7f, 0f), new(-1f, -7f, 0f),
            new( 3f, -7f, 0f), new( 8f, -7f, 0f),
            new( 0f,  0f, 0f),
        };
        for (int i = 0; i < itemNames.Length; i++)
        {
            var it = Place($"Items/{itemNames[i]}", itemsRoot.transform,
                           i < itemSlots.Length ? itemSlots[i] : Vector3.zero);
            it.name = itemNames[i];
        }

        // ── 12. NPCs ──────────────────────────────────────────────────────────
        // Shoppers patrol the four mid-aisles (between columns)
        SpawnGroup("Shopper", "Characters/Shopper", npcsRoot.transform, new[]
        {
            new Vector3(-5f,  4f, 0f),   // aisle between lane 1 & 2, upper
            new Vector3(-5f, -4f, 0f),   // aisle between lane 1 & 2, lower
            new Vector3( 3f,  4f, 0f),   // aisle between lane 3 & 4, upper
            new Vector3( 3f, -4f, 0f),   // aisle between lane 3 & 4, lower
        });

        // Kids scatter across the crossing gaps
        SpawnGroup("Kid", "Characters/Kid", npcsRoot.transform, new[]
        {
            new Vector3(-1f,  0.5f, 0f), // aisle 2-3 crossing — near display stand
            new Vector3(-7f,  0.5f, 0f), // far-left aisle crossing
            new Vector3( 5f, -0.5f, 0f), // right-side crossing
        });

        // Security Guard at the top entrance
        Place("Characters/SecurityGuard", npcsRoot.transform,
              new Vector3(0f, 8.5f, 0f)).name = "SecurityGuard";

        // Neutral shoppers in the outer aisles
        SpawnGroup("NeutralShopper", "Characters/NeutralShopper", npcsRoot.transform, new[]
        {
            new Vector3(-9.5f,  2f, 0f),  // far-left outer aisle
            new Vector3( 8.5f, -3f, 0f),  // far-right outer aisle
        });

        // ── 13. Hazards ───────────────────────────────────────────────────────
        SpawnHazards(hazardsRoot.transform, new[]
        {
            new Vector3(-5f,  0f,  0f),   // crossing gap in aisle 1-2 — centre of store
            new Vector3(-9f, -3f,  0f),   // far-left aisle lower half
            new Vector3(-1f,  5f,  0f),   // aisle 2-3 upper
            new Vector3( 3f, -2f,  0f),   // aisle 3-4 lower
            new Vector3( 8f,  4f,  0f),   // far-right upper
        });

        // ── 14. Player — bottom-left, far from register ───────────────────────
        var player = Place("Characters/Player", playerRoot.transform,
                           new Vector3(-8.5f, -7.5f, 0f));
        player.name = "Player";
        follow.target = player.transform;

        // ── 15. Save ──────────────────────────────────────────────────────────
        EnsureFolder(ScenesFolder);
        string savePath = $"{ScenesFolder}/{SceneName}.unity";
        bool saved = EditorSceneManager.SaveScene(scene, savePath);
        AssetDatabase.Refresh();

        Debug.Log(saved
            ? $"[Level2Builder] Saved to {savePath}"
            : $"[Level2Builder] Save FAILED for {savePath}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject Folder(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject Place(string subPath, Transform parent, Vector3 worldPos)
    {
        string path   = $"{PrefabRoot}/{subPath}.prefab";
        var    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"[Level2Builder] Prefab not found: {path}");
            return new GameObject($"MISSING_{subPath}");
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = worldPos;
        return go;
    }

    static void SpawnGroup(string baseName, string prefabPath,
                           Transform parent, Vector3[] positions)
    {
        for (int i = 0; i < positions.Length; i++)
            Place(prefabPath, parent, positions[i]).name = $"{baseName}_{i + 1}";
    }

    static void SpawnHazards(Transform parent, Vector3[] positions)
    {
        for (int i = 0; i < positions.Length; i++)
            Place("Hazards/WetFloor", parent, positions[i]).name = $"WetFloor_{i + 1}";
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        string child  = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, child);
    }
}
