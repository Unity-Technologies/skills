using System.Linq;
using UnityEditor;
using UnityEngine;

public class RuleTileGenerator : EditorWindow
{
    float m_MatchThreshold = 0.75f;

    [MenuItem("Tools/Generate RuleTile from Sprites")]
    static void ShowWindow()
    {
        GetWindow<RuleTileGenerator>("RuleTile Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Match Threshold", EditorStyles.boldLabel);
        m_MatchThreshold = EditorGUILayout.Slider(m_MatchThreshold, 0.5f, 0.9f);

        if (GUILayout.Button("Generate from Selected Texture"))
            GenerateRuleTile();
    }

    void GenerateRuleTile()
    {
        var texture = Selection.activeObject as Texture2D;
        if (texture == null)
        {
            Debug.LogError("Please select a Texture2D asset");
            return;
        }

        // Get sprites from texture
        string path = AssetDatabase.GetAssetPath(texture);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();

        if (sprites.Count == 0)
        {
            Debug.LogError("No sprites found in texture. Ensure texture is sliced.");
            return;
        }

        // Complete workflow: sprite-segment-3x3grid -> TilingRules
        var rules = TilemapRuleTileCreateFromSegment.CreateRuleTileFromSprites(
            sprites,
            matchThreshold: m_MatchThreshold
        );

        // Create and save RuleTile asset
        var ruleTile = ScriptableObject.CreateInstance<RuleTile>();
        TilemapRuleTileCreateFromSegment.ApplyRulesToTile(ruleTile, rules);

        string savePath = path.Replace(".png", "_RuleTile.asset")
                              .Replace(".jpg", "_RuleTile.asset");
        AssetDatabase.CreateAsset(ruleTile, savePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Created RuleTile with {rules.Count} rules at {savePath}");
        Selection.activeObject = ruleTile;
    }
}
