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

        public int LayerNumber => _def.LayerNumber;
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

            // set layer size
            var size = MathUtils.GetBoundingRectangle(_def.Bounds);
            var rotatedSize = MathUtils.GetRotatedRectangle(size, degreesRotation);
            RectTransform.sizeDelta = rotatedSize;

            // set layer offset
            var offset = MathUtils.GetMidpoint(layerDef.Bounds);
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
