// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor.U2D;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// Distributes sprites across multiple atlas pages with automatic page management.
/// Handles both row wrapping within a page and page overflow.
/// </summary>
public class MultiPagePacker : ScriptablePacker
{
    /// <summary>Maximum number of sprites per page</summary>
    public int maxSpritesPerPage = 64;

    /// <summary>Width of each atlas page in pixels</summary>
    public int pageWidth = 2048;

    /// <summary>Padding between sprites in pixels</summary>
    public int padding = 4;

    /// <summary>
    /// Packs sprites across multiple pages.
    /// Automatically creates new pages when the current page is full or a row wraps.
    /// </summary>
    public override bool Pack(SpriteAtlasPackingSettings config,
                             SpriteAtlasTextureSettings setting,
                             PackerData input)
    {
        NativeArray<SpriteData> sprites = input.spriteData;
        NativeArray<TextureData> textures = input.textureData;

        int currentPage = 0;
        int spritesInPage = 0;
        int currentX = padding;
        int currentY = padding;
        int maxHeightInRow = 0;

        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteData sprite = sprites[i];
            TextureData texture = textures[sprite.texIndex];

            int spriteWidth = sprite.rect.width;
            int spriteHeight = sprite.rect.height;

            // Check if we need a new page
            if (spritesInPage >= maxSpritesPerPage)
            {
                currentPage++;
                spritesInPage = 0;
                currentX = padding;
                currentY = padding;
                maxHeightInRow = 0;
            }

            // Check if we need a new row
            if (currentX + spriteWidth + padding > pageWidth)
            {
                currentX = padding;
                currentY += maxHeightInRow + padding;
                maxHeightInRow = 0;
            }

            // Set output
            sprite.output.x = currentX;
            sprite.output.y = currentY;
            sprite.output.page = currentPage;
            sprite.output.rot = PackTransform.None;

            // Update position for next sprite
            currentX += spriteWidth + padding;
            maxHeightInRow = Mathf.Max(maxHeightInRow, spriteHeight);
            spritesInPage++;

            sprites[i] = sprite;
        }

        return true;
    }
}
