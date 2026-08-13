using UnityEditor;
using UnityEngine;

namespace Editor
{
    static public partial class SpriteEditorUtility
    {
        static public byte[] SpriteToPng(Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            Rect rect = sprite.textureRect;

            // Create a temporary RenderTexture
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                (int)rect.width,
                (int)rect.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);

            // Save the current active RenderTexture
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;

            GL.Clear(true, true, Color.clear);

            // Get sprite vertices and UVs
            Vector2[] vertices = sprite.vertices;
            Vector2[] uvs = sprite.uv;
            ushort[] triangles = sprite.triangles;

            // Calculate bounds for centering
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (var v in vertices)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }

            Vector2 size = max - min;
            Vector2 offset = -min;

            // Create material for rendering the sprite with proper alpha
            Material mat = new Material(Shader.Find("UI/Default"));
            mat.mainTexture = texture;

            // Render the sprite mesh
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, rect.width, 0, rect.height);

            mat.SetPass(0);
            GL.Begin(GL.TRIANGLES);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    int idx = triangles[i + j];
                    Vector2 vertex = vertices[idx];
                    Vector2 uv = uvs[idx];

                    // Transform vertex to render texture space
                    float x = (vertex.x + offset.x) * rect.width / size.x;
                    float y = (vertex.y + offset.y) * rect.height / size.y;

                    GL.TexCoord2(uv.x, uv.y);
                    GL.Vertex3(x, y, 0);
                }
            }

            GL.End();
            GL.PopMatrix();

            // Read pixels from RenderTexture into a new Texture2D
            Texture2D croppedTexture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.ARGB32, false);
            croppedTexture.ReadPixels(new Rect(0, 0, rect.width, rect.height), 0, 0);
            croppedTexture.Apply();

            // Restore the previous RenderTexture and clean up
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(renderTexture);
            Object.DestroyImmediate(mat);

            return croppedTexture.EncodeToPNG();
        }
    }
}
