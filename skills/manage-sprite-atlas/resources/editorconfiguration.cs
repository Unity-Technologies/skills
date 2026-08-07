// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// Demonstrates proper editor configuration for SpriteAtlas V2.
/// Always use SpriteAtlasImporter for all settings in editor code.
/// </summary>
public static class EditorConfigurationExample
{
    /// <summary>
    /// Configures an existing atlas asset with proper editor settings.
    /// This is the correct pattern - create with SpriteAtlasAsset, then configure via SpriteAtlasImporter.
    /// </summary>
    public static void ConfigureSpriteAtlas(string atlasPath)
    {
        // Step 1: Ensure Sprite Packer is enabled
        EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

        // Step 2: Get the importer for configuration
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;

        if (importer == null)
        {
            Debug.LogError($"Could not get SpriteAtlasImporter for {atlasPath}");
            return;
        }

        // Step 3: Configure texture settings via importer
        var textureSettings = importer.textureSettings;
        textureSettings.generateMipMaps = false;
        textureSettings.filterMode = FilterMode.Bilinear;
        importer.textureSettings = textureSettings;

        // Step 4: Configure packing settings
        var packingSettings = importer.packingSettings;
        packingSettings.padding = 2;
        packingSettings.enableRotation = true;
        packingSettings.enableTightPacking = true;

        // Step 5: Configure platform settings
        var platformSettings = importer.GetPlatformSettings("DefaultTexturePlatform");;
        platformSettings.maxTextureSize = 2048;
        platformSettings.format = TextureImporterFormat.ASTC_6x6;
        platformSettings.compressionQuality = 100;
        importer.GetPlatformSettings("DefaultTexturePlatform");

        // Step 6: Set build inclusion
        importer.includeInBuild = true;

        // Step 7: CRITICAL - Save and reimport to apply changes
        importer.SaveAndReimport();

        Debug.Log($"Configured atlas at {atlasPath}");
    }

    /// <summary>
    /// Configures platform-specific settings for multiple platforms.
    /// </summary>
    public static void ConfigurePlatformSettings(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;

        // Mobile platform (Android/iOS)
        importer.SetPlatformSettings(new TextureImporterPlatformSettings
        {
            name = "Android",
            maxTextureSize = 1024,
            format = TextureImporterFormat.ETC2_RGB4,
            compressionQuality = 100
        });

        importer.SetPlatformSettings(new TextureImporterPlatformSettings
        {
            name = "iOS",
            maxTextureSize = 1024,
            format = TextureImporterFormat.ASTC_6x6,
            compressionQuality = 100
        });

        // Desktop platform (Windows/Mac/Linux)
        importer.SetPlatformSettings(new TextureImporterPlatformSettings
        {
            name = "Standalone",
            maxTextureSize = 1024,
            format = TextureImporterFormat.DXT5,
            compressionQuality = 100
        });


        // Save and reimport
        importer.SaveAndReimport();
    }
}
