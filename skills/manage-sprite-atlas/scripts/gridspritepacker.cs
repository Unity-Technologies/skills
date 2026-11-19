// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor.U2D;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// Simple grid-based sprite packer that arranges sprites in a uniform grid pattern.
/// Sprites are placed row by row from left to right, top to bottom.
/// </summary>
public class GridSpritePacker : ScriptablePacker
{
    /// <summary>Number of columns in the grid</summary>
    public int columns = 4;

    /// <summary>Size of each cell in pixels (assumed square)</summary>
    public int cellSize = 256;

    /// <summary>Padding between sprites in pixels</summary>
    public int padding = 2;

    /// <summary>
    /// Packs sprites in a grid pattern.
    /// Sprite at index i is placed at column (i % columns) and row (i / columns).
    /// </summary>
    public override bool Pack(SpriteAtlasPackingSettings config,
                             SpriteAtlasTextureSettings setting,
                             PackerData input)
    {
        NativeArray<SpriteData> sprites = input.spriteData;

        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteData sprite = sprites[i];

            // Calculate grid position
            int col = i % columns;
            int row = i / columns;

            // Set output position with padding
            sprite.output.x = col * (cellSize + padding) + padding;
            sprite.output.y = row * (cellSize + padding) + padding;
            sprite.output.page = 0; // Single page
            sprite.output.rot = PackTransform.None;

            sprites[i] = sprite;
        }

        return true;
    }
}
