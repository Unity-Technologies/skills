// [UNITY-SKILL:SPRITEATLAS]
using UnityEngine;

/// <summary>
/// Sprite transformation options during packing.
/// These transforms are applied to sprites when packing them into the atlas.
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

/// <summary>
/// Extension methods for PackTransform.
/// </summary>
public static class PackTransformExtensions
{
    /// <summary>
    /// Gets a human-readable name for the transform.
    /// </summary>
    public static string GetName(this PackTransform transform)
    {
        return transform.ToString();
    }

    /// <summary>
    /// Checks if the transform includes horizontal flipping.
    /// </summary>
    public static bool HasHorizontalFlip(this PackTransform transform)
    {
        return transform == PackTransform.FlipHorizontal;
    }

    /// <summary>
    /// Checks if the transform includes vertical flipping.
    /// </summary>
    public static bool HasVerticalFlip(this PackTransform transform)
    {
        return transform == PackTransform.FlipVertical;
    }
}
