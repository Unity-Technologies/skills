// [UNITY-SKILL:SPRITEATLAS]

using System.Collections.Generic;
using UnityEditor.U2D;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// Provides debugging utilities for custom packers.
/// Includes logging, validation, and diagnostics for packing operations.
/// </summary>
public static class PackerDebugging
{
    /// <summary>
    /// Logs detailed information about the packing operation.
    /// Call this at the start of your Pack method for diagnostics.
    /// </summary>
    public static void LogPackingInfo(SpriteAtlasTextureSettings setting, SpriteAtlasPackingSettings config)
    {
        Debug.Log($"=== Packer Debug Info ===");
        Debug.Log($"Atlas max size: {setting.maxTextureSize}");
        Debug.Log($"Padding: {config.padding}");
        Debug.Log($"Tight packing: {config.enableTightPacking}");
        Debug.Log($"Enable rotation: {config.enableRotation}");
        Debug.Log($"Enable alpha dilation: {config.enableAlphaDilation}");
    }

    /// <summary>
    /// Validates all sprite outputs after packing.
    /// Returns false if any sprite has invalid output data.
    /// </summary>
    public static bool ValidateOutputs(ScriptablePacker.PackerData input)
    {
        NativeArray<ScriptablePacker.SpriteData> sprites = input.spriteData;
        bool valid = true;

        for (int i = 0; i < sprites.Length; i++)
        {
            ScriptablePacker.SpriteData sprite = sprites[i];

            // Check bounds
            if (sprite.output.x < 0 || sprite.output.y < 0)
            {
                Debug.LogError($"Sprite {i}: Invalid position ({sprite.output.x}, {sprite.output.y})");
                valid = false;
            }

            // Check page
            if (sprite.output.page < 0)
            {
                Debug.LogError($"Sprite {i}: Invalid page {sprite.output.page}");
                valid = false;
            }
        }

        return valid;
    }

    /// <summary>
    /// Logs statistics about the packing operation.
    /// </summary>
    public static void LogPackingStats(ScriptablePacker.PackerData input, int totalPages)
    {
        NativeArray<ScriptablePacker.SpriteData> sprites = input.spriteData;

        Debug.Log($"=== Packing Statistics ===");
        Debug.Log($"Sprites packed: {sprites.Length}");
        Debug.Log($"Total pages: {totalPages}");

        // Count by page
        var pageCounts = new System.Collections.Generic.Dictionary<int, int>();
        foreach (var sprite in sprites)
        {
            int page = sprite.output.page;
            pageCounts[page] = pageCounts.GetValueOrDefault(page) + 1;
        }

        Debug.Log($"Sprites per page:");
        foreach (var kvp in pageCounts)
        {
            Debug.Log($"  Page {kvp.Key}: {kvp.Value} sprites");
        }
    }

    /// <summary>
    /// Logs sprite data for debugging.
    /// </summary>
    public static void LogSpriteData(ScriptablePacker.PackerData input, int spriteIndex)
    {
        NativeArray<ScriptablePacker.SpriteData> sprites = input.spriteData;
        NativeArray<ScriptablePacker.TextureData> textures = input.textureData;

        if (spriteIndex >= sprites.Length)
            return;

        ScriptablePacker.SpriteData sprite = sprites[spriteIndex];
        ScriptablePacker.TextureData texture = textures[sprite.texIndex];

        Debug.Log($"=== Sprite {spriteIndex} ===");
        Debug.Log($"GUID: {sprite.guid}");
        Debug.Log($"Texture index: {sprite.texIndex}");
        Debug.Log($"Texture size: {texture.width}x{texture.height}");
        Debug.Log($"Source rect: {sprite.rect}");
        Debug.Log($"Output position: ({sprite.output.x}, {sprite.output.y})");
        Debug.Log($"Output page: {sprite.output.page}");
        Debug.Log($"Rotation: {sprite.output.rot}");
    }
}

/// <summary>
/// Example packer with built-in debugging.
/// </summary>
public class DebuggableSpritePacker : ScriptablePacker
{
    public bool enableDebugLogging = false;

    public override bool Pack(SpriteAtlasPackingSettings config,
                             SpriteAtlasTextureSettings setting,
                             PackerData input)
    {
        if (enableDebugLogging)
        {
            PackerDebugging.LogPackingInfo(setting, config);
            PackerDebugging.LogSpriteData(input, 0); // Log first sprite
        }

        // Your packing logic here...
        PackDefault(input);

        if (enableDebugLogging)
        {
            bool valid = PackerDebugging.ValidateOutputs(input);
            PackerDebugging.LogPackingStats(input, 1); // Assuming single page

            if (!valid)
            {
                Debug.LogError("Packing validation failed!");
                return false;
            }
        }

        return true;
    }

    private void PackDefault(PackerData input)
    {
        NativeArray<SpriteData> sprites = input.spriteData;
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteData sprite = sprites[i];
            sprite.output.x = i * 100;
            sprite.output.y = 0;
            sprite.output.page = 0;
            sprite.output.rot = PackTransform.None;
            sprites[i] = sprite;
        }
    }
}
