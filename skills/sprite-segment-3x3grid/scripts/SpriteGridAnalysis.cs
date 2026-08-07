// Code template for analyzing Sprite textures as a 3x3 color grid.
// Segments a Sprite into a 3x3 grid and outputs a text pattern based on
// color matching with the center cell.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpriteGridAnalysis
{
    public static class SpriteGridAnalysis
    {
        /// <summary>
        /// Analyzes a sprite and returns a 3x3 grid pattern based on color matching.
        /// </summary>
        /// <param name="sprite">The sprite to analyze</param>
        /// <param name="matchThreshold">Percentage of pixels that must match center color (0.5 = 50%, 0.75 = 75%, 0.9 = 90%)</param>
        /// <param name="colorTolerance">RGB tolerance for considering two colors as matching</param>
        public static string AnalyzeSpriteGrid(Sprite sprite, float matchThreshold = 0.5f, float colorTolerance = 0.04f)
        {
            var texture = sprite.texture;
            var rect = sprite.rect;

            int cellWidth = (int)(rect.width / 3);
            int cellHeight = (int)(rect.height / 3);

            // Get center cell bounds (row 1, col 1)
            int centerStartX = (int)rect.x + cellWidth;
            int centerStartY = (int)rect.y + cellHeight;
            Color centerColor = GetMajorityColor(texture, centerStartX, centerStartY, cellWidth, cellHeight);

            // Build output by checking each cell against center color
            var symbols = new string[9];
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int cellIndex = row * 3 + col;

                    if (cellIndex == 4)
                    {
                        symbols[cellIndex] = "*";
                        continue;
                    }

                    int startX = (int)rect.x + col * cellWidth;
                    int startY = (int)rect.y + (2 - row) * cellHeight; // Flip Y for top-to-bottom
                    float matchPercentage = GetColorMatchPercentage(texture, startX, startY, cellWidth, cellHeight, centerColor, colorTolerance);

                    symbols[cellIndex] = matchPercentage >= matchThreshold ? "." : "X";
                }
            }

            // Format: "A B C / D E F / G H I"
            return $"{symbols[0]} {symbols[1]} {symbols[2]} / {symbols[3]} {symbols[4]} {symbols[5]} / {symbols[6]} {symbols[7]} {symbols[8]}";
        }

        /// <summary>
        /// Gets the majority (most frequent) color in a cell region.
        /// </summary>
        static Color GetMajorityColor(Texture2D texture, int startX, int startY, int width, int height)
        {
            var colorCounts = new Dictionary<Color32, int>();

            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    Color32 pixel = texture.GetPixel(x, y);
                    if (!colorCounts.ContainsKey(pixel))
                        colorCounts[pixel] = 0;
                    colorCounts[pixel]++;
                }
            }

            return colorCounts.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        /// <summary>
        /// Calculates what percentage of pixels in a cell match the target color.
        /// </summary>
        static float GetColorMatchPercentage(Texture2D texture, int startX, int startY, int width, int height, Color targetColor, float colorTolerance)
        {
            int totalPixels = 0;
            int matchingPixels = 0;

            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    totalPixels++;
                    Color pixel = texture.GetPixel(x, y);
                    if (ColorsMatch(pixel, targetColor, colorTolerance))
                        matchingPixels++;
                }
            }

            return totalPixels > 0 ? (float)matchingPixels / totalPixels : 0f;
        }

        static bool ColorsMatch(Color a, Color b, float tolerance)
        {
            return Mathf.Abs(a.r - b.r) < tolerance &&
                   Mathf.Abs(a.g - b.g) < tolerance &&
                   Mathf.Abs(a.b - b.b) < tolerance;
        }
    }
}

// Usage Examples:
//
// // Default 50% threshold
// string pattern1 = SpriteGridAnalysis.AnalyzeSpriteGrid(mySprite);
//
// // Strict 90% threshold
// string pattern2 = SpriteGridAnalysis.AnalyzeSpriteGrid(mySprite, matchThreshold: 0.9f);
//
// // 75% threshold with tighter color tolerance
// string pattern3 = SpriteGridAnalysis.AnalyzeSpriteGrid(mySprite, matchThreshold: 0.75f, colorTolerance: 0.02f);
