using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// One-click prefab factory for Grocery Rush.
/// Run:  GroceryRush / Create All Prefabs
///
/// Folder layout produced:
///   Assets/Prefabs/
///     Environment/   Floor, Shelf, DisplayStand, Register, StoreWalls
///     Items/         One prefab per unique grocery item
///     Characters/    Player, Shopper, Kid, SecurityGuard, NeutralShopper
///     Hazards/       WetFloor
///     Managers/      GameManager
///
/// To build a new level: open a blank scene, drag prefabs from the Project
/// window into the hierarchy, position them, done.
public static class PrefabFactory
{
    const string Root = "Assets/Prefabs";

    // Each entry: "SceneHierarchyPath" => "PrefabName"
    // SceneHierarchyPath uses '/' for parent→child traversal.
    static readonly (string folder, (string scene, string prefab)[] items)[] Catalog =
    {
        ("Environment", new[]
        {
            ("MAP/Floor",            "Floor"),
            ("MAP/Shelves/Shelf_8",  "Shelf"),
            ("MAP/Register",         "Register"),
            ("MAP/DisplayStand_1",   "DisplayStand"),
            ("MAP/Walls",            "StoreWalls"),
        }),

        ("Items", new[]
        {
            ("ITEMS/Milk",       "Milk"),
            ("ITEMS/Bread",      "Bread"),
            ("ITEMS/Eggs",       "Eggs"),
            ("ITEMS/Cheese",     "Cheese"),
            ("ITEMS/Apples",     "Apples"),
            ("ITEMS/Juice",      "Juice"),
            ("ITEMS/Cereal",     "Cereal"),
            ("ITEMS/Yogurt",     "Yogurt"),
            ("ITEMS/Skim Milk",  "SkimMilk"),
            ("ITEMS/Rye Bread",  "RyeBread"),
            ("ITEMS/Diet Soda",  "DietSoda"),
            ("ITEMS/eggs",       "EggsAlt"),   // second eggs variant
        }),

        ("Characters", new[]
        {
            ("PLAYER/Player",            "Player"),
            ("NPCS/Shopper_1",           "Shopper"),
            ("NPCS/Kid_1",               "Kid"),
            ("NPCS/SecurityGuard",       "SecurityGuard"),
            ("NPCS/NeutralShopper_1",    "NeutralShopper"),
        }),

        ("Hazards", new[]
        {
            ("HAZARDS/WetFloor_1", "WetFloor"),
        }),

        ("Managers", new[]
        {
            ("MANAGERS/GameManager", "GameManager"),
        }),
    };

    [MenuItem("GroceryRush/Create All Prefabs")]
    public static void CreateAll()
    {
        EnsureFolder(Root);

        int created = 0, skipped = 0, failed = 0;
        var report = new System.Text.StringBuilder();
        report.AppendLine("[PrefabFactory] Results:");

        foreach (var (folder, entries) in Catalog)
        {
            string folderPath = $"{Root}/{folder}";
            EnsureFolder(folderPath);

            foreach (var (scenePath, prefabName) in entries)
            {
                string prefabPath = $"{folderPath}/{prefabName}.prefab";

                GameObject source = FindByPath(scenePath);
                if (source == null)
                {
                    report.AppendLine($"  SKIP  {scenePath}  (not found in scene)");
                    skipped++;
                    continue;
                }

                try
                {
                    PrefabUtility.SaveAsPrefabAsset(source, prefabPath, out bool ok);
                    if (ok)
                    {
                        report.AppendLine($"  OK    {prefabPath}");
                        created++;
                    }
                    else
                    {
                        report.AppendLine($"  FAIL  {prefabPath}  (SaveAsPrefabAsset returned false)");
                        failed++;
                    }
                }
                catch (System.Exception ex)
                {
                    report.AppendLine($"  ERR   {scenePath}  → {ex.Message}");
                    failed++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        report.AppendLine($"\nTotal: {created} created, {skipped} skipped, {failed} failed.");
        Debug.Log(report.ToString());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// Resolves a slash-separated path such as "MAP/Shelves/Shelf_8".
    /// The first segment uses GameObject.Find (searches entire scene);
    /// subsequent segments walk the transform hierarchy by child name.
    static GameObject FindByPath(string path)
    {
        string[] parts = path.Split('/');
        GameObject root = GameObject.Find(parts[0]);
        if (root == null) return null;

        Transform t = root.transform;
        for (int i = 1; i < parts.Length; i++)
        {
            t = t.Find(parts[i]);
            if (t == null) return null;
        }
        return t.gameObject;
    }

    /// Creates the folder at 'path' if it doesn't already exist.
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        string child  = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, child);
    }
}
