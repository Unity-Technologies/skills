// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Demonstrates proper null handling when working with SpriteAtlases.
/// Always check for null before accessing assets and importers.
/// </summary>
public static class HandleNullCasesExample
{
    /// <summary>
    /// Safely loads a sprite atlas asset with null checking.
    /// </summary>
    public static SpriteAtlasAsset LoadSpriteAtlasSafely(string path)
    {
        // Check if path is valid
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Atlas path is null or empty");
            return null;
        }

        // Load the asset with null check
        SpriteAtlasAsset atlasAsset = AssetDatabase.LoadAssetAtPath<SpriteAtlasAsset>(path);

        if (atlasAsset == null)
        {
            Debug.LogWarning($"Could not load SpriteAtlasAsset at {path}. It may not exist yet or is invalid.");
            return null;
        }

        return atlasAsset;
    }

    /// <summary>
    /// Safely gets the SpriteAtlasImporter with null checking.
    /// </summary>
    public static SpriteAtlasImporter GetSpriteAtlasImporterSafely(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Atlas path is null or empty");
            return null;
        }

        // Get importer with null check
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;

        if (importer == null)
        {
            Debug.LogWarning($"Could not get SpriteAtlasImporter for {path}. Is this a valid atlas file?");
            return null;
        }

        return importer;
    }

    /// <summary>
    /// Safely adds sprites to an atlas with null checking.
    /// </summary>
    public static bool AddSpritesToAtlasSafely(SpriteAtlasAsset atlas, Object[] sprites)
    {
        // Check all inputs
        if (atlas == null)
        {
            Debug.LogError("Cannot add sprites: atlas is null");
            return false;
        }

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("No sprites provided to add to atlas");
            return true; // Not an error, just nothing to do
        }

        // Add sprites
        atlas.Add(sprites);
        return true;
    }

    /// <summary>
    /// Safely configures an atlas with comprehensive null checking.
    /// </summary>
    public static bool ConfigureAtlasSafely(string path)
    {
        // Check path
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Cannot configure atlas: path is null or empty");
            return false;
        }

        // Load asset
        SpriteAtlasAsset atlas = LoadSpriteAtlasSafely(path);
        if (atlas == null)
        {
            return false;
        }

        // Get importer
        SpriteAtlasImporter importer = GetSpriteAtlasImporterSafely(path);
        if (importer == null)
        {
            return false;
        }

        // Configure settings
        try
        {
            importer.includeInBuild = true;
            var packingSettings = importer.packingSettings;
            packingSettings.enableTightPacking = true;
            importer.packingSettings = packingSettings;
            importer.SaveAndReimport();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to configure atlas: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Safely gets sprites from an atlas with null checking.
    /// </summary>
    public static Object[] GetAtlasSpritesSafely(SpriteAtlas atlas)
    {
        if (atlas == null)
        {
            Debug.LogError("Cannot get sprites: atlas is null");
            return new Object[0];
        }

        // Use GetPackables() - this is the V2 way
        Object[] packables = atlas.GetPackables();
        return packables ?? new Object[0];
    }
}
