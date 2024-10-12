using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Comfort.Common;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;
using System.Collections;
using DynamicMaps.Config;

namespace DynamicMaps.DynamicMarkers
{
    public class EnemyHotZonesProvider : IDynamicMarkerProvider
    {
        private const string _circleImagePath = "Markers/plain-circle.png";

        // TODO: bring these all out to config

        private static Color _enemyPlayerColor = Color.red;

        private static Color _scavColor = Color.Lerp(Color.red, Color.yellow, 0.5f);

        private static Color _bossColor = Color.Lerp(Color.red, Color.yellow, 0.7f);

        //
        private static Vector2 _markerSize = new Vector2(30, 30);

        private MapView _lastMapView;
        private Dictionary<Player, MapMarker> _playerHotZoneMarkers = new Dictionary<Player, MapMarker>();
        
        public EnemyHotZonesProvider()
            {
                // do nothing
            }

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;
            PerformUpdate();
            
        }

        private void PerformUpdate()
        {
                TryRemoveMarkers();
                TryAddMarkers();
                RemoveNonActivePlayers();

        }
        
        
        public void OnHideInRaid(MapView map)
        {
            // do nothing
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            foreach (var player in _playerHotZoneMarkers.Keys.ToList())
            {
                TryRemoveMarker(player);
                TryAddMarker(player);
            }
        }

        public void OnDisable(MapView map)
        {
            // do nothing
        }

        private void TryRemoveMarkers()
        {
            foreach (var player in _playerHotZoneMarkers.Keys.ToList())
            {
                TryRemoveMarker(player);
            }

            _playerHotZoneMarkers.Clear();
        }

        private void TryAddMarkers()
        {
            if (!GameUtils.IsInRaid())
            {
                return;
            }
            // add all players that have spawned already in raid
            var gameWorld = Singleton<GameWorld>.Instance;
            foreach (var player in gameWorld.AllAlivePlayersList)
            {
                if (player.IsYourPlayer)
                {
                    continue;
                }

                TryAddMarker(player);
            }
        }

        private void OnUnregisterPlayer(IPlayer iPlayer)
        {
            // do nothing
        }

        private void RemoveNonActivePlayers()
        {
            var alivePlayers = new HashSet<Player>(Singleton<GameWorld>.Instance.AllAlivePlayersList);
            foreach (var player in _playerHotZoneMarkers.Keys.ToList())
            {
                if (player.HasCorpse() || !alivePlayers.Contains(player))
                {
                    TryRemoveMarker(player);
                }
            }
        }

        private void TryAddMarker(IPlayer iPlayer)
        {
            var player = iPlayer as Player;
            
            if (player == null)
            {
                return;
            }

            if (_lastMapView == null || player.IsBTRShooter() || _playerHotZoneMarkers.ContainsKey(player))
            {
                return;
            }

            // set category and color
            var category = "HotZone";
            var imagePath = _circleImagePath;
            var color = _scavColor;

            if (player.IsTrackedBoss() || Settings.UnifyZonesColors.Value)
            {
                color = _bossColor;
            }
            else if (player.IsPMC()|| Settings.UnifyZonesColors.Value)
            {
                color = _enemyPlayerColor;
            }

            var position = MathUtils.ConvertToMapPosition(player.Position);
            
            // try adding marker
            var marker = _lastMapView.AddHotZonesMarker(category, "HotZone", color, imagePath, position,_markerSize,Settings.HotZonesMarkerScale.Value);
            _playerHotZoneMarkers[player] = marker;
        }

        private void RemoveDisabledMarkers()
        {
            foreach (var player in _playerHotZoneMarkers.Keys.ToList())
            {
                    TryRemoveMarker(player);
            }
        }

        private void TryRemoveMarker(Player player)
        {
            if (!_playerHotZoneMarkers.ContainsKey(player))
            {
                return;
            }
            _playerHotZoneMarkers[player].ContainingMapView.RemoveMapMarker(_playerHotZoneMarkers[player]);
            _playerHotZoneMarkers.Remove(player);
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
