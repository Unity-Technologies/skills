// [UNITY-SKILL:SPRITEATLAS]
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Demonstrates proper handling of late-binding for SpriteAtlases.
/// Late-binding allows atlases to be loaded on-demand when sprites are first accessed.
/// </summary>
public class HandleLateBinding : MonoBehaviour
{
    /// <summary>Prefix used for atlas resources</summary>
    [SerializeField] private string _atlasResourcePrefix = "Atlases/";

    /// <summary>Whether to automatically load atlases</summary>
    [SerializeField] private bool _autoLoad = true;

    private void Awake()
    {
        if (_autoLoad)
        {
            RegisterLateBinding();
        }
    }

    private void OnDestroy()
    {
        UnregisterLateBinding();
    }

    /// <summary>
    /// Registers for late-binding via SpriteAtlasManager.
    /// When a sprite needs an atlas, the registration callback will be invoked.
    /// </summary>
    public void RegisterLateBinding()
    {
        SpriteAtlasManager.atlasRequested += RequestLateBindingAtlas;
        SpriteAtlasManager.atlasRegistered += AtlasRegistered;
        Debug.Log("Late-binding registered");
    }

    /// <summary>
    /// Unregisters late-binding callbacks.
    /// Call this when the component is destroyed or no longer needs late-binding.
    /// </summary>
    public void UnregisterLateBinding()
    {
        SpriteAtlasManager.atlasRequested -= RequestLateBindingAtlas;
        SpriteAtlasManager.atlasRegistered -= AtlasRegistered;
        Debug.Log("Late-binding unregistered");
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
/// Example of using late-binding in a scene with multiple objects.
/// Each object can request its atlas to be loaded.
/// </summary>
public class LateBindingConsumer : MonoBehaviour
{
    [SerializeField] private Sprite _spriteToDisplay;

    private void Start()
    {
        // Accessing the sprite will trigger late-binding if needed
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && _spriteToDisplay != null)
        {
            renderer.sprite = _spriteToDisplay;
            Debug.Log($"Set sprite: {_spriteToDisplay.name}");
        }
    }
}
