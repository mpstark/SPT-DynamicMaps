using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    public class TransformMapMarker : MapMarker
    {
        private static float _maxCallbackTime = 0.5f;  // how often to call callback in seconds
        private static Vector2 _pivot = new Vector2(0.5f, 0.5f);

        public Transform FollowingTransform { get; private set; }

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

        private void Update()
        {
            if (FollowingTransform == null)
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

            MoveAndRotate(MathUtils.ConvertToMapPosition(FollowingTransform),
                          MathUtils.ConvertToMapRotation(FollowingTransform),
                          callback);
        }
    }
}
