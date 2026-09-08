// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// Demonstrates setting includeInBuild for SpriteAtlases.
/// Controls whether atlases are included in the build or loaded externally.
/// </summary>
public static class SetIncludeInBuildExample
{
    /// <summary>
    /// Includes an atlas in the build (built-in data).
    /// Atlases with includeInBuild=true are embedded in the game build.
    /// </summary>
    public static void IncludeInBuild(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        importer.includeInBuild = true;
        importer.SaveAndReimport();

        Debug.Log($"Included {atlasPath} in build");
    }

    /// <summary>
    /// Excludes an atlas from the build (for Addressables or AssetBundles).
    /// Atlases with includeInBuild=false are not embedded and must be loaded separately.
    /// </summary>
    public static void ExcludeFromBuild(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        importer.includeInBuild = false;
        importer.SaveAndReimport();

        Debug.Log($"Excluded {atlasPath} from build");
    }

    /// <summary>
    /// Configures all atlases in a folder for built-in delivery.
    /// Use this when you want all atlases included in the game build.
    /// </summary>
    public static void IncludeAllInBuild(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlasAsset", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;

            if (importer != null)
            {
                importer.includeInBuild = true;
                importer.SaveAndReimport();
            }
        }

        Debug.Log($"Configured {guids.Length} atlases for built-in delivery");
    }

    /// <summary>
    /// Configures all atlases in a folder for Addressables delivery.
    /// Use this when using Addressables for late-binding atlas loading.
    /// </summary>
    public static void ExcludeAllForAddressables(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlasAsset", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;

            if (importer != null)
            {
                importer.includeInBuild = false; // Exclude from build for Addressables
                importer.SaveAndReimport();
            }
        }

        Debug.Log($"Configured {guids.Length} atlases for Addressables delivery");
    }

    /// <summary>
    /// Checks if an atlas is configured to be included in the build.
    /// </summary>
    public static bool IsIncludedInBuild(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return false;

        return importer.includeInBuild;
    }

    /// <summary>
    /// Toggles the includeInBuild setting for an atlas.
    /// </summary>
    public static void ToggleIncludeInBuild(string atlasPath)
    {
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(atlasPath) as SpriteAtlasImporter;
        if (importer == null)
            return;

        importer.includeInBuild = !importer.includeInBuild;
        importer.SaveAndReimport();

        Debug.Log($"Set {atlasPath} includeInBuild to {importer.includeInBuild}");
    }
}

/// <summary>
/// Editor window for batch configuring includeInBuild settings.
/// </summary>
public class IncludeInBuildWindow : EditorWindow
{
    private string _atlasFolder = "Assets/Atlases";
    private bool _includeInBuild = true;

    [MenuItem("Tools/Configure Include In Build")]
    public static void ShowWindow()
    {
        GetWindow<IncludeInBuildWindow>("Include In Build");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Batch Configure Include In Build", EditorStyles.boldLabel);

        _atlasFolder = EditorGUILayout.TextField("Atlas Folder", _atlasFolder);
        _includeInBuild = EditorGUILayout.Toggle("Include in Build", _includeInBuild);

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
                importer.includeInBuild = _includeInBuild;
                importer.SaveAndReimport();
            }
        }

        Debug.Log($"Configured {guids.Length} atlases (includeInBuild={_includeInBuild})");
        AssetDatabase.Refresh();
    }
}
