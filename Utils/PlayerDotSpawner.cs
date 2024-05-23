using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps
{
    public class PlayerDotSpawner : MonoBehaviour
    {
        private static float _spawnTime = 0.25f;
        private float _timeAccumulator = 0f;

        public MapView MapView { get; set; }

        private void Update()
        {
            if (MapView == null)
            {
                return;
            }

            if (!Input.GetKey(KeyCode.M) || !Input.GetKey(KeyCode.LeftShift))
            {
                return;
            }

            _timeAccumulator += Time.deltaTime;
            if (_timeAccumulator <= _spawnTime && !Input.GetKeyDown(KeyCode.M))
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                ImagePath = "Markers/dot.png",
                Position = MathUtils.ConvertToMapPosition(GameUtils.GetMainPlayer().Position)
            };

            MapView.AddMapMarker(markerDef);
            _timeAccumulator = 0f;
        }
    }
}
