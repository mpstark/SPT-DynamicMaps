using System.Linq;
using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI
{
    public class MapLayer
    {
        public string Name { get; private set; }
        public Image Image { get; private set; }
        public GameObject GameObject { get; private set; }
        public Vector2 HeightBounds { get; private set; }

        public int Level => _def.Level;
        public RectTransform RectTransform => GameObject.transform as RectTransform;

        private MapLayerDef _def = new MapLayerDef();

        public MapLayer(GameObject parent, string name, MapLayerDef layerDef, float degreesRotation)
        {
            Name = name;
            _def = layerDef;

            GameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            GameObject.layer = parent.layer;
            GameObject.transform.SetParent(parent.transform);
            GameObject.ResetRectTransform();

            // convert 3d bounds to 2d ones
            var bounds2d = _def.Bounds.Select(p => new Vector2(p.x, p.y));
            HeightBounds = new Vector2(_def.Bounds.Min(p => p.z), _def.Bounds.Max(p => p.z));

            // set layer size
            var size = MathUtils.GetBoundingRectangle(bounds2d);
            var rotatedSize = MathUtils.GetRotatedRectangle(size, degreesRotation);
            RectTransform.sizeDelta = rotatedSize;

            // set layer offset
            var offset = MathUtils.GetMidpoint(bounds2d);
            RectTransform.anchoredPosition = offset;

            // set rotation to combat when we rotate the whole map content
            RectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);

            // load image
            Image = GameObject.AddComponent<Image>();
            Image.sprite = TextureUtils.GetOrLoadCachedSprite(_def.ImagePath);
            Image.type = Image.Type.Simple;
        }
    }
}
