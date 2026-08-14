using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Editor
{
    static public partial class SpriteEditorUtility
    {
        /// <summary>
        /// Sets a custom pivot point for a specific sprite within a sprite editor data provider.
        /// The pivot is specified as a normalized Vector2 where (0,0) is bottom-left and (1,1) is top-right.
        /// </summary>
        /// <param name="dataProvider">The sprite editor data provider containing the sprite data.</param>
        /// <param name="sprite">The GUID of the sprite to modify.</param>
        /// <param name="pivot">The normalized pivot position (0-1 range for both x and y).</param>
        /// <returns>True if the sprite was found and the pivot was set successfully; otherwise, false.</returns>
        public static bool SetCustomPivot(ISpriteEditorDataProvider dataProvider, GUID sprite, Vector2 pivot)
        {
            var rects = dataProvider.GetSpriteRects();
            for (int i = 0; i < rects.Length; ++i)
            {
                if (rects[i].spriteID == sprite)
                {
                    rects[i].pivot = pivot;
                    rects[i].alignment = SpriteAlignment.Custom;
                    dataProvider.SetSpriteRects(rects);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets a predefined pivot alignment for a specific sprite within a sprite editor data provider.
        /// Uses Unity's built-in alignment options (e.g., Center, TopLeft, BottomRight).
        /// </summary>
        /// <param name="dataProvider">The sprite editor data provider containing the sprite data.</param>
        /// <param name="sprite">The GUID of the sprite to modify.</param>
        /// <param name="alignment">The predefined sprite alignment to apply.</param>
        /// <returns>True if the sprite was found and the alignment was set successfully; otherwise, false.</returns>
        public static bool SetPivot(ISpriteEditorDataProvider dataProvider, GUID sprite, SpriteAlignment alignment)
        {
            var rects = dataProvider.GetSpriteRects();
            for (int i = 0; i < rects.Length; ++i)
            {
                if (rects[i].spriteID == sprite)
                {
                    rects[i].alignment = alignment;
                    dataProvider.SetSpriteRects(rects);
                    return true;
                }
            }

            return false;
        }
    }
}