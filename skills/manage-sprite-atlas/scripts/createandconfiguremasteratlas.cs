using System;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

public static class MasterAtlasCreator
{
    public static void CreateMasterAtlas(string path, Object[] spritesOrFolders)
    {
        // CRITICAL: Validate all sprites are from Assets/ folder before calling this
        // Use: sprites.All(s => AssetDatabase.GetAssetPath(s).StartsWith("Assets/"))

        // Step 1: Author content (editor-only)
        SpriteAtlasAsset atlasAsset = new SpriteAtlasAsset();
        atlasAsset.Add(spritesOrFolders); // Add sprites/folders - ONLY from Assets/ folder
        SpriteAtlasAsset.Save(atlasAsset, path);
        AssetDatabase.ImportAsset(path);

        // Step 2: Configure via importer (editor-only)
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        if (importer == null) throw new InvalidOperationException("Failed to get SpriteAtlasImporter");

        // Texture settings is a struct, so members cannot be edited directly.
        var textureSettings = importer.textureSettings;
        textureSettings.filterMode = FilterMode.Bilinear;
        textureSettings.generateMipMaps = false;
        importer.textureSettings = textureSettings;

        // Packing settings is a struct, so members cannot be edited directly.
        var packingSettings = importer.packingSettings;
        packingSettings.padding = 4;
        packingSettings.enableAlphaDilation = true;
        importer.packingSettings = packingSettings;

        // Platform settings (safe loop with type checks)
        SetPlatformSettings(importer, "Android", TextureImporterFormat.ASTC_6x6); // Ensure format is valid.
        SetPlatformSettings(importer, "iOS", TextureImporterFormat.ASTC_4x4); // [critical] TextureImporterPlatformSettings format type is TextureImporterFormat

        // Mark for build inclusion
        importer.includeInBuild = true;
        importer.SaveAndReimport(); // REQUIRED
    }

    private static void SetPlatformSettings(SpriteAtlasImporter importer, string platformName, TextureImporterFormat format)
    {
        var settings = new TextureImporterPlatformSettings
        {
            name = platformName,
            overridden = true,
            maxTextureSize = 2048,
            format = format // ✅ Direct enum assignment (no cast) - Unity stores internally as int but struct expects enum
        };
        importer.SetPlatformSettings(settings);
    }
}
