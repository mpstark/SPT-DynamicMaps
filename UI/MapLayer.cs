using System.Linq;
using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI
{
    public class MapLayer : MonoBehaviour
    {
        public string Name { get; private set; }
        public Image Image { get; private set; }
        public Vector2 HeightBounds { get; private set; }
        public int Level => _def.Level;
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        private MapLayerDef _def = new MapLayerDef();

        public static MapLayer Create(GameObject parent, string name, MapLayerDef def, float degreesRotation)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.layer;
            go.transform.SetParent(parent.transform);
            go.ResetRectTransform();

            var rectTransform = go.GetRectTransform();
            var layer = go.AddComponent<MapLayer>();

            // convert 3d bounds to 2d ones
            var bounds2d = def.Bounds.Select(p => new Vector2(p.x, p.y));
            layer.HeightBounds = new Vector2(def.Bounds.Min(p => p.z), def.Bounds.Max(p => p.z));

            // set layer size
            var size = MathUtils.GetBoundingRectangle(bounds2d);
            var rotatedSize = MathUtils.GetRotatedRectangle(size, degreesRotation);
            rectTransform.sizeDelta = rotatedSize;

            // set layer offset
            var offset = MathUtils.GetMidpoint(bounds2d);
            rectTransform.anchoredPosition = offset;

            // set rotation to combat when we rotate the whole map content
            rectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);

            layer.Name = name;
            layer._def = def;

            // load image
            layer.Image = go.AddComponent<Image>();
            layer.Image.sprite = TextureUtils.GetOrLoadCachedSprite(def.ImagePath);
            layer.Image.type = Image.Type.Simple;

            return layer;
        }
    }
}
