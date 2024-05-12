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
        public string LinkedLayer { get; private set; }
        public Image Image { get; private set; }
        public GameObject GameObject { get; private set; }
        public RectTransform RectTransform => GameObject.transform as RectTransform;

        public MapMarker(GameObject parent, string name, MapMarkerDef def, Vector2 size, float degreesRotation = 0, float scale = 0)
            : this(parent, name, def.Category, def.ImagePath, def.Position, size, degreesRotation, scale)
        {
            LinkedLayer = def.LinkedLayer;
        }

        public MapMarker(GameObject parent, string name, string category, string imageRelativePath,
                         Vector2 position, Vector2 size, float degreesRotation = 0, float scale = 0)
        {
            Name = name;
            Category = category;

            GameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            GameObject.layer = parent.layer;
            GameObject.transform.SetParent(parent.transform);

            // set size, position and scale
            GameObject.ResetRectTransform();
            RectTransform.anchoredPosition = position;
            RectTransform.sizeDelta = size;
            RectTransform.localScale = scale * Vector2.one;

            // set rotation to combat when we rotate the whole map content
            RectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);

            // load image
            Image = GameObject.AddComponent<Image>();
            Image.sprite = TextureUtils.GetOrLoadCachedSprite(imageRelativePath);
            Image.type = Image.Type.Simple;
        }

        public void Move(Vector2 position)
        {
            RectTransform.anchoredPosition = position;
        }

        public void MoveAndRotate(Vector2 position, float degreesRotation)
        {
            RectTransform.anchoredPosition = position;
            RectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);
        }

        internal void OnLayerSelect(string layerName, bool layerSelected)
        {
            if (LinkedLayer.IsNullOrEmpty() || LinkedLayer != layerName)
            {
                return;
            }

            GameObject.SetActive(layerSelected);
        }

        internal void Destroy()
        {
            Object.Destroy(GameObject);
        }
    }
}