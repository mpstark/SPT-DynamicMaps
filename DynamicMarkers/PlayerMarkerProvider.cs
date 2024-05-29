using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class PlayerMarkerProvider : IDynamicMarkerProvider
    {
        private static string _arrowIconPath = "Markers/arrow.png";

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
            _playerMarker = map.AddPlayerMarker(player, "Player", Color.green, _arrowIconPath);
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
