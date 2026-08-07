using UnityEngine;
using UnityEngine.U2D;

// Query sprites *after* atlas is loaded via SpriteAtlasManager
public static class SpriteQueryHelper
{
    public static Sprite GetSpriteByName(SpriteAtlas atlas, string name)
    {
        // atlas.GetSprite(name) is safe in runtime context
        return atlas.GetSprite(name);
    }

    public static Sprite[] GetSpritesByPrefix(SpriteAtlas atlas, string prefix)
    {
        Sprite[] sprites = new Sprite[atlas.spriteCount];
        int count = atlas.GetSprites(sprites);
        return System.Array.FindAll(sprites, s => s.name.StartsWith(prefix));
    }
}
