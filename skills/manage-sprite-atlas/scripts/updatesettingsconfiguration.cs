// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// Demonstrates updating settings configuration for SpriteAtlases.
/// Shows how to migrate from old configuration patterns to V2.
/// </summary>
public static class UpdateSettingsConfigurationDocumentation
{
    /// <summary>
    /// OLD V1 Pattern: Using SetBuildSettings (NO LONGER AVAILABLE)
    ///
    /// ❌ OLD CODE:
    /// var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
    /// importer.SetBuildSettings(settings);
    ///
    /// ✅ NEW V2 CODE:
    /// var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
    /// importer.packingSettings = settings;
    /// importer.SaveAndReimport();
    /// </summary>
    public static void Update_PackingSettings()
    {
        // ===== UPDATING: Packing Settings =====

        string path = "Assets/MyAtlas.spriteatlasv2";

        // ❌ OLD (V1 - Method removed):
        /*
        var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
        importer.SetBuildSettings(settings); // Compile error!
        */

        // ✅ NEW (V2):
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        if (importer != null)
        {
            var packingSettings = importer.packingSettings;
            packingSettings.padding = 2;
            packingSettings.enableTightPacking = true;
            packingSettings.enableRotation = true;
            importer.packingSettings = packingSettings;

            // ✅ CRITICAL: Save and reimport
            importer.SaveAndReimport();
        }
    }

    /// <summary>
    /// OLD V1 Pattern: Direct texture settings modification (CHANGED)
    ///
    /// ❌ OLD CODE:
    /// var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
    /// importer.textureSettings.filterMode = FilterMode.Trilinear;
    ///
    /// ✅ NEW V2 CODE:
    /// Use SetPlatformTextureSettings() for platform-specific settings
    /// </summary>
    public static void Update_TextureSettings()
    {
        // ===== UPDATING: Texture Settings =====

        string path = "Assets/MyAtlas.spriteatlasv2";

        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        if (importer != null)
        {
            // ✅ NEW: Use platform-specific texture settings
            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone",
                maxTextureSize = 1024,
                format = TextureImporterFormat.DXT5,
                compressionQuality = 100
            });

            // ✅ CRITICAL: Save and reimport
            importer.SaveAndReimport();
        }
    }

    /// <summary>
    /// OLD V1 Pattern: Platform settings (RESTRUCTURED)
    ///
    /// ❌ OLD CODE:
    /// var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
    /// importer.platformSettings.maxTextureSize = 1024;
    ///
    /// ✅ NEW V2 CODE:
    /// Use SetPlatformTextureSettings() for each platform
    /// </summary>
    public static void Update_PlatformSettings()
    {
        // ===== UPDATING: Platform Settings =====

        string path = "Assets/MyAtlas.spriteatlasv2";

        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        if (importer != null)
        {
            // ✅ NEW: Set platform-specific settings
            // Mobile platform (Android/iOS)
            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                maxTextureSize = 1024,
                format = TextureImporterFormat.ETC2_RGB4,
                compressionQuality = 100
            });

            // IOS
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

            // ✅ CRITICAL: Save and reimport
            importer.SaveAndReimport();
        }
    }
}
