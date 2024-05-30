using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class CorpseMarkerProvider : IDynamicMarkerProvider
    {
        private static FieldInfo _playerCorpseField = AccessTools.Field(typeof(Player), "Corpse");

        private bool _showFriendlyCorpses = true;
        public bool ShowFriendlyCorpses
        {
            get
            {
                return _showFriendlyCorpses;
            }

            set
            {
                if (value == _showFriendlyCorpses)
                {
                    return;
                }

                _showFriendlyCorpses = value;

                if (_showFriendlyCorpses)
                {
                    TryAddMarkers();
                }
                else
                {
                    RemoveDisabledMarkers();
                }
            }
        }

        private bool _showOtherCorpses = false;
        public bool ShowOtherCorpses
        {
            get
            {
                return _showOtherCorpses;
            }

            set
            {
                if (value == _showOtherCorpses)
                {
                    return;
                }

                _showOtherCorpses = value;

                if (_showOtherCorpses)
                {
                    TryAddMarkers();
                }
                else
                {
                    RemoveDisabledMarkers();
                }
            }
        }

        private bool _showKilledCorpses = false;
        public bool ShowKilledCorpses
        {
            get
            {
                return _showKilledCorpses;
            }

            set
            {
                if (value == _showKilledCorpses)
                {
                    return;
                }

                _showKilledCorpses = value;

                if (_showKilledCorpses)
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
        private Dictionary<Player, MapMarker> _corpseMarkers = new Dictionary<Player, MapMarker>();

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;

            TryAddMarkers();

            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer += OnUnregisterPlayer;
        }

        public void OnHideInRaid(MapView map)
        {
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
        }

        public void OnRaidEnd(MapView map)
        {
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;

            _lastMapView = map;
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            foreach (var corpse in _corpseMarkers.Keys.ToList())
            {
                TryRemoveMarker(corpse);
                TryAddMarker(corpse);
            }
        }

        public void OnDisable(MapView map)
        {
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
            TryRemoveMarkers();
        }

        private void OnUnregisterPlayer(IPlayer iPlayer)
        {
            if (!(iPlayer is Player))
            {
                return;
            }

            var player = iPlayer as Player;
            var corpse = _playerCorpseField.GetValue(player) as Corpse;
            if (corpse != null)
            {
                TryAddMarker(player);
            }
        }

        private void TryRemoveMarkers()
        {
            foreach (var corpse in _corpseMarkers.Keys.ToList())
            {
                TryRemoveMarker(corpse);
            }

            _corpseMarkers.Clear();
        }

        private void TryAddMarkers()
        {
            if (!GameUtils.IsInRaid())
            {
                return;
            }

            // add all players that have spawned already in raid
            var gameWorld = Singleton<GameWorld>.Instance;
            foreach (var player in gameWorld.AllPlayersEverExisted)
            {
                var corpse = _playerCorpseField.GetValue(player) as Corpse;
                if (corpse == null || _corpseMarkers.ContainsKey(player))
                {
                    continue;
                }

                TryAddMarker(player);
            }
        }

        private void TryAddMarker(Player player)
        {
            if (_lastMapView == null || _corpseMarkers.ContainsKey(player))
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = "Other Corpse",
                ImagePath = "Markers/skull.png",
                Text = player.Profile.GetCorrectedNickname(),
                Color = Color.yellow,
                Position = MathUtils.ConvertToMapPosition(player.Position)
            };

            // set category and color
            var victims = GameUtils.GetMainPlayer()?.Profile?.EftStats?.Victims;
            var mainPlayerGroupId = GameUtils.GetMainPlayer().GroupId;

            var victim = victims.FirstOrDefault(v => v.ProfileId == player.ProfileId);
            if (!string.IsNullOrEmpty(mainPlayerGroupId) && player.GroupId == mainPlayerGroupId)
            {
                markerDef.Color = Color.blue;
                markerDef.Category = "Friendly Corpse";
            }
            else if (victim != null && OtherPlayersMarkerProvider.PlayerIsBoss(player))
            {
                var orangeColor = new Color(255,165,0);
				markerDef.Color = orangeColor;
				markerDef.Category = "Boss Corpse";
			}
            else if (victim != null)
            {
                markerDef.Color = Color.red;
                markerDef.Category = "Killed Corpse";
            }

            if (markerDef.Category == "Friendly Corpse" && !_showFriendlyCorpses
             || (markerDef.Category == "Killed Corpse" || markerDef.Category == "Boss Corpse") && !_showKilledCorpses
             || markerDef.Category == "Other Corpse" && !_showOtherCorpses)
            {
                return;
            }

            // try adding marker
            var marker = _lastMapView.AddMapMarker(markerDef);
            _corpseMarkers[player] = marker;
        }

        private void RemoveDisabledMarkers()
        {
            foreach (var corpse in _corpseMarkers.Keys.ToList())
            {
                var marker = _corpseMarkers[corpse];

                if ((!_showFriendlyCorpses && marker.Category == "Friendly Corpse")
                 || (!_showKilledCorpses && marker.Category == "Killed Corpse")
                 || (!_showOtherCorpses && marker.Category == "Other Corpse"))
                {
                    TryRemoveMarker(corpse);
                }
            }
        }

        private void TryRemoveMarker(Player player)
        {
            if (!_corpseMarkers.ContainsKey(player))
            {
                return;
            }

            _corpseMarkers[player].ContainingMapView.RemoveMapMarker(_corpseMarkers[player]);
            _corpseMarkers.Remove(player);
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
