using Comfort.Common;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    public class TransformMapMarker : MapMarker
    {
        private static float _maxCallbackTime = 0.5f;  // how often to call callback in seconds
        private static Vector2 _pivot = new Vector2(0.5f, 0.5f);

        public Transform FollowingTransform { get; private set; }
        public MathUtils.RotationAxis RotationAxis { get; set; } = MathUtils.RotationAxis.Y;

        private float _callbackTime = _maxCallbackTime;  // make sure to start with a callback

        public static TransformMapMarker Create(Transform followingTransform, GameObject parent, string imagePath, Color color,
                                                string name, string category, Vector2 size, float degreesRotation, float scale)
        {
            var marker = Create<TransformMapMarker>(parent, name, category, imagePath, color,
                                                    MathUtils.ConvertToMapPosition(followingTransform),
                                                    size, _pivot, degreesRotation, scale);
            marker.IsDynamic = true;
            marker.FollowingTransform = followingTransform;

            return marker;
        }

        public TransformMapMarker()
        {
            ImageAlphaLayerStatus[LayerStatus.Hidden] = 0.25f;
            ImageAlphaLayerStatus[LayerStatus.Underneath] = 0.25f;
            ImageAlphaLayerStatus[LayerStatus.OnTop] = 1f;
            ImageAlphaLayerStatus[LayerStatus.FullReveal] = 1f;

            LabelAlphaLayerStatus[LayerStatus.Hidden] = 0.0f;
            LabelAlphaLayerStatus[LayerStatus.Underneath] = 0.0f;
            LabelAlphaLayerStatus[LayerStatus.OnTop] = 0.0f;
            LabelAlphaLayerStatus[LayerStatus.FullReveal] = 1.00f;
        }

        private void FixedUpdate()
        {
            if (FollowingTransform == null)
            {
                return;
            }

            var mapPosition = MathUtils.ConvertToMapPosition(FollowingTransform);
            var mapRotation = MathUtils.ConvertToMapRotation(FollowingTransform, RotationAxis);

            // check if in exactly the same place and skip updating if it is
            if (MathUtils.ApproxEquals(Position.x, mapPosition.x)
             && MathUtils.ApproxEquals(Position.y, mapPosition.y)
             && MathUtils.ApproxEquals(Position.z, mapPosition.z)
             && MathUtils.ApproxEquals(Rotation, mapRotation))
            {
                return;
            }

            // throttle callback, since that leads to a layer search which might be expensive
            _callbackTime += Time.deltaTime;
            var callback = _callbackTime >= _maxCallbackTime;
            if (callback)
            {
                _callbackTime = 0f;
            }

            MoveAndRotate(mapPosition, mapRotation, callback);
        }
    }
}
