using UnityEngine;
using UnityEngine.U2D;

public class AtlasLoader : MonoBehaviour
{
    void OnEnable() => SpriteAtlasManager.atlasRequested += OnAtlasRequested;
    void OnDisable() => SpriteAtlasManager.atlasRequested -= OnAtlasRequested;

    void OnAtlasRequested(string tag, System.Action<SpriteAtlas> callback)
    {
        // Load from Resources or AssetBundle (NOT AssetDatabase!)
        SpriteAtlas atlas = Resources.Load<SpriteAtlas>($"Atlases/{tag}");
        callback(atlas);
    }
}