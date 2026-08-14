using System;
using UnityEditor.U2D.Sprites;

namespace Editor
{
    static public partial class SpriteEditorUtility
    {
        /// <summary>
        /// Automatically slices a texture by detecting visible pixel regions and creating sprite rectangles.
        /// Uses Unity's internal automatic sprite detection algorithm to find sprite boundaries.
        /// </summary>
        /// <param name="spriteDataProvider">The sprite data provider for the texture.</param>
        /// <param name="textureProvider">The texture data provider.</param>
        /// <param name="minRectSize">Minimum size in pixels for detected sprite rectangles.</param>
        /// <param name="extrudeSize">Number of pixels to extrude (expand) sprite boundaries.</param>
        /// <param name="addNewSpriteMethod">Method for handling existing sprites (DeleteAll, Smart, Safe).</param>
        /// <param name="nameGenerator">Function to generate sprite names based on index.</param>
        /// <param name="kOverlapTolerance">Tolerance for detecting overlapping sprites.</param>
        /// <param name="kBestFitTolerance">Tolerance for best-fit matching.</param>
        /// <param name="bestFit">Whether to use best-fit algorithm for overlap detection.</param>
        /// <returns>True if slicing succeeded, false if texture is not readable.</returns>
        static public bool AutomaticSliceTexture(ISpriteEditorDataProvider spriteDataProvider, ITextureDataProvider textureProvider,
            int minRectSize, int extrudeSize, AddNewSpriteMethod addNewSpriteMethod, Func<int, string> nameGenerator,
            float kOverlapTolerance= 0.00001f, float kBestFitTolerance = 0.5f, bool bestFit = false)
        {
            var texture = GetTextureToSlice(textureProvider);
            if (texture == null)
            {
                return false;
            }

            var rects = UnityEditorInternal.InternalSpriteUtility.GenerateAutomaticSpriteRectangles(texture, minRectSize, extrudeSize);

            var newRects = GenerateNewSpriteRects(spriteDataProvider, rects, addNewSpriteMethod, nameGenerator, kOverlapTolerance, kBestFitTolerance, bestFit);
            spriteDataProvider.SetSpriteRects(newRects.ToArray());

            return true;
        }
    }
}
