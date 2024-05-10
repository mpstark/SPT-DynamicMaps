using System.IO;
using UnityEngine;

namespace SimpleCrosshair.Utils
{
    public static class TextureUtils
    {
        public static Texture2D LoadTexture2DFromPath(string absolutePath)
        {
            if (!File.Exists(absolutePath))
            {
                return null;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(absolutePath));

            return tex;
        }
    }
}
