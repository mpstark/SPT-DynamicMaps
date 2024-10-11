using DynamicMaps.Utils;
using DynamicMaps.Data;
using EFT;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    public class HotZonesMarker: MapMarker
    {
        private static float _maxCallbackTime = 0.5f;  // how often to call callback in seconds
        private static Vector2 _pivot = new Vector2(0.5f, 0.5f);

        public IPlayer Player { get; private set; }

        private float _callbackTime = _maxCallbackTime;  // make sure to start with a callback

        public static HotZonesMarker Create(GameObject parent, string text, string category, Color color, string imagePath, Vector3 position, 
                                            Vector2 size, float degreesRotation, float scale )
        {
            var mapMarker = Create<HotZonesMarker>(parent, text, "HotZone", imagePath, color, 
                                                    position, size, _pivot, degreesRotation, scale);

            return mapMarker;
        }

        public HotZonesMarker()
        {
            // ImageAlphaLayerStatus[LayerStatus.Hidden] = 0.25f;
            // ImageAlphaLayerStatus[LayerStatus.Underneath] = 0.25f;
            // ImageAlphaLayerStatus[LayerStatus.OnTop] = 0.25f;
            // ImageAlphaLayerStatus[LayerStatus.FullReveal] = 0.25f;

            // LabelAlphaLayerStatus[LayerStatus.Hidden] = 0.0f;
            // LabelAlphaLayerStatus[LayerStatus.Underneath] = 0.0f;
            // LabelAlphaLayerStatus[LayerStatus.OnTop] = 0.0f;
            // LabelAlphaLayerStatus[LayerStatus.FullReveal] = 1.00f;
        }

        private void LateUpdate()
        {
            if (Player?.Transform?.Original == null)
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

            MoveAndRotate(MathUtils.ConvertToMapPosition(Player.Position), -Player.Rotation.x, callback);
        }
    }
}
