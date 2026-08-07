// [UNITY-SKILL:SPRITEATLAS]
using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;
using UnityEditor.U2D;

/// <summary>
/// Demonstrates efficient querying of sprites from SpriteAtlases.
/// Avoids expensive operations and unnecessary allocations.
/// </summary>
public static class QuerySpritesEfficientlyExample
{
    /// <summary>
    /// Gets all sprites from an atlas efficiently.
    /// Uses the V2 GetPackables() method on runtime SpriteAtlas.
    /// </summary>
    public static Sprite[] GetSpritesFromAtlas(SpriteAtlas atlas)
    {
        if (atlas == null)
        {
            Debug.LogError("Cannot query: atlas is null");
            return new Sprite[0];
        }

        // Use GetPackables() - this is the V2 way
        Object[] packables = atlas.GetPackables();

        // Filter to get only sprites
        var sprites = new List<Sprite>();
        for (int i = 0; i < packables.Length; i++)
        {
            if (packables[i] is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }

        return sprites.ToArray();
    }

    /// <summary>
    /// Gets a sprite by name from an atlas efficiently.
    /// Avoids iterating through all sprites on each call by caching the results.
    /// </summary>
    public static Sprite GetSpriteFromAtlas(SpriteAtlas atlas, string spriteName)
    {
        if (atlas == null)
        {
            Debug.LogError("Cannot query: atlas is null");
            return null;
        }

        if (string.IsNullOrEmpty(spriteName))
        {
            Debug.LogWarning("Cannot query: sprite name is null or empty");
            return null;
        }

        // Get all sprites once
        Sprite[] sprites = GetSpritesFromAtlas(atlas);

        // Find matching sprite
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i].name == spriteName)
            {
                return sprites[i];
            }
        }

        Debug.LogWarning($"Sprite '{spriteName}' not found in atlas: {atlas.name}");
        return null;
    }

    /// <summary>
    /// Queries multiple sprites by name from an atlas.
    /// More efficient than calling GetSpriteFromAtlas multiple times
    /// when you need to query many sprites at once.
    /// </summary>
    public static Sprite[] GetSpritesFromAtlas(SpriteAtlas atlas, string[] spriteNames)
    {
        if (atlas == null || spriteNames == null || spriteNames.Length == 0)
            return new Sprite[0];

        // Get all sprites once
        Sprite[] allSprites = GetSpritesFromAtlas(atlas);

        // Create lookup dictionary for efficient searching
        var spriteLookup = new Dictionary<string, Sprite>();
        for (int i = 0; i < allSprites.Length; i++)
        {
            spriteLookup[allSprites[i].name] = allSprites[i];
        }

        // Query requested sprites
        var result = new List<Sprite>(spriteNames.Length);
        for (int i = 0; i < spriteNames.Length; i++)
        {
            if (spriteLookup.TryGetValue(spriteNames[i], out Sprite foundSprite))
            {
                result.Add(foundSprite);
            }
            else
            {
                Debug.LogWarning($"Sprite '{spriteNames[i]}' not found in atlas: {atlas.name}");
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Checks if an atlas contains a specific sprite by name.
    /// </summary>
    public static bool ContainsSprite(SpriteAtlas atlas, string spriteName)
    {
        if (atlas == null || string.IsNullOrEmpty(spriteName))
            return false;

        Sprite[] sprites = GetSpritesFromAtlas(atlas);

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i].name == spriteName)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the count of sprites in an atlas without loading all sprites.
    /// </summary>
    public static int GetSpriteCount(SpriteAtlas atlas)
    {
        if (atlas == null)
            return 0;

        Object[] packables = atlas.GetPackables();
        int count = 0;

        for (int i = 0; i < packables.Length; i++)
        {
            if (packables[i] is Sprite)
                count++;
        }

        return count;
    }
}

/// <summary>
/// Component that demonstrates efficient sprite querying.
/// Caches queries for repeated access.
/// </summary>
public class EfficientSpriteQuery : MonoBehaviour
{
    [SerializeField] private SpriteAtlas _atlas;

    private Dictionary<string, Sprite> _spriteCache;
    private bool _cacheInitialized = false;

    /// <summary>
    /// Initializes the cache with all sprites from the atlas.
    /// Call this once at startup for efficient repeated queries.
    /// </summary>
    public void InitializeCache()
    {
        if (_atlas == null)
            return;

        Sprite[] sprites = QuerySpritesEfficientlyExample.GetSpritesFromAtlas(_atlas);

        _spriteCache = new Dictionary<string, Sprite>(sprites.Length);
        for (int i = 0; i < sprites.Length; i++)
        {
            _spriteCache[sprites[i].name] = sprites[i];
        }

        _cacheInitialized = true;
        Debug.Log($"Cached {sprites.Length} sprites from atlas: {_atlas.name}");
    }

    /// <summary>
    /// Gets a sprite by name using the cached lookup.
    /// Much faster than querying the atlas directly for each call.
    /// </summary>
    public Sprite GetCachedSprite(string name)
    {
        if (!_cacheInitialized)
        {
            InitializeCache();
        }

        if (_spriteCache.TryGetValue(name, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"Sprite '{name}' not found in cached atlas");
        return null;
    }

    /// <summary>
    /// Gets a sprite by name, with optional cache initialization.
    /// </summary>
    public Sprite GetSprite(string name, bool initializeIfNeeded = true)
    {
        if (!_cacheInitialized && initializeIfNeeded)
        {
            InitializeCache();
        }

        if (_spriteCache != null && _spriteCache.TryGetValue(name, out Sprite sprite))
        {
            return sprite;
        }

        // Fallback to direct query if cache not available
        return QuerySpritesEfficientlyExample.GetSpriteFromAtlas(_atlas, name);
    }
}
