// [UNITY-SKILL:SPRITEATLAS]
using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;

/// <summary>
/// Demonstrates caching loaded SpriteAtlases for efficient reuse.
/// Avoid reloading the same atlas multiple times by caching loaded instances.
/// </summary>
public class CacheLoadedAtlases : MonoBehaviour
{
    /// <summary>Cached dictionary of loaded atlases</summary>
    private readonly Dictionary<string, SpriteAtlas> _loadedAtlases = new Dictionary<string, SpriteAtlas>();

    /// <summary>Reference to SpriteAtlasManager for loading</summary>
    private SpriteAtlasManager _atlasManager;

    private void Awake()
    {
        RegisterCallbacks();
    }

    private void OnDestroy()
    {
        UnregisterCallbacks();
        ClearCache();
    }

    private void RegisterCallbacks()
    {
        SpriteAtlasManager.atlasRegistered += OnAtlasLoaded;
    }

    private void UnregisterCallbacks()
    {
        SpriteAtlasManager.atlasRegistered -= OnAtlasLoaded;
    }

    /// <summary>
    /// Gets an atlas by name, loading it if not already cached.
    /// Returns true if the atlas was found (cached or newly loaded).
    /// </summary>
    public bool GetAtlas(string atlasName, out SpriteAtlas atlas)
    {
        // Check cache first
        if (_loadedAtlases.TryGetValue(atlasName, out atlas))
        {
            return true;
        }

        // Not cached - load it
        return LoadAndCacheAtlas(atlasName);
    }

    /// <summary>
    /// Loads an atlas and adds it to the cache.
    /// </summary>
    private bool LoadAndCacheAtlas(string atlasName)
    {
        // Example: Load from Resources (not recommended for production)
        // SpriteAtlas atlas = Resources.Load<SpriteAtlas>($"Atlases/{atlasName}");

        // For production, use Addressables or AssetBundle loading
        // SpriteAtlas atlas = await Addressables.LoadAssetAsync<SpriteAtlas>($"Atlases/{atlasName}").Task;

        Debug.Log($"Loading and caching atlas: {atlasName}");

        // Placeholder - replace with actual loading logic
        SpriteAtlas atlas = null; // Replace with actual loaded atlas

        if (atlas != null)
        {
            _loadedAtlases[atlasName] = atlas;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a cached atlas without attempting to load it.
    /// Returns false if not found in cache.
    /// </summary>
    public bool TryGetCachedAtlas(string atlasName, out SpriteAtlas atlas)
    {
        return _loadedAtlases.TryGetValue(atlasName, out atlas);
    }

    /// <summary>
    /// Checks if an atlas is currently cached.
    /// </summary>
    public bool IsAtlasCached(string atlasName)
    {
        return _loadedAtlases.ContainsKey(atlasName);
    }

    /// <summary>
    /// Removes an atlas from the cache without destroying it.
    /// The atlas can still be used, but will need to be reloaded if requested again.
    /// </summary>
    public bool RemoveFromCache(string atlasName)
    {
        return _loadedAtlases.Remove(atlasName);
    }

    /// <summary>
    /// Clears all cached atlases.
    /// Use this when switching scenes or cleaning up.
    /// </summary>
    public void ClearCache()
    {
        _loadedAtlases.Clear();
        Debug.Log("Atlas cache cleared");
    }

    /// <summary>
    /// Gets the number of atlases currently in the cache.
    /// </summary>
    public int CachedAtlasCount => _loadedAtlases.Count;

    /// <summary>
    /// Logs all cached atlas names.
    /// </summary>
    public void LogCachedAtlases()
    {
        Debug.Log("Cached atlases:");
        foreach (var kvp in _loadedAtlases)
        {
            Debug.Log($"  - {kvp.Key}");
        }
    }

    /// <summary>
    /// Callback when an atlas is loaded by SpriteAtlasManager.
    /// Adds the loaded atlas to our cache for future use.
    /// </summary>
    private void OnAtlasLoaded(SpriteAtlas atlas)
    {
        if (atlas != null && !_loadedAtlases.ContainsKey(atlas.name))
        {
            _loadedAtlases[atlas.name] = atlas;
            Debug.Log($"Atlas cached: {atlas.name}");
        }
    }
}
