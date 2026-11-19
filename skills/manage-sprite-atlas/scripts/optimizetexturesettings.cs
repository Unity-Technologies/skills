// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// Demonstrates optimizing texture settings for SpriteAtlases.
/// Proper texture settings are critical for performance and memory usage.
/// </summary>
public static class OptimizeTextureSettingsExample
{
    /// <summary>
    /// Configures optimal texture settings for UI atlases.
    /// UI sprites typically need high quality and alpha channel.
    /// </summary>
    public static void ConfigureUITextureSettings(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        // UI textures need good quality with alpha
        var textureSettings = importer.textureSettings;
        textureSettings.filterMode = FilterMode.Bilinear;
        importer.textureSettings = textureSettings;

        // Set appropriate format for UI (with alpha)
        var platformSettings = importer.GetPlatformSettings("DefaultTexturePlatform");
        platformSettings.maxTextureSize = 2048;
        platformSettings.format = TextureImporterFormat.ASTC_6x6;
        platformSettings.compressionQuality = 50;
        importer.SetPlatformSettings(platformSettings);

        importer.SaveAndReimport();
    }

    /// <summary>
    /// Configures optimal texture settings for game object atlases.
    /// Game objects can use lower quality and don't always need alpha.
    /// </summary>
    public static void ConfigureGameObjectTextureSettings(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        // Game object textures can use lower quality
        var textureSettings = importer.textureSettings;
        textureSettings.filterMode = FilterMode.Trilinear;
        importer.textureSettings = textureSettings;

        // Use DXT5 for desktop, ASTC for mobile
        var platformSettings = importer.GetPlatformSettings("DefaultTexturePlatform");
        platformSettings.maxTextureSize = 1024;
        platformSettings.format = TextureImporterFormat.DXT5;
        platformSettings.compressionQuality = 50;
        importer.SetPlatformSettings(platformSettings);

        importer.SaveAndReimport();
    }

    /// <summary>
    /// Configures texture settings for a specific platform.
    /// </summary>
    public static void ConfigurePlatformTextureSettings(string atlasPath, string platformName, int maxTextureSize, TextureImporterFormat format)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        importer.SetPlatformSettings(new TextureImporterPlatformSettings
        {
            name = "DefaultTexturePlatform",
            maxTextureSize = maxTextureSize,
            format = format,
            compressionQuality = 50
        });

        importer.SaveAndReimport();
    }

    /// <summary>
    /// Optimizes texture settings for mobile platforms.
    /// Focuses on reducing memory usage and improving loading times.
    /// </summary>
    public static void OptimizeForMobile(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        // Android settings
        importer.SetPlatformSettings(new TextureImporterPlatformSettings
        {
            name = "Android",
            maxTextureSize = 1024,
            format = TextureImporterFormat.ETC2_RGB4,
            compressionQuality = 100,
        });

        // iOS settings
        importer.SetPlatformSettings(new TextureImporterPlatformSettings
        {
            name = "iOS",
            maxTextureSize = 1024,
            format = TextureImporterFormat.ASTC_6x6,
            compressionQuality = 100
        });

        importer.SaveAndReimport();
    }

    /// <summary>
    /// Optimizes texture settings for web deployment.
    /// Focuses on smaller file sizes and faster loading.
    /// </summary>
    public static void OptimizeForWeb(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        importer.SetPlatformSettings(new TextureImporterPlatformSettings
        {
            name = "WebGL",
            maxTextureSize = 1024,
            format = TextureImporterFormat.DXT5Crunched,
            compressionQuality = 100
        });

        importer.SaveAndReimport();
    }
}

/// <summary>
/// Editor window for batch optimizing texture settings across multiple atlases.
/// </summary>
public class OptimizeTextureSettingsWindow : EditorWindow
{
    private string _atlasFolder = "Assets/Atlases";
    private bool _optimizeForMobile = false;
    private bool _optimizeForWeb = false;

    [MenuItem("Tools/Optimize Atlas Texture Settings")]
    public static void ShowWindow()
    {
        GetWindow<OptimizeTextureSettingsWindow>("Optimize Atlases");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Batch Optimize Texture Settings", EditorStyles.boldLabel);

        _atlasFolder = EditorGUILayout.TextField("Atlas Folder", _atlasFolder);
        _optimizeForMobile = EditorGUILayout.Toggle("Optimize for Mobile", _optimizeForMobile);
        _optimizeForWeb = EditorGUILayout.Toggle("Optimize for Web", _optimizeForWeb);

        if (GUILayout.Button("Optimize Atlases"))
        {
            BatchOptimize();
        }
    }

    private void BatchOptimize()
    {
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlasAsset", new[] { _atlasFolder });

        int processed = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;

            if (importer != null)
            {
                if (_optimizeForMobile)
                {
                    OptimizeTextureSettingsExample.OptimizeForMobile(path);
                }
                else if (_optimizeForWeb)
                {
                    OptimizeTextureSettingsExample.OptimizeForWeb(path);
                }
                processed++;
            }
        }

        Debug.Log($"Optimized {processed} atlases");
        AssetDatabase.Refresh();
    }
}
