// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// Demonstrates using correct platform names for SpriteAtlas configuration.
/// Platform names must match Unity's BuildTarget enum values.
/// </summary>
public static class PlatformNamesExample
{
    /// <summary>
    /// Gets the correct platform name string for a given BuildTarget.
    /// </summary>
    public static string GetPlatformName(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "Standalone";
            case BuildTarget.StandaloneOSX:
                return "Standalone";
            case BuildTarget.StandaloneLinux64:
                return "Standalone";
            case BuildTarget.WebGL:
                return "WebGL";
            case BuildTarget.WSAPlayer:
                return "WSA";
            case BuildTarget.PS4:
                return "PS4";
            case BuildTarget.XboxOne:
                return "XboxOne";
            case BuildTarget.tvOS:
                return "tvOS";
            default:
                Debug.LogWarning($"Unknown BuildTarget: {target}. Using 'Standalone'.");
                return "Standalone";
        }
    }

    /// <summary>
    /// Configures platform settings for multiple platforms using correct names.
    /// </summary>
    public static void ConfigurePlatformSettings(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

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

        // WebGL
        importer.SetPlatformSettings(new TextureImporterPlatformSettings
        {
            name = "WebGL",
            maxTextureSize = 1024,
            format = TextureImporterFormat.DXT5Crunched,
            compressionQuality = 100
        });

        // Save and reimport
        importer.SaveAndReimport();
    }

    /// <summary>
    /// Gets all supported platform names.
    /// </summary>
    public static string[] GetAllPlatformNames()
    {
        return new string[]
        {
            "Android",
            "iOS",
            "Standalone",
            "WebGL",
            "WSA",
            "PS4",
            "XboxOne",
            "tvOS"
        };
    }

    /// <summary>
    /// Checks if a platform name is valid.
    /// </summary>
    public static bool IsValidPlatformName(string platformName)
    {
        string[] validPlatforms = GetAllPlatformNames();
        for (int i = 0; i < validPlatforms.Length; i++)
        {
            if (validPlatforms[i] == platformName)
                return true;
        }
        return false;
    }

}
