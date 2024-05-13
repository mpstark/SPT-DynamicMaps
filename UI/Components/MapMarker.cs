using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI.Components
{
    public class MapMarker : MonoBehaviour
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public RectTransform RectTransform => gameObject.transform as RectTransform;
        public Image Image { get; protected set; }

        private MapLayer _linkedLayer;
        public MapLayer LinkedLayer
        {
            get
            {
                return _linkedLayer;
            }
            set
            {
                if (_linkedLayer == value)
                {
                    return;
                }

                if (_linkedLayer != null)
                {
                    _linkedLayer.OnLayerDisplayChanged -= OnLinkedLayerChanged;
                }

                _linkedLayer = value;
                _linkedLayer.OnLayerDisplayChanged += OnLinkedLayerChanged;
                OnLinkedLayerChanged(_linkedLayer.IsDisplayed, _linkedLayer.IsOnTopLevel);
            }
        }

        public static MapMarker Create(GameObject parent, string name, MapMarkerDef def, Vector2 size,
                                       float degreesRotation = 0, float scale = 1)
        {
            var mapMarker = Create(parent, name, def.Category, def.ImagePath, def.Position, size, degreesRotation, scale);
            return mapMarker;
        }

        public static MapMarker Create(GameObject parent, string name, string category, string imageRelativePath,
                                       Vector2 position, Vector2 size, float degreesRotation = 0, float scale = 0)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.layer;
            go.transform.SetParent(parent.transform);
            go.ResetRectTransform();

            var rectTransform = go.GetRectTransform();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = scale * Vector2.one;
            rectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);

            var marker = go.AddComponent<MapMarker>();
            marker.Name = name;
            marker.Category = category;
            marker.Image = go.AddComponent<Image>();
            marker.Image.sprite = TextureUtils.GetOrLoadCachedSprite(imageRelativePath);
            marker.Image.type = Image.Type.Simple;

            return marker;
        }

        protected virtual void OnDestroy()
        {
            if (_linkedLayer == null)
            {
                return;
            }

            _linkedLayer.OnLayerDisplayChanged -= OnLinkedLayerChanged;
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

        protected virtual void OnLinkedLayerChanged(bool isDisplayed, bool isOnTopLevel)
        {
            // TODO: revisit this
            var color = Image.color;
            var alpha = 1f;
            if (!isDisplayed)
            {
                alpha = 0.0f;
            }
            else if (!isOnTopLevel)
            {
                alpha = 0.25f;
            }

            var newColor = new Color(color.r, color.g, color.b, alpha);
            Image.color = newColor;
        }
    }
}
