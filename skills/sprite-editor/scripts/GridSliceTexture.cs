using System;
using UnityEngine;
using UnityEditor.U2D.Sprites;

namespace Editor
{
    static public partial class SpriteEditorUtility
    {
        /// <summary>
        /// Slices a texture into a regular grid of sprite rectangles based on specified cell size, offset, and padding.
        /// Optionally keeps or discards empty rectangles based on pixel alpha values.
        /// </summary>
        /// <param name="spriteDataProvider">The sprite data provider for the texture.</param>
        /// <param name="textureProvider">The texture data provider.</param>
        /// <param name="offset">The offset from the top-left corner to start the grid.</param>
        /// <param name="size">The size of each grid cell (width x height).</param>
        /// <param name="padding">The padding between grid cells.</param>
        /// <param name="addNewSpriteMethod">Method for handling existing sprites (DeleteAll, Smart, Safe).</param>
        /// <param name="nameGenerator">Function to generate sprite names based on index.</param>
        /// <param name="keepEmptyRects">Whether to keep sprites with no visible pixels.</param>
        /// <param name="kOverlapTolerance">Tolerance for detecting overlapping sprites.</param>
        /// <param name="kBestFitTolerance">Tolerance for best-fit matching.</param>
        /// <param name="bestFit">Whether to use best-fit algorithm for overlap detection.</param>
        /// <returns>True if slicing succeeded, false if texture is not readable.</returns>
        static public bool GridSliceTexture(ISpriteEditorDataProvider spriteDataProvider, ITextureDataProvider textureProvider, Vector2 offset, Vector2 size, Vector2 padding,
            AddNewSpriteMethod addNewSpriteMethod, Func<int, string> nameGenerator,
            bool keepEmptyRects =false, float kOverlapTolerance= 0.00001f, float kBestFitTolerance = 0.5f, bool bestFit = false)
        {
            var textureToUse = GetTextureToSlice(textureProvider);
            if (textureToUse == null)
            {
                return false;
            }
            var rects = UnityEditorInternal.InternalSpriteUtility.GenerateGridSpriteRectangles(textureToUse, offset, size, padding, keepEmptyRects);
            var newRects = GenerateNewSpriteRects(spriteDataProvider, rects, addNewSpriteMethod, nameGenerator, kOverlapTolerance, kBestFitTolerance, bestFit);
            spriteDataProvider.SetSpriteRects(newRects.ToArray());
            return true;
        }
    }
}
