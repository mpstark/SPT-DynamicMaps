using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class TextureUtils
    {
        private static Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

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

        public static Sprite GetOrLoadCachedSprite(string path)
        {
            if (_spriteCache.ContainsKey(path))
            {
                return _spriteCache[path];
            }

            var absolutePath = Path.Combine(Plugin.Path, path);
            var texture = LoadTexture2DFromPath(absolutePath);
            _spriteCache[path] = Sprite.Create(texture,
                                               new Rect(0f, 0f, texture.width, texture.height),
                                               new Vector2(texture.width / 2, texture.height / 2));

            return _spriteCache[path];
        }
    }
}
