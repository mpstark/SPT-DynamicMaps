using InGameMap.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace InGameMap.UI
{
    public class TransformMarker : MapMarker
    {
        private Transform _following { get; set; }

        public static TransformMarker Create(Transform follow, GameObject parent, string imagePath, string category, Vector2 size, float scale)
        {
            var name = $"{follow.name} marker";

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.layer;
            go.transform.SetParent(parent.transform);
            go.ResetRectTransform();

            var rectTransform = go.GetRectTransform();
            rectTransform.sizeDelta = size;
            rectTransform.localScale = scale * Vector2.one;

            var marker = go.AddComponent<TransformMarker>();
            marker._following = follow;
            marker.Name = follow.name;
            marker.Category = category;
            marker.Image = go.AddComponent<Image>();
            marker.Image.sprite = TextureUtils.GetOrLoadCachedSprite(imagePath);
            marker.Image.type = Image.Type.Simple;

            return marker;
        }

        public void Update()
        {
            // move marker to follow transform
            var position3D = _following.position;
            var position2D = new Vector2(position3D.x, position3D.z);
            var angles = _following.eulerAngles;

            MoveAndRotate(position2D, -angles.y); // I'm unsure why negative rotation here
        }
    }
}
