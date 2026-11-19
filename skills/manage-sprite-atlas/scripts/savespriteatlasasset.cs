// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Demonstrates proper saving of SpriteAtlasAsset.
/// Always use SpriteAtlasAsset.Save() followed by AssetDatabase.ImportAsset().
/// </summary>
public static class SaveSpriteAtlasAssetExample
{
    /// <summary>
    /// Creates and saves a new sprite atlas asset properly.
    /// Follows the two-step V2 pattern: Create → Save → Import → Configure.
    /// </summary>
    public static void CreateAndSaveAtlas(string path, Object[] sprites)
    {
        // Step 1: Create the atlas asset in memory
        SpriteAtlasAsset atlasAsset = new SpriteAtlasAsset();

        // Step 2: Add sprites to the atlas
        if (sprites != null && sprites.Length > 0)
        {
            atlasAsset.Add(sprites);
        }

        // Step 3: Save the asset to disk
        SpriteAtlasAsset.Save(atlasAsset, path);

        // Step 4: Import the asset into Unity's asset database
        AssetDatabase.ImportAsset(path);

        Debug.Log($"Created and saved atlas at {path}");
    }

    /// <summary>
    /// Creates an atlas with automatic folder-based organization.
    /// </summary>
    public static void CreateAtlasFromFolder(string folderPath, string outputPath)
    {
        // Find all sprites in the folder
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });

        Object[] sprites = new Object[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        // Create and save the atlas
        CreateAndSaveAtlas(outputPath, sprites);

        // Configure via importer (two-step V2 pattern)
        ConfigureViaImporter(outputPath);
    }

    /// <summary>
    /// Configures an atlas after saving using SpriteAtlasImporter.
    /// This is the correct two-step approach for V2.
    /// </summary>
    private static void ConfigureViaImporter(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;

        if (importer != null)
        {
            // Configure settings
            importer.includeInBuild = true;
            var packingSettings = importer.packingSettings;
            packingSettings.enableTightPacking = true;
            packingSettings.enableAlphaDilation = false;
            importer.packingSettings = packingSettings;

            // CRITICAL: Save and reimport to apply changes
            importer.SaveAndReimport();
        }
    }

    /// <summary>
    /// Updates an existing atlas by adding more sprites.
    /// </summary>
    public static void AddSpritesToExistingAtlas(string atlasPath, Object[] newSprites)
    {
        // Load the existing atlas
        SpriteAtlasAsset atlasAsset = AssetDatabase.LoadAssetAtPath<SpriteAtlasAsset>(atlasPath);

        if (atlasAsset == null)
        {
            Debug.LogError($"Could not load atlas at {atlasPath}");
            return;
        }

        // Add new sprites
        if (newSprites != null && newSprites.Length > 0)
        {
            atlasAsset.Add(newSprites);
        }

        // Save changes
        SpriteAtlasAsset.Save(atlasAsset, atlasPath);
        AssetDatabase.ImportAsset(atlasPath);

        Debug.Log($"Added {newSprites.Length} sprites to atlas at {atlasPath}");
    }
}
