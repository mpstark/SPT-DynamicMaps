using InGameMap.Data;
using InGameMap.UI.Components;
using InGameMap.Utils;
using UnityEngine;

namespace InGameMap.DynamicMarkers
{
    public class PlayerMarkerProvider : IDynamicMarkerProvider
    {
        private PlayerMapMarker _playerMarker;

        public void OnShowInRaid(MapView map, string mapInternalName)
        {
            TryAddMarker(map);
        }

        public void OnHideInRaid(MapView map)
        {
            // do nothing
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarker();
        }

        public void OnShowOutOfRaid(MapView map)
        {
            // do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            // map will clear all markers
            _playerMarker = null;
            TryAddMarker(map);
        }

        private void TryAddMarker(MapView map)
        {
            if (_playerMarker != null)
            {
                TryRemoveMarker();
            }

            var player = GameUtils.GetMainPlayer();
            if (player == null)
            {
                return;
            }

            // try adding the marker
            _playerMarker = map.AddPlayerMarker(player, "player");
            _playerMarker.Color = Color.green;
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
    }
}
