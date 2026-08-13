using System;
using System.Collections.Generic;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Editor
{
    static public class IsometricSliceUtility
    {
        /// <summary>
        /// Slices a texture into isometric tiles based on specified size and offset.
        /// Creates sprite rectangles in an isometric diamond pattern and optionally sets diamond-shaped outlines.
        /// </summary>
        /// <param name="spriteDataProvider">The sprite data provider for the texture.</param>
        /// <param name="textureProvider">The texture data provider.</param>
        /// <param name="size">The size of each isometric tile (width x height).</param>
        /// <param name="offset">The offset from the top-left corner to start slicing.</param>
        /// <param name="alignment">The alignment value for sprites.</param>
        /// <param name="pivot">The pivot point for sprites.</param>
        /// <param name="slicingMethod">Method for handling existing sprites (DeleteAll, Smart, Safe).</param>
        /// <param name="nameGenerator">Function to generate sprite names based on index.</param>
        /// <param name="kOverlapTolerance">Tolerance for detecting overlapping sprites.</param>
        /// <param name="kBestFitTolerance">Tolerance for best-fit matching.</param>
        /// <param name="bestFit">Whether to use best-fit algorithm for overlap detection.</param>
        /// <param name="keepEmptyRects">Whether to keep sprites with no visible pixels.</param>
        /// <param name="isAlternate">Whether to start with alternating row offset.</param>
        /// <returns>True if slicing succeeded, false if texture is not readable.</returns>
        static public bool IsometricSliceTexture(ISpriteEditorDataProvider spriteDataProvider, ITextureDataProvider textureProvider,
            Vector2 size, Vector2 offset, int alignment, Vector2 pivot, SpriteEditorUtility.AddNewSpriteMethod slicingMethod, Func<int, string> nameGenerator,
            float kOverlapTolerance = 0.00001f, float kBestFitTolerance = 0.5f, bool bestFit = false, bool keepEmptyRects = false, bool isAlternate = false)
        {
            var texture = SpriteEditorUtility.GetTextureToSlice(textureProvider);
            if (texture == null)
            {
                return false;
            }
            var rects = GetIsometricRects(texture, size, offset, isAlternate, keepEmptyRects);
            var newRects = SpriteEditorUtility.GenerateNewSpriteRects(spriteDataProvider, rects, slicingMethod, nameGenerator, kOverlapTolerance, kBestFitTolerance, bestFit);
            spriteDataProvider.SetSpriteRects(newRects.ToArray());

            // Set diamond-shaped outlines for isometric sprites
            var outlineDataProvider = spriteDataProvider.GetDataProvider<ISpriteOutlineDataProvider>();
            if (outlineDataProvider != null)
            {
                List<Vector2[]> outlines = new List<Vector2[]>(4);
                outlines.Add(new[] {
                    new Vector2(0.0f, -size.y / 2),
                    new Vector2(size.x / 2, 0.0f),
                    new Vector2(0.0f, size.y / 2),
                    new Vector2(-size.x / 2, 0.0f)
                });
                foreach (var rect in newRects)
                {
                    outlineDataProvider.SetOutlines(rect.spriteID, outlines);
                }
            }

            return true;
        }

        private static bool PixelHasAlpha(int x, int y, int width, bool[] alphaPixelCache)
        {
            var index = y * width + x;
            return alphaPixelCache[index];
        }

        /// <summary>
        /// Generates rectangles for isometric tile slicing by walking the texture in an isometric pattern.
        /// Optionally filters out empty rectangles based on alpha pixel density.
        /// </summary>
        public static IEnumerable<Rect> GetIsometricRects(Texture2D textureToUse, Vector2 size, Vector2 offset, bool isAlternate, bool keepEmptyRects)
        {
            var alphaPixelCache = new bool[textureToUse.width * textureToUse.height];
            Color32[] pixels = textureToUse.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
                alphaPixelCache[i] = pixels[i].a != 0;

            var gradient = (size.x / 2) / (size.y / 2);
            bool isAlt = isAlternate;
            float x = offset.x;
            if (isAlt)
                x += size.x / 2;
            float y = textureToUse.height - offset.y;

            while (y - size.y >= 0)
            {
                while (x + size.x <= textureToUse.width)
                {
                    var rect = new Rect(x, y - size.y, size.x, size.y);
                    if (!keepEmptyRects)
                    {
                        // Check if the isometric diamond area has sufficient alpha pixels
                        int sx = (int)rect.x;
                        int sy = (int)rect.y;
                        int width = (int)size.x;
                        int odd = ((int)size.y) % 2;
                        int topY = ((int)size.y / 2) - 1;
                        int bottomY = topY + odd;
                        int totalPixels = 0;
                        int alphaPixels = 0;

                        // Sample pixels in diamond shape
                        for (int ry = 0; ry <= topY; ry++)
                        {
                            var pixelOffset = Mathf.CeilToInt(gradient * ry);
                            for (int rx = pixelOffset; rx < width - pixelOffset; ++rx)
                            {
                                if (PixelHasAlpha(sx + rx, sy + topY - ry, textureToUse.width, alphaPixelCache))
                                    alphaPixels++;
                                if (PixelHasAlpha(sx + rx, sy + bottomY + ry, textureToUse.width, alphaPixelCache))
                                    alphaPixels++;
                                totalPixels += 2;
                            }
                        }

                        if (odd > 0)
                        {
                            int ry = topY + 1;
                            for (int rx = 0; rx < size.x; ++rx)
                            {
                                if (PixelHasAlpha(sx + rx, sy + ry, textureToUse.width, alphaPixelCache))
                                    alphaPixels++;
                                totalPixels++;
                            }
                        }
                        if (totalPixels > 0 && ((float)alphaPixels) / totalPixels > 0.01f)
                            yield return rect;
                    }
                    else
                        yield return rect;
                    x += size.x;
                }
                isAlt = !isAlt;
                x = offset.x;
                if (isAlt)
                    x += size.x / 2;
                y -= size.y / 2;
            }
        }
    }
}
