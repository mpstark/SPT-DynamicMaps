using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI
{
    public class MapMarker
    {
        public string Name { get; private set; }
        public string Category { get; private set; }
        public Image Image { get; private set; }
        public GameObject GameObject { get; private set; }
        public RectTransform RectTransform => GameObject.transform as RectTransform;

        public MapMarker(GameObject parent, string name, MapMarkerDef def, Vector2 size, float degreesRotation, float zoom)
            : this(parent, name, def.Category, def.ImagePath, def.Position, size, degreesRotation, zoom) {}

        public MapMarker(GameObject parent, string name, string category, string imageRelativePath, Vector2 position, Vector2 size, float degreesRotation, float zoom)
        {
            Name = name;

            GameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            GameObject.layer = parent.layer;
            GameObject.transform.SetParent(parent.transform);

            // set position
            GameObject.ResetRectTransform();
            RectTransform.anchoredPosition = position;
            RectTransform.sizeDelta = size;

            // set rotation to combat when we rotate the whole map content
            RectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);
            RectTransform.localScale = (1 / zoom) * Vector3.one;

            // load image
            Image = GameObject.AddComponent<Image>();
            Image.sprite = TextureUtils.GetOrLoadCachedSprite(imageRelativePath);
            Image.type = Image.Type.Simple;
        }
    }
}