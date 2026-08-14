using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Editor
{
    static public partial class SpriteEditorUtility
    {
        /// <summary>
        /// Gets a readable texture for slicing operations, upscaling if necessary to match original dimensions.
        /// This ensures slicing is performed on the original image size, not the imported texture size.
        /// </summary>
        /// <param name="textureDataProvider">The texture data provider.</param>
        /// <returns>A readable Texture2D at original dimensions, or null if texture is not readable.</returns>
        static public Texture2D GetTextureToSlice(ITextureDataProvider textureDataProvider)
        {
            textureDataProvider.GetTextureActualWidthAndHeight(out var width, out var height);
            var readableTexture = textureDataProvider.GetReadableTexture2D();
            if (readableTexture == null || (readableTexture.width == width && readableTexture.height == height))
                return readableTexture;

            // Upscale the imported texture to match original dimensions for accurate slicing
            var texture = CreateTemporaryDuplicate(readableTexture, width, height);
            texture.hideFlags = HideFlags.HideAndDontSave;

            return texture;
        }

        /// <summary>
        /// Creates a temporary duplicate of a texture at a specified size using RenderTexture.
        /// Used internally to upscale textures for slicing operations.
        /// </summary>
        public static Texture2D CreateTemporaryDuplicate(Texture2D original, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture || !(bool) (Object) original)
                return null;

            RenderTexture active = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));
            Graphics.Blit(original, temporary);
            RenderTexture.active = temporary;

            bool flag = width >= SystemInfo.maxTextureSize || height >= SystemInfo.maxTextureSize;
            Texture2D temporaryDuplicate = new Texture2D(width, height, TextureFormat.RGBA32, original.mipmapCount > 1 | flag);
            temporaryDuplicate.ReadPixels(new Rect(0.0f, 0.0f, width, height), 0, 0);
            temporaryDuplicate.Apply();

            RenderTexture.ReleaseTemporary(temporary);
            temporaryDuplicate.alphaIsTransparency = original.alphaIsTransparency;

            return temporaryDuplicate;
        }
    }
}
