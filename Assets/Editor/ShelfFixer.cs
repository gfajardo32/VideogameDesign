using UnityEngine;
using UnityEditor;

/// Rescales shelf GameObjects so they always appear at a fixed world size,
/// regardless of Shelf.png's pixel dimensions.
public static class ShelfFixer
{
    // How big each shelf should appear in world units
    const float TargetWidth  = 8f;
    const float TargetHeight = 1.4f;

    [MenuItem("GroceryRush/Fix Shelf Size")]
    static void Fix()
    {
        const string path = "Assets/Sprites/Shelf.png";

        // ?????? Step 1: ensure Sprite import mode at ppu=100 ???????????????????????????????????????????????????????????????
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogError("[ShelfFixer] Shelf.png importer not found."); return; }

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.SaveAndReimport();

        // ?????? Step 2: read actual pixel dimensions ??????????????????????????????????????????????????????????????????????????????????????????
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) { Debug.LogError("[ShelfFixer] Could not load Shelf.png texture."); return; }

        int W = tex.width;
        int H = tex.height;
        Debug.Log($"[ShelfFixer] Shelf.png = {W}x{H}px at ppu=100 ??? natural {W/100f:F2}x{H/100f:F2} units");

        // ?????? Step 3: compute scale so sprite renders at TargetWidth x TargetHeight ??????
        float scaleX = TargetWidth  / (W / 100f);
        float scaleY = TargetHeight / (H / 100f);
        Debug.Log($"[ShelfFixer] Applying scale ({scaleX:F3}, {scaleY:F3}) ??? {TargetWidth}x{TargetHeight} world units");

        // ?????? Step 4: apply to every shelf parent (MAP/Shelves/Shelf_N) ???????????????????????????
        int count = 0;
        var shelvesParent = GameObject.Find("Shelves");
        if (shelvesParent == null) { Debug.LogError("[ShelfFixer] 'Shelves' parent not found in scene."); return; }

        foreach (Transform child in shelvesParent.transform)
        {
            // Only the shelf bodies (not caps ??? those are grandchildren)
            if (!child.name.Contains("Shelf")) continue;
            Undo.RecordObject(child, "Fix shelf scale");
            child.localScale = new Vector3(scaleX, scaleY, 1f);
            EditorUtility.SetDirty(child.gameObject);
            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[ShelfFixer] Done ??? {count} shelves rescaled to {TargetWidth}x{TargetHeight} world units. Save with Ctrl+S.");
    }
}
