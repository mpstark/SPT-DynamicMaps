using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using InGameMap.Data;
using InGameMap.UI.Components;
using InGameMap.Utils;
using UnityEngine;

namespace InGameMap.DynamicMarkers
{
    public class OtherPlayersMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _lastMapView;
        private Dictionary<IPlayer, PlayerMapMarker> _playerMarkers = new Dictionary<IPlayer, PlayerMapMarker>();

        public void OnShowInRaid(MapView map, string mapInternalName)
        {
            // add all players that have spawned already in raid
            var gameWorld = Singleton<GameWorld>.Instance;
                foreach (var player in gameWorld.AllAlivePlayersList)
                {
                    if (player.IsYourPlayer || _playerMarkers.ContainsKey(player))
                    {
                        continue;
                    }

                    TryAddMarker(player);
                }

            _lastMapView = map;

            // register to event to get all the new ones while map is showing
            gameWorld.OnPersonAdd += TryAddMarker;
        }

        public void OnHideInRaid(MapView map)
        {
            // unregister from event while map isn't showing
            var gameWorld = Singleton<GameWorld>.Instance;
            gameWorld.OnPersonAdd -= TryAddMarker;
        }

        public void OnShowOutOfRaid(MapView map)
        {
            // do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }

        public void OnRaidEnd(MapView map)
        {
            _lastMapView = map;

            foreach (var player in _playerMarkers.Keys.ToList())
            {
                TryRemoveMarker(player);
            }

            _playerMarkers.Clear();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            foreach (var player in _playerMarkers.Keys.ToList())
            {
                TryRemoveMarker(player);
                TryAddMarker(player);
            }
        }

        private void TryAddMarker(IPlayer player)
        {
            if (_lastMapView == null || _playerMarkers.ContainsKey(player))
            {
                return;
            }

            // set category and color
            var category = "scav";
            var color = Color.Lerp(Color.red, Color.yellow, 0.5f);
            var mainPlayerGroupId = GameUtils.GetMainPlayer().GroupId;
            if (!string.IsNullOrEmpty(mainPlayerGroupId) && player.GroupId == mainPlayerGroupId)
            {
                color = Color.blue;
                category = "allied-player";
            }
            else if (player.Profile.Side == EPlayerSide.Bear || player.Profile.Side == EPlayerSide.Usec)
            {
                color = Color.red;
                category = "enemy-player";
            }

            // try adding marker
            var marker = _lastMapView.AddPlayerMarker(player, category, color);
            player.OnIPlayerDeadOrUnspawn += TryRemoveMarker;

            _playerMarkers[player] = marker;
        }

        private void TryRemoveMarker(IPlayer player)
        {
            if (!_playerMarkers.ContainsKey(player))
            {
                return;
            }

            player.OnIPlayerDeadOrUnspawn -= TryRemoveMarker;
            _playerMarkers[player].ContainingMapView.RemoveMapMarker(_playerMarkers[player]);
            _playerMarkers.Remove(player);
        }
    }
}
