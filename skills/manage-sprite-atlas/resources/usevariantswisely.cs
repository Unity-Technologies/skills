// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Demonstrates using SpriteAtlas variants wisely.
/// Variants allow different atlases for different platforms or resolutions.
/// </summary>
public static class UseVariantsWiselyExample
{
    /// <summary>
    /// Creates a variant atlas from a master atlas.
    /// Variants inherit settings from the master but can override texture formats.
    /// </summary>
    public static void CreateVariantAtlas(string masterPath, string variantPath)
    {
        // Load the master atlas (runtime type)
        SpriteAtlas masterAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(masterPath);

        if (masterAtlas == null)
        {
            Debug.LogError($"Could not load master atlas at {masterPath}");
            return;
        }

        // Create variant asset
        SpriteAtlasAsset variantAsset = new SpriteAtlasAsset();
        variantAsset.SetMasterAtlas(masterAtlas); // Link to master

        // Add sprites from master (optional - variants can use same packables)
        Object[] masterPackables = masterAtlas.GetPackables();
        variantAsset.Add(masterPackables);

        // Save variant
        SpriteAtlasAsset.Save(variantAsset, variantPath);
        AssetDatabase.ImportAsset(variantPath);

        Debug.Log($"Created variant atlas at {variantPath}");
    }

    /// <summary>
    /// Configures platform-specific variants for different quality levels.
    /// High-quality variant for desktop, low-quality for mobile.
    /// </summary>
    public static void CreateQualityVariants(string masterPath)
    {
        // Create high-quality variant
        string highVariantPath = masterPath.Replace(".spriteatlasv2", "_HQ.spriteatlasv2");
        ConfigureVariantForPlatform(highVariantPath, "Standalone", 2048, TextureImporterFormat.ASTC_6x6);

        // Create low-quality variant for mobile
        string lowVariantPath = masterPath.Replace(".spriteatlasv2", "_LQ.spriteatlasv2");
        ConfigureVariantForPlatform(lowVariantPath, "Android", 1024, TextureImporterFormat.ASTC_6x6);
    }

    /// <summary>
    /// Configures a variant for a specific platform with given settings.
    /// </summary>
    private static void ConfigureVariantForPlatform(string variantPath, string platformName, int maxTextureSize, TextureImporterFormat format)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(variantPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        // Configure platform-specific settings
        importer.SetPlatformSettings(new TextureImporterPlatformSettings
        {
            name = platformName,
            maxTextureSize = maxTextureSize,
            format = format,
            compressionQuality = 50
        });

        importer.SaveAndReimport();
    }

    /// <summary>
    /// Creates resolution variants for different screen sizes.
    /// Example: 1x, 2x, and 3x variants for different DPI support.
    /// </summary>
    public static void CreateResolutionVariants(string masterPath)
    {
        // Create 2x variant
        string x2Path = masterPath.Replace(".spriteatlasv2", "_2x.spriteatlasv2");
        SpriteAtlasImporter importerX2 = CreateVariantImporter(x2Path);
        if (importerX2 != null)
        {
            importerX2.variantScale = 2.0f;
            importerX2.SaveAndReimport();
        }

        // Create 3x variant
        string x3Path = masterPath.Replace(".spriteatlasv2", "_3x.spriteatlasv2");
        SpriteAtlasImporter importerX3 = CreateVariantImporter(x3Path);
        if (importerX3 != null)
        {
            importerX3.variantScale = 3.0f;
            importerX3.SaveAndReimport();
        }
    }

    /// <summary>
    /// Creates a variant importer for a new variant atlas.
    /// </summary>
    private static SpriteAtlasImporter CreateVariantImporter(string path)
    {
        // Check if file exists
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning($"Variant file does not exist: {path}");
            return null;
        }

        return AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
    }

    /// <summary>
    /// Gets the appropriate variant for a given platform.
    /// Use this at runtime to load the correct variant.
    /// </summary>
    public static string GetVariantForPlatform(string masterPath, BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneOSX:
            case BuildTarget.StandaloneLinux64:
                // Desktop - use high quality
                return masterPath.Replace(".spriteatlasv2", "_HQ.spriteatlasv2");
            case BuildTarget.Android:
            case BuildTarget.iOS:
            case BuildTarget.WebGL:
                // Mobile - use optimized
                return masterPath.Replace(".spriteatlasv2", "_LQ.spriteatlasv2");
            default:
                return masterPath; // Use master for unknown platforms
        }
    }

    /// <summary>
    /// Cleans up unused variant atlases.
    /// Remove variants that are no longer needed.
    /// </summary>
    public static void CleanupUnusedVariants(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlasAsset", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Check if this is a variant (has quality suffix)
            if (path.Contains("_HQ") || path.Contains("_LQ") || path.Contains("_2x") || path.Contains("_3x"))
            {
                Debug.Log($"Checking variant: {path}");
                // Add cleanup logic here as needed
            }
        }
    }
}

/// <summary>
/// Component that loads the appropriate atlas variant at runtime.
/// </summary>
public class VariantAtlasLoader : MonoBehaviour
{
    [SerializeField] private string _masterAtlasPath = "Atlases/MyAtlas.spriteatlasv2";

    private void Start()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string variantPath = UseVariantsWiselyExample.GetVariantForPlatform(_masterAtlasPath, target);

        Debug.Log($"Loading variant for {target}: {variantPath}");
        // Load the appropriate variant...
    }
}
