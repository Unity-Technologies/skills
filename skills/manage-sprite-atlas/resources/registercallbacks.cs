// [UNITY-SKILL:SPRITEATLAS]
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Demonstrates registering SpriteAtlasManager callbacks early in the lifecycle.
/// Always register callbacks in Awake() or OnEnable(), not in Start().
/// </summary>
public class RegisterCallbacksExample : MonoBehaviour
{
    /// <summary>
    /// Registers atlas loading callbacks when the component awakens.
    /// This is the correct place - before any sprites might need atlases.
    /// </summary>
    private void Awake()
    {
        RegisterAtlasCallbacks();
    }

    /// <summary>
    /// Registers SpriteAtlasManager callbacks for dynamic atlas loading.
    /// Called during Awake() to ensure callbacks are ready before any sprite access.
    /// </summary>
    private void RegisterAtlasCallbacks()
    {
        // Register for atlas load events
        SpriteAtlasManager.atlasRequested += RequestLateBindingAtlas;
        SpriteAtlasManager.atlasRegistered += AtlasRegistered;

        Debug.Log("SpriteAtlas callbacks registered");
    }

    /// <summary>
    /// Unregisters callbacks when the component is destroyed.
    /// Always unregister to prevent memory leaks and errors.
    /// </summary>
    private void OnDestroy()
    {
        SpriteAtlasManager.atlasRequested -= RequestLateBindingAtlas;
        SpriteAtlasManager.atlasRegistered -= AtlasRegistered;

        Debug.Log("SpriteAtlas callbacks unregistered");
    }

    /// <summary>
    /// Callback invoked when a sprite needs its atlas but it's not loaded yet.
    /// This is where you implement the late-binding logic.
    /// </summary>
    void RequestLateBindingAtlas(string tag, System.Action<SpriteAtlas> action)
    {
        // Determine which atlas should contain this sprite
        string atlasName = GetAtlasNameForSprite(tag);

        if (string.IsNullOrEmpty(atlasName))
        {
            Debug.LogWarning($"Could not determine atlas for sprite: {tag}");
            return;
        }

        // Load the atlas asynchronously
        LoadAtlasAsync(atlasName);
    }

    /// <summary>
    /// Gets the atlas name for a given sprite.
    /// This is project-specific logic - adjust based on your naming conventions.
    /// </summary>
    private string GetAtlasNameForSprite(string tag)
    {
        // Pattern 1: Use sprite name prefix
        if (tag.StartsWith("btn_"))
            return "UI_Buttons";
        else if (tag.StartsWith("icon_"))
            return "UI_Icons";
        else if (tag.StartsWith("char_"))
            return "Characters";
        return "Default";
    }

    /// <summary>
    /// Loads an atlas asynchronously.
    /// In a real implementation, this would use Addressables or AssetBundle loading.
    /// </summary>
    private void LoadAtlasAsync(string atlasName)
    {
        // Example using Resources.Load (not recommended for production)
        // SpriteAtlas atlas = Resources.Load<SpriteAtlas>(atlasName);

        // For production with Addressables:
        /*
        var handle = Addressables.LoadAssetAsync<SpriteAtlas>(atlasName);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Late-bound atlas loaded: {atlasName}");
            }
            else
            {
                Debug.LogError($"Failed to load late-bound atlas: {atlasName}");
            }
        };
        */

        Debug.Log($"Loading late-bound atlas: {atlasName}");
    }

    void AtlasRegistered(SpriteAtlas spriteAtlas)
    {
        Debug.LogFormat("Registered {0}.", spriteAtlas.name);
    }

    /// <summary>
    /// Forces loading of a specific atlas (for manual control).
    /// </summary>
    public void ForceLoadAtlas(string atlasName)
    {
        LoadAtlasAsync(atlasName);
    }
}

/// <summary>
/// Alternative pattern using OnEnable/OnDisable instead of Awake/OnDestroy.
/// Use this if the component can be enabled/disabled dynamically.
/// </summary>
public class RegisterCallbacksWithEnable : MonoBehaviour
{
    private void OnEnable()
    {
        RegisterAtlasCallbacks();
    }

    private void OnDisable()
    {
        UnregisterAtlasCallbacks();
    }

    private void RegisterAtlasCallbacks()
    {
        SpriteAtlasManager.atlasRequested += RequestLateBindingAtlas;
        SpriteAtlasManager.atlasRegistered += OnAtlasLoaded;
    }

    private void UnregisterAtlasCallbacks()
    {
        SpriteAtlasManager.atlasRequested -= RequestLateBindingAtlas;
        SpriteAtlasManager.atlasRegistered -= OnAtlasLoaded;
    }

    private void OnAtlasLoaded(SpriteAtlas atlas) { }
    private void RequestLateBindingAtlas(string tag, System.Action<SpriteAtlas> action) { }
}
