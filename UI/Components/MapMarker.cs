using System;
using InGameMap.Data;
using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI.Components
{
    public class MapMarker : MonoBehaviour
    {
        public event Action<MapMarker> OnPositionChanged;

        public string Name { get; protected set; }
        public string Category { get; protected set; }
        public Vector3 Position { get; protected set;}
        public Image Image { get; protected set; }
        public bool IsDynamic { get; protected set; } = false;
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        public static MapMarker Create(GameObject parent, string name, MapMarkerDef def, Vector2 size,
                                       float degreesRotation = 0, float scale = 1)
        {
            var mapMarker = Create(parent, name, def.Category, def.ImagePath, def.Position, size, degreesRotation, scale);
            return mapMarker;
        }

        public static MapMarker Create(GameObject parent, string name, string category, string imageRelativePath,
                                       Vector3 position, Vector2 size, float degreesRotation = 0, float scale = 1)
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
            OnPositionChanged = null;
        }

        public void Move(Vector3 newPosition)
        {
            RectTransform.anchoredPosition = newPosition; // vector3 to vector2 discards z
            Position = newPosition;

            OnPositionChanged?.Invoke(this);
        }

        public void Rotate(float degreesRotation)
        {
            RectTransform.localRotation = Quaternion.Euler(0, 0, degreesRotation);
        }

        public void MoveAndRotate(Vector3 newPosition, float rotation)
        {
            Move(newPosition);
            Rotate(rotation);
        }

        public virtual void OnContainingLayerChanged(bool isDisplayed, bool isOnTopLevel)
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
