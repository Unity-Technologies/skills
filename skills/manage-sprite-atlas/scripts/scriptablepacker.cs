// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor.U2D;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// Base class for implementing custom sprite packing algorithms.
/// Extend this class to create specialized packing strategies beyond Unity's default algorithms.
/// </summary>
public class CustomSpritePacker : ScriptablePacker
{
    /// <summary>
    /// Required: Implement packing logic.
    /// This method is called during atlas building to determine sprite positions.
    /// </summary>
    /// <param name="config">Packing configuration settings</param>
    /// <param name="setting">Texture settings for the atlas</param>
    /// <param name="input">Input data containing sprites, textures, and vertex data</param>
    /// <returns>True if packing succeeded, false otherwise</returns>
    public override bool Pack(SpriteAtlasPackingSettings config,
                             SpriteAtlasTextureSettings setting,
                             PackerData input)
    {
        // Access input data
        NativeArray<SpriteData> sprites = input.spriteData;
        NativeArray<TextureData> textures = input.textureData;
        NativeArray<Color32> colors = input.colorData;
        NativeArray<int> indices = input.indexData;
        NativeArray<Vector2> vertices = input.vertexData;

        // Implement packing algorithm
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteData sprite = sprites[i];

            // Set output position and page
            sprite.output.x = CalculateX(i);
            sprite.output.y = CalculateY(i);
            sprite.output.page = CalculatePage(i);
            sprite.output.rot = PackTransform.None;

            sprites[i] = sprite;
        }

        return true; // Return true if packing succeeded
    }

    /// <summary>
    /// Optional: Implement fitting without texture creation.
    /// This method is used to determine if sprites fit within the atlas bounds.
    /// </summary>
    protected override bool Fit(SpriteAtlasPackingSettings config,
                               SpriteAtlasTextureSettings setting,
                               PackerData input)
    {
        // Implement fit algorithm (optional)
        return false;
    }

    #region Helper Methods

    private int CalculateX(int index)
    {
        // Custom X position calculation
        return index * 100;
    }

    private int CalculateY(int index)
    {
        // Custom Y position calculation
        return (index / 10) * 100;
    }

    private int CalculatePage(int index)
    {
        // Page assignment logic
        return index / 64;
    }

    #endregion
}
