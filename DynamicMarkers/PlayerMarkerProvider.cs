using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class PlayerMarkerProvider : IDynamicMarkerProvider
    {
        // TODO: move to config
        private const string _playerCategory = "Main Player";
        private const string _playerImagePath = "Markers/arrow.png";
        private static Color _playerColor = Color.green;
        //

        private PlayerMapMarker _playerMarker;

        public void OnShowInRaid(MapView map)
        {
            TryAddMarker(map);
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarker();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            TryRemoveMarker();

            if (GameUtils.IsInRaid())
            {
                TryAddMarker(map);
            }
        }

        public void OnDisable(MapView map)
        {
            TryRemoveMarker();
        }

        private void TryAddMarker(MapView map)
        {
            if (_playerMarker != null)
            {
                return;
            }

            var player = GameUtils.GetMainPlayer();
            if (player == null)
            {
                return;
            }

            // try adding the marker
            _playerMarker = map.AddPlayerMarker(player, _playerCategory, _playerColor, _playerImagePath);
        }

        private void TryRemoveMarker()
        {
            if (_playerMarker == null)
            {
                return;
            }

            _playerMarker.ContainingMapView.RemoveMapMarker(_playerMarker);
            _playerMarker = null;
        }

        public void OnHideInRaid(MapView map)
        {
            // do nothing
        }

        public void OnShowOutOfRaid(MapView map)
        {
            // do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }
    }
}
