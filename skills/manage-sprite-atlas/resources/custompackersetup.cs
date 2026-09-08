// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// Utility class for creating atlases with custom packers.
/// Demonstrates how to assign a custom ScriptablePacker to a SpriteAtlasAsset.
/// </summary>
public static class CustomPackerSetup
{
    /// <summary>
    /// Creates an atlas with a custom grid packer using menu item.
    /// Menu path: Tools/Create Atlas with Custom Packer
    /// </summary>
    [MenuItem("Tools/Create Atlas with Custom Packer")]
    public static void CreateAtlasWithCustomPacker()
    {
        // Create atlas asset
        SpriteAtlasAsset atlasAsset = new SpriteAtlasAsset();

        // Add sprites from Assets/Sprites folder
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Sprites" });
        Object[] sprites = new Object[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                AssetDatabase.GUIDToAssetPath(guids[i])
            );
        }
        atlasAsset.Add(sprites);

        // Create custom packer instance
        GridSpritePacker customPacker = ScriptableObject.CreateInstance<GridSpritePacker>();
        customPacker.columns = 8;
        customPacker.cellSize = 128;
        customPacker.padding = 2;

        // Assign custom packer to atlas
        atlasAsset.SetScriptablePacker(customPacker);

        // Save atlas to disk
        string path = "Assets/CustomPackedAtlas.spriteatlas";
        SpriteAtlasAsset.Save(atlasAsset, path);
        AssetDatabase.ImportAsset(path);

        // Configure via importer
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        importer.includeInBuild = true;
        importer.SaveAndReimport();

        Debug.Log($"Created atlas with custom packer at {path}");
    }

    /// <summary>
    /// Creates an atlas with a size-optimized packer programmatically.
    /// </summary>
    public static void CreateSizeOptimizedAtlas()
    {
        SpriteAtlasAsset atlasAsset = new SpriteAtlasAsset();

        // Add sprites...
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Sprites" });
        Object[] sprites = new Object[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                AssetDatabase.GUIDToAssetPath(guids[i])
            );
        }
        atlasAsset.Add(sprites);

        // Use size-optimized packer
        SizeOptimizedPacker packer = ScriptableObject.CreateInstance<SizeOptimizedPacker>();
        packer.atlasSize = 4096;
        packer.padding = 4;

        atlasAsset.SetScriptablePacker(packer);

        string path = "Assets/OptimizedAtlas.spriteatlas";
        SpriteAtlasAsset.Save(atlasAsset, path);
        AssetDatabase.ImportAsset(path);

        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        importer.includeInBuild = true;
        importer.SaveAndReimport();
    }
}
