// [UNITY-SKILL:SPRITEATLAS]

using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// Information about each sprite in the packing process.
/// This structure is populated by Unity and used by custom packers.
/// </summary>
public struct SpriteData
{
    /// <summary>Unique identifier for the sprite asset</summary>
    public GUID guid;

    /// <summary>Index into the textureData array</summary>
    public int texIndex;

    /// <summary>Source rectangle in the source texture</summary>
    public RectInt rect;

    /// <summary>Number of mesh indices for this sprite</summary>
    public int indexCount;

    /// <summary>Number of mesh vertices for this sprite</summary>
    public int vertexCount;

    /// <summary>Offset into the indexData array</summary>
    public int indexOffset;

    /// <summary>Offset into the vertexData array</summary>
    public int vertexOffset;

    /// <summary>OUTPUT: Set this to specify packing result</summary>
    public ScriptablePacker.SpritePack output;
}
