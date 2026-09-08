// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor.U2D;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Size-optimized sprite packer that places larger sprites first.
/// Uses a simple bin-packing approach to minimize atlas space usage.
/// </summary>
public class SizeOptimizedPacker : ScriptablePacker
{
    /// <summary>Maximum atlas size (width and height) in pixels</summary>
    public int atlasSize = 2048;

    /// <summary>Padding between sprites in pixels</summary>
    public int padding = 4;

    /// <summary>
    /// Packs sprites with larger ones first for better space utilization.
    /// Sorts sprites by area (width * height) and places them using first-fit bin packing.
    /// </summary>
    public override bool Pack(SpriteAtlasPackingSettings config,
                             SpriteAtlasTextureSettings setting,
                             PackerData input)
    {
        NativeArray<SpriteData> sprites = input.spriteData;
        NativeArray<TextureData> textures = input.textureData;

        // Create sorted list by area (largest first)
        var sortedIndices = new List<int>();
        for (int i = 0; i < sprites.Length; i++)
        {
            sortedIndices.Add(i);
        }

        sortedIndices = sortedIndices
            .OrderByDescending(i =>
            {
                var sprite = sprites[i];
                return sprite.rect.width * sprite.rect.height;
            })
            .ToList();

        // Simple bin packing
        int currentX = padding;
        int currentY = padding;
        int maxHeightInRow = 0;

        foreach (int idx in sortedIndices)
        {
            SpriteData sprite = sprites[idx];

            int spriteWidth = sprite.rect.width;
            int spriteHeight = sprite.rect.height;

            // Check if we need a new row
            if (currentX + spriteWidth + padding > atlasSize)
            {
                currentX = padding;
                currentY += maxHeightInRow + padding;
                maxHeightInRow = 0;
            }

            // Check if atlas is full
            if (currentY + spriteHeight + padding > atlasSize)
            {
                Debug.LogError("Atlas size exceeded. Increase atlas size or reduce sprite count.");
                return false;
            }

            // Set output
            sprite.output.x = currentX;
            sprite.output.y = currentY;
            sprite.output.page = 0;
            sprite.output.rot = PackTransform.None;

            // Update position for next sprite
            currentX += spriteWidth + padding;
            maxHeightInRow = Mathf.Max(maxHeightInRow, spriteHeight);

            sprites[idx] = sprite;
        }

        return true;
    }
}
