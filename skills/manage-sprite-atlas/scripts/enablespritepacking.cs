// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// Demonstrates enabling and configuring Sprite Packer for SpriteAtlases.
/// Proper packing configuration is essential for efficient atlas usage.
/// </summary>
public static class EnableSpritePackingExample
{
    /// <summary>
    /// Enables Sprite Packer mode in Project Settings.
    /// This must be done before creating atlases.
    /// </summary>
    public static void EnableSpritePacker()
    {
        EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
        Debug.Log("Sprite Packer V2 enabled");
    }

    /// <summary>
    /// Configures packing settings for an atlas.
    /// Tight packing reduces texture memory usage.
    /// </summary>
    public static void ConfigurePackingSettings(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        // Enable tight packing for better space utilization
        var packingSettings = importer.packingSettings;
        packingSettings.enableTightPacking = true;
        importer.packingSettings = packingSettings;

        importer.SaveAndReimport();
    }

    /// <summary>
    /// Configures packing settings with rotation enabled.
    /// Rotation can significantly improve packing density for irregular sprites.
    /// </summary>
    public static void EnableRotationPacking(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        var packingSettings = importer.packingSettings;
        packingSettings.enableRotation = true;
        importer.packingSettings = packingSettings;

        importer.SaveAndReimport();
    }

    /// <summary>
    /// Configures packing settings with padding.
    /// Add padding to prevent texture bleeding between adjacent sprites.
    /// </summary>
    public static void ConfigurePackingWithPadding(string atlasPath, int paddingPixels)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        var packingSettings = importer.packingSettings;
        packingSettings.padding = 2;
        importer.packingSettings = packingSettings;

        importer.SaveAndReimport();
    }

    /// <summary>
    /// Configures packing settings for UI atlases.
    /// UI sprites typically need consistent positioning without rotation.
    /// </summary>
    public static void ConfigureUIPacking(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        // UI sprites usually shouldn't be rotated
        var packingSettings = importer.packingSettings;
        packingSettings.padding = 2;
        packingSettings.enableTightPacking = false;
        packingSettings.enableRotation = false;
        packingSettings.enableAlphaDilation = false;
        importer.packingSettings = packingSettings;

        importer.SaveAndReimport();
    }

    /// <summary>
    /// Configures packing settings for character/animation atlases.
    /// Animation frames need consistent positioning across frames.
    /// </summary>
    public static void ConfigureAnimationPacking(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        // UI sprites usually shouldn't be rotated
        var packingSettings = importer.packingSettings;
        packingSettings.padding = 2;
        packingSettings.enableTightPacking = true;
        packingSettings.enableRotation = false;
        packingSettings.enableAlphaDilation = false;
        importer.packingSettings = packingSettings;

        importer.SaveAndReimport();
    }
}

/// <summary>
/// Editor window for batch configuring sprite packing settings.
/// </summary>
public class ConfigurePackingWindow : EditorWindow
{
    private string _atlasFolder = "Assets/Atlases";
    private bool _tightPacking = true;
    private bool _allowRotation = false;
    private int _padding = 2;

    [MenuItem("Tools/Configure Sprite Packing")]
    public static void ShowWindow()
    {
        GetWindow<ConfigurePackingWindow>("Configure Packing");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Batch Configure Packing", EditorStyles.boldLabel);

        _atlasFolder = EditorGUILayout.TextField("Atlas Folder", _atlasFolder);
        _tightPacking = EditorGUILayout.Toggle("Tight Packing", _tightPacking);
        _allowRotation = EditorGUILayout.Toggle("Allow Rotation", _allowRotation);
        _padding = EditorGUILayout.IntField("Padding (pixels)", _padding);

        if (GUILayout.Button("Configure Atlases"))
        {
            BatchConfigure();
        }
    }

    private void BatchConfigure()
    {
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlasAsset", new[] { _atlasFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;

            if (importer != null)
            {
                var packingSettings = importer.packingSettings;
                packingSettings.padding = _padding;
                packingSettings.enableTightPacking = _tightPacking;
                packingSettings.enableRotation = _allowRotation;
                packingSettings.enableAlphaDilation = false;
                importer.packingSettings = packingSettings;
                importer.SaveAndReimport();
            }
        }

        Debug.Log($"Configured packing for {guids.Length} atlases");
        AssetDatabase.Refresh();
    }
}
