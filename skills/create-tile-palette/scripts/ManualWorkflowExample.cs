using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Example: Manual two-step workflow for more control over the analysis step.
/// </summary>
public static class ManualWorkflowExample
{
    public static void RunManualWorkflow(List<Sprite> sprites, RuleTile myRuleTile,
        bool filterToKnownPatterns = true)
    {
        // Step 1: Run sprite-segment-3x3grid on each sprite, filter, and deduplicate
        var seenPatterns = new HashSet<string>();
        var uniquePatterns = new List<(string pattern, Sprite sprite)>();

        foreach (var sprite in sprites)
        {
            // Apply sprite-segment-3x3grid analysis
            string pattern = SpriteSegment3x3Grid.AnalyzeSpriteGrid(
                sprite,
                matchThreshold: 0.75f,
                colorTolerance: 0.04f
            );

            // Discard patterns not in the known set
            if (filterToKnownPatterns &&
                !TilemapRuleTileCreateFromSegment.IsKnownPattern(pattern))
            {
                Debug.Log($"Sprite '{sprite.name}' skipped (unknown pattern: {pattern})");
                continue;
            }

            // Skip duplicates
            if (seenPatterns.Contains(pattern))
            {
                Debug.Log($"Sprite '{sprite.name}' skipped (duplicate pattern: {pattern})");
                continue;
            }

            seenPatterns.Add(pattern);
            Debug.Log($"Sprite '{sprite.name}' pattern: {pattern}");
            uniquePatterns.Add((pattern, sprite));
        }

        // Step 2: Create TilingRules from unique patterns
        var rules = new List<(RuleTile.TilingRule rule, int specificity)>();
        foreach (var (pattern, sprite) in uniquePatterns)
        {
            var result = TilemapRuleTileCreateFromSegment.CreateTilingRuleFromPattern(pattern, sprite);
            rules.Add(result);
        }

        // Sort by specificity
        rules.Sort((a, b) => b.specificity.CompareTo(a.specificity));

        // Apply to RuleTile
        var sortedRules = rules.Select(r => r.rule).ToList();
        TilemapRuleTileCreateFromSegment.ApplyRulesToTile(myRuleTile, sortedRules);

        Debug.Log($"Created {sortedRules.Count} unique rules from {sprites.Count} sprites");
    }
}
