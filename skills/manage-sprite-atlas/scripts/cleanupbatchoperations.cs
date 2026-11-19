// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// Demonstrates proper cleanup after batch operations on SpriteAtlases.
/// Always use try/finally blocks to ensure cleanup happens even on errors.
/// </summary>
public static class CleanupBatchOperationsExample
{
    /// <summary>
    /// Creates multiple atlases in a batch with proper cleanup.
    /// Uses try/finally to ensure AssetDatabase.ImportAsset is called for each atlas.
    /// </summary>
    public static void BatchCreateAtlases()
    {
        var atlasConfigs = new[]
        {
            new { Folder = "Assets/Sprites/UI", Output = "Assets/Atlases/UI.spriteatlasv2" },
            new { Folder = "Assets/Sprites/Characters", Output = "Assets/Atlases/Characters.spriteatlasv2" },
            new { Folder = "Assets/Sprites/Items", Output = "Assets/Atlases/Items.spriteatlasv2" }
        };

        for (int i = 0; i < atlasConfigs.Length; i++)
        {
            try
            {
                CreateAtlasFromFolder(atlasConfigs[i].Folder, atlasConfigs[i].Output);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create atlas {atlasConfigs[i].Output}: {ex.Message}");
            }
        }

        // Final cleanup - refresh asset database
        AssetDatabase.Refresh();
        Debug.Log("Batch atlas creation completed");
    }

    /// <summary>
    /// Creates an atlas from a folder and configures it.
    /// </summary>
    private static void CreateAtlasFromFolder(string folderPath, string outputPath)
    {
        // Find sprites in folder
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });

        Object[] sprites = new Object[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                AssetDatabase.GUIDToAssetPath(guids[i])
            );
        }

        // Create and save atlas
        SpriteAtlasAsset atlasAsset = new SpriteAtlasAsset();
        atlasAsset.Add(sprites);
        SpriteAtlasAsset.Save(atlasAsset, outputPath);

        // Import and configure via importer
        AssetDatabase.ImportAsset(outputPath);

        SpriteAtlasImporter importer = AssetImporter.GetAtPath(outputPath) as SpriteAtlasImporter;
        if (importer != null)
        {
            importer.includeInBuild = true;
            var packingSettings = importer.packingSettings;
            packingSettings.enableTightPacking = true;
            importer.packingSettings = packingSettings;
            importer.SaveAndReimport();
        }
    }

    /// <summary>
    /// Processes multiple atlases with progress bar and proper cleanup.
    /// </summary>
    public static void ProcessAtlasesWithProgress(string[] atlasPaths)
    {
        for (int i = 0; i < atlasPaths.Length; i++)
        {
            string path = atlasPaths[i];
            float progress = (float)i / atlasPaths.Length;

            if (EditorUtility.DisplayCancelableProgressBar(
                "Processing Atlases",
                $"Processing: {path}",
                progress))
            {
                break; // User cancelled
            }

            try
            {
                ProcessSingleAtlas(path);
            }
            finally
            {
                // Ensure progress bar is cleared even on error
                EditorUtility.ClearProgressBar();
            }
        }
    }

    /// <summary>
    /// Processes a single atlas.
    /// </summary>
    private static void ProcessSingleAtlas(string path)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        if (importer != null)
        {
            // Reimport to update
            importer.SaveAndReimport();
        }
    }
}
