// [UNITY-SKILL:SPRITEATLAS]
using UnityEngine;

/// <summary>
/// Output structure for sprite packing.
/// Set this on each SpriteData to specify where and how the sprite should be packed.
/// </summary>
public struct SpritePack
{
    /// <summary>X position in the atlas (OUTPUT)</summary>
    public int x;

    /// <summary>Y position in the atlas (OUTPUT)</summary>
    public int y;

    /// <summary>Atlas page index for multi-page atlases (OUTPUT)</summary>
    public int page;

    /// <summary>Rotation/flip transform to apply (OUTPUT)</summary>
    public PackTransform rot;
}

/// <summary>
/// Sprite transformation options during packing.
/// </summary>
public enum PackTransform
{
    /// <summary>No transformation applied</summary>
    None,

    /// <summary>Flip horizontally</summary>
    FlipHorizontal,

    /// <summary>Flip vertically</summary>
    FlipVertical,

    /// <summary>Rotate 180 degrees</summary>
    Rotate180
}
