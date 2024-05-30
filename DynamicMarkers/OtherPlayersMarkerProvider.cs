using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class OtherPlayersMarkerProvider : IDynamicMarkerProvider
    {
        private static string _arrowIconPath = "Markers/arrow.png";
        private static string _starIconPath = "Markers/star.png";

        private static HashSet<WildSpawnType> _bosses = new HashSet<WildSpawnType>
        {
            WildSpawnType.bossBoar,
            WildSpawnType.bossBully,
            WildSpawnType.bossGluhar,
            WildSpawnType.bossKilla,
            WildSpawnType.bossKnight,
            WildSpawnType.followerBigPipe,
            WildSpawnType.followerBirdEye,
            WildSpawnType.bossKolontay,
            WildSpawnType.bossKojaniy,
            WildSpawnType.bossSanitar,
            WildSpawnType.bossTagilla,
            WildSpawnType.bossZryachiy,
            (WildSpawnType) 4206927,  // Punisher
            (WildSpawnType) 199, // Legion
        };

        private bool _showFriendlyPlayers = true;
        public bool ShowFriendlyPlayers
        {
            get
            {
                return _showFriendlyPlayers;
            }

            set
            {
                if (value == _showFriendlyPlayers)
                {
                    return;
                }

                _showFriendlyPlayers = value;

                if (_showFriendlyPlayers)
                {
                    TryAddMarkers();
                }
                else
                {
                    RemoveDisabledMarkers();
                }
            }
        }

        private bool _showEnemyPlayers = false;
        public bool ShowEnemyPlayers
        {
            get
            {
                return _showEnemyPlayers;
            }

            set
            {
                if (value == _showEnemyPlayers)
                {
                    return;
                }

                _showEnemyPlayers = value;

                if (_showEnemyPlayers)
                {
                    TryAddMarkers();
                }
                else
                {
                    RemoveDisabledMarkers();
                }
            }
        }

        private bool _showScavs = false;
        public bool ShowScavs
        {
            get
            {
                return _showScavs;
            }

            set
            {
                if (value == _showScavs)
                {
                    return;
                }

                _showScavs = value;

                if (_showScavs)
                {
                    TryAddMarkers();
                }
                else
                {
                    RemoveDisabledMarkers();
                }
            }
        }

        private bool _showBosses = false;
        public bool ShowBosses
        {
            get
            {
                return _showBosses;
            }

            set
            {
                if (value == _showBosses)
                {
                    return;
                }

                _showBosses = value;

                if (_showBosses)
                {
                    TryAddMarkers();
                }
                else
                {
                    RemoveDisabledMarkers();
                }
            }
        }

        private MapView _lastMapView;
        private Dictionary<IPlayer, PlayerMapMarker> _playerMarkers = new Dictionary<IPlayer, PlayerMapMarker>();

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;

            TryAddMarkers();

            // register to event to get all the new ones while map is showing
            Singleton<GameWorld>.Instance.OnPersonAdd += TryAddMarker;
        }

        public void OnHideInRaid(MapView map)
        {
            // unregister from event while map isn't showing
            Singleton<GameWorld>.Instance.OnPersonAdd -= TryAddMarker;
        }

        public void OnRaidEnd(MapView map)
        {
            Singleton<GameWorld>.Instance.OnPersonAdd -= TryAddMarker;

            _lastMapView = map;
            TryRemoveMarkers();
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

        public void OnDisable(MapView map)
        {
            Singleton<GameWorld>.Instance.OnPersonAdd -= TryAddMarker;

            TryRemoveMarkers();
        }

        private void TryRemoveMarkers()
        {
            foreach (var player in _playerMarkers.Keys.ToList())
            {
                TryRemoveMarker(player);
            }

            _playerMarkers.Clear();
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
                if (player.IsYourPlayer || _playerMarkers.ContainsKey(player))
                {
                    continue;
                }

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
            var category = "Scav";
            var color = Color.Lerp(Color.red, Color.yellow, 0.5f);
            var imagePath = _arrowIconPath;

            var mainPlayerGroupId = GameUtils.GetMainPlayer().GroupId;
            if (!string.IsNullOrEmpty(mainPlayerGroupId) && player.GroupId == mainPlayerGroupId)
            {
                color = Color.blue;
                category = "Friendly Player";
            }
            else if (player.Profile.Side == EPlayerSide.Bear || player.Profile.Side == EPlayerSide.Usec)
            {
                color = Color.red;
                category = "Enemy Player";
            }
            else if (player.Profile.Side == EPlayerSide.Savage && _bosses.Contains(player.Profile.Info.Settings.Role))
            {
                imagePath = _starIconPath;
                category = "Boss";
            }

            if (category == "Scav" && !_showScavs
             || category == "Boss" && !_showBosses
             || category == "Friendly Player" && !_showFriendlyPlayers
             || category == "Enemy Player" && !_showEnemyPlayers)
            {
                return;
            }

            // try adding marker
            var marker = _lastMapView.AddPlayerMarker(player, category, color, imagePath);
            player.OnIPlayerDeadOrUnspawn += TryRemoveMarker;

            _playerMarkers[player] = marker;
        }

        private void RemoveDisabledMarkers()
        {
            foreach (var player in _playerMarkers.Keys.ToList())
            {
                var marker = _playerMarkers[player];

                if ((!_showFriendlyPlayers && marker.Category == "Friendly Player")
                 || (!_showEnemyPlayers && marker.Category == "Enemy Player")
                 || (!_showScavs && marker.Category == "Scav")
                 || (!_showBosses && marker.Category == "Boss"))
                {
                    TryRemoveMarker(player);
                }
            }
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

        public void OnShowOutOfRaid(MapView map)
        {
            // do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }

        public static bool PlayerIsBoss(IPlayer player)
        {
			return player.Profile.Side == EPlayerSide.Savage && _bosses.Contains(player.Profile.Info.Settings.Role);
		}
    }
}
