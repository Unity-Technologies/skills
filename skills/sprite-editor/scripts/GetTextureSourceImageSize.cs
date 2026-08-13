using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Editor
{
    static public partial class SpriteEditorUtility
    {
        /// <summary>
        /// Get the original source image size of a texture, which is different from the texture size when the texture is imported with "Max Size" smaller than the original image size.
        /// This method will try to get the original source image size from TextureImporter, if it fails, it will return the texture size as fallback.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        static public void GetTextureSourceImageSize(Texture2D texture, out int width, out int height)
        {
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            var dataProvider = factories.GetSpriteEditorDataProviderFromObject(texture);
            var textureDataProvider = dataProvider?.GetDataProvider<ITextureDataProvider>();
            if(textureDataProvider != null)
            {
                textureDataProvider.GetTextureActualWidthAndHeight(out width, out height);
                return;
            }

            width = texture.width;
            height = texture.height;
        }
    }
}
