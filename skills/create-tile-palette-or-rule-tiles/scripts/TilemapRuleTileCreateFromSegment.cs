using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Creates RuleTile TilingRules by first applying sprite-segment-3x3grid analysis,
/// then converting the text patterns to neighbor configurations.
/// </summary>
public static class TilemapRuleTileCreateFromSegment
{
    /// <summary>
    /// Grid position to Unity neighbor Vector3Int mapping.
    /// Index 4 (center) is not included as it's not a neighbor.
    /// </summary>
    static readonly Dictionary<int, Vector3Int> k_GridIndexToPosition = new()
    {
        { 0, new Vector3Int(-1, 1, 0) },   // Top-Left
        { 1, new Vector3Int(0, 1, 0) },    // Top
        { 2, new Vector3Int(1, 1, 0) },    // Top-Right
        { 3, new Vector3Int(-1, 0, 0) },   // Left
        // 4 is center, skipped
        { 5, new Vector3Int(1, 0, 0) },    // Right
        { 6, new Vector3Int(-1, -1, 0) },  // Bottom-Left
        { 7, new Vector3Int(0, -1, 0) },   // Bottom
        { 8, new Vector3Int(1, -1, 0) },   // Bottom-Right
    };

    /// <summary>
    /// The 47 known tile patterns from the Common Tile Patterns table.
    /// Patterns not in this set are discarded by default.
    /// </summary>
    static readonly HashSet<string> k_KnownPatterns = new()
    {
        ". . . / . * . / . . .",  // Fully surrounded
        "X . . / . * . / . . .",  // Top left corner missing
        ". . X / . * . / . . .",  // Top right corner missing
        ". . . / . * . / X . .",  // Bottom left corner missing
        ". . . / . * . / . . X",  // Bottom right corner missing
        "X . X / . * . / . . .",  // Top corners missing
        ". . . / . * . / X . X",  // Bottom corners missing
        "X . . / . * . / X . .",  // Left corners missing
        ". . X / . * . / . . X",  // Right corners missing
        "X . . / . * . / . . X",  // Top Left and Bottom Right diagonal
        ". . X / . * . / X . .",  // Top Right and Bottom Left diagonal
        "X . X / . * . / X . .",  // Top Left, Top Right, Bottom Left
        "X . X / . * . / . . X",  // Top Left, Top Right, Bottom Right
        "X . . / . * . / X . X",  // Top Left, Bottom Left, Bottom Right
        ". . X / . * . / X . X",  // Top Right, Bottom Left, Bottom Right
        "X . X / . * . / X . X",  // Cross Section
        "X X X / . * . / . . .",  // Flat edge (Bottom)
        ". . . / . * . / X X X",  // Flat edge (Top)
        ". . X / . * X / . . X",  // Flat edge (Left)
        "X . . / X * . / X . .",  // Flat edge (Right)
        "X X X / . * . / . . X",  // L-Shape, Point Right
        ". . X / . * X / X . X",  // L-Shape, Point Bottom
        "X . . / . * . / X X X",  // L-Shape, Point Left
        "X . X / X * . / X . .",  // L-Shape, Point Top
        "X X X / . * . / X . .",  // L-Shape Inverse, Point Left
        "X . . / X * . / X . X",  // L-Shape Inverse, Point Bottom
        ". . X / . * . / X X X",  // L-Shape Inverse, Point Right
        "X . X / . * . / . . X",  // L-Shape Inverse, Point Top
        "X X X / . * . / X . X",  // T-Shape, face down
        "X . X / . * . / X X X",  // T-Shape, face top
        "X . X / . * X / X . X",  // T-Shape, face left
        "X . X / X * . / X . X",  // T-Shape, face right
        "X X X / . * . / X X X",  // Horizontal bridge
        "X . X / X * X / X . X",  // Vertical bridge
        "X X X / X * . / X . .",  // Corner piece, Bottom Right
        "X X X / . * X / . . X",  // Corner piece, Bottom Left
        ". . X / . * X / X X X",  // Corner piece, Top Left
        "X . . / X * . / X X X",  // Corner piece, Top Right
        "X X X / X * . / X . X",  // Edge end, Bottom Right
        "X X X / . * X / X . X",  // Edge end, Bottom Left
        "X . X / . * X / X X X",  // Edge end, Top Left
        "X . X / X * . / X X X",  // Edge end, Top Right
        "X X X / . * X / X X X",  // Single isolated tile, Left
        "X X X / X * . / X X X",  // Single isolated tile, Right
        "X . X / X * X / X X X",  // Single isolated tile, Top
        "X X X / X * X / X . X",  // Single isolated tile, Bottom
        "X X X / X * X / X X X",  // Center
    };

    /// <summary>
    /// Creates a RuleTile from sprites by first running sprite-segment-3x3grid analysis.
    /// This is the main entry point that implements the complete workflow.
    /// Duplicate patterns are automatically removed (first sprite wins).
    /// Patterns not in the known 47 Common Tile Patterns are discarded by default.
    /// </summary>
    /// <param name="sprites">Sprites to analyze and create rules for</param>
    /// <param name="matchThreshold">Match threshold for sprite-segment-3x3grid (0.5, 0.75, or 0.9)</param>
    /// <param name="colorTolerance">Color tolerance for sprite-segment-3x3grid</param>
    /// <param name="filterToKnownPatterns">If true (default), discard patterns not in Common Tile Patterns</param>
    /// <returns>Sorted list of unique TilingRules (most specific first)</returns>
    public static List<RuleTile.TilingRule> CreateRuleTileFromSprites(
        IEnumerable<Sprite> sprites,
        float matchThreshold = 0.75f,
        float colorTolerance = 0.04f,
        bool filterToKnownPatterns = true)
    {
        var rules = new List<(RuleTile.TilingRule rule, int specificity, string pattern)>();
        var seenPatterns = new HashSet<string>();

        foreach (var sprite in sprites)
        {
            // Step 1: Always run sprite-segment-3x3grid first
            string pattern = SpriteSegment3x3Grid.AnalyzeSpriteGrid(sprite, matchThreshold, colorTolerance);

            // Step 2: Discard patterns not in the known set
            if (filterToKnownPatterns && !k_KnownPatterns.Contains(pattern))
            {
                Debug.Log($"Sprite '{sprite.name}' skipped (unknown pattern: {pattern})");
                continue;
            }

            // Step 3: Skip duplicate patterns (keep first sprite only)
            if (seenPatterns.Contains(pattern))
                continue;

            seenPatterns.Add(pattern);

            // Step 4: Create TilingRule from the pattern
            var (rule, specificity) = CreateTilingRuleFromPattern(pattern, sprite);
            rules.Add((rule, specificity, pattern));
        }

        // Sort by specificity (descending) - more 'This' rules first
        rules.Sort((a, b) => b.specificity.CompareTo(a.specificity));

        return rules.Select(r => r.rule).ToList();
    }

    /// <summary>
    /// Returns true if the pattern is one of the 47 known Common Tile Patterns.
    /// </summary>
    public static bool IsKnownPattern(string pattern) => k_KnownPatterns.Contains(pattern);

    /// <summary>
    /// Creates a single TilingRule from a sprite-segment-3x3grid output pattern.
    /// </summary>
    /// <param name="pattern">Pattern string from sprite-segment-3x3grid, e.g., "X X X / X * X / . . ."</param>
    /// <param name="sprite">Sprite to assign to this rule</param>
    /// <returns>Tuple of (TilingRule, specificity count)</returns>
    public static (RuleTile.TilingRule rule, int specificity) CreateTilingRuleFromPattern(string pattern, Sprite sprite)
    {
        var cells = ParsePattern(pattern);
        var neighbors = new List<int>();
        var neighborPositions = new List<Vector3Int>();
        int specificity = 0;

        for (int i = 0; i < 9; i++)
        {
            // Skip center cell
            if (i == 4)
                continue;

            string cell = cells[i];

            // Map '.' to 'This' rule
            // Map 'X' to DontCare (don't add position)
            if (cell == ".")
            {
                neighbors.Add(RuleTile.TilingRuleOutput.Neighbor.This);
                neighborPositions.Add(k_GridIndexToPosition[i]);
                specificity++;
            }
            // 'X' is implicitly DontCare - position not added
        }

        var rule = new RuleTile.TilingRule
        {
            m_Neighbors = neighbors,
            m_NeighborPositions = neighborPositions,
            m_Sprites = new[] { sprite },
            m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
            m_ColliderType = Tile.ColliderType.Sprite,
            m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
        };

        return (rule, specificity);
    }

    /// <summary>
    /// Parses a sprite-segment-3x3grid output pattern into an array of 9 cell values.
    /// </summary>
    /// <param name="pattern">Pattern string, e.g., "X X X / X * X / . . ."</param>
    /// <returns>Array of 9 cell strings in order: top-left to bottom-right</returns>
    public static string[] ParsePattern(string pattern)
    {
        var rows = pattern.Split(new[] { " / " }, StringSplitOptions.None);
        if (rows.Length != 3)
            throw new ArgumentException($"Pattern must have 3 rows, got {rows.Length}: {pattern}");

        var cells = new string[9];
        for (int row = 0; row < 3; row++)
        {
            var rowCells = rows[row].Split(' ');
            if (rowCells.Length != 3)
                throw new ArgumentException($"Row {row} must have 3 cells, got {rowCells.Length}: {rows[row]}");

            for (int col = 0; col < 3; col++)
            {
                cells[row * 3 + col] = rowCells[col];
            }
        }

        return cells;
    }

    /// <summary>
    /// Applies generated rules to a RuleTile asset.
    /// Sets the default sprite to the last rule's sprite (least specific / bottom-most match).
    /// </summary>
    /// <param name="ruleTile">Target RuleTile to configure</param>
    /// <param name="rules">Sorted list of TilingRules to apply</param>
    public static void ApplyRulesToTile(RuleTile ruleTile, List<RuleTile.TilingRule> rules)
    {
        ruleTile.m_TilingRules.Clear();
        ruleTile.m_TilingRules.AddRange(rules);

        // Use the bottom-most (least specific) rule's sprite as the default
        if (rules.Count > 0 && rules[rules.Count - 1].m_Sprites.Length > 0)
            ruleTile.m_DefaultSprite = rules[rules.Count - 1].m_Sprites[0];
    }
}
