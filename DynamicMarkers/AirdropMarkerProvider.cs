using System.Collections.Generic;
using System.Linq;
using Aki.Custom.Airdrops;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class AirdropMarkerProvider : IDynamicMarkerProvider
    {
        private MapView _lastMapView;
        private Dictionary<AirdropBox, MapMarker> _airdropMarkers = new Dictionary<AirdropBox, MapMarker>();

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;

            // add all existing airdrops
            foreach (var airdrop in AirdropBoxOnBoxLandPatch.Airdrops)
            {
                TryAddMarker(airdrop);
            }

            // subscribe to new airdrops while map is open
            AirdropBoxOnBoxLandPatch.OnAirdropLanded += TryAddMarker;
        }

        public void OnHideInRaid(MapView map)
        {
            // unsubscribe to new airdrops, since we're hiding
            AirdropBoxOnBoxLandPatch.OnAirdropLanded -= TryAddMarker;
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            // transition markers from last map to this one
            foreach (var airdrop in _airdropMarkers.Keys.ToList())
            {
                TryRemoveMarker(airdrop);
                TryAddMarker(airdrop);
            }
        }

        public void OnDisable(MapView map)
        {
            OnRaidEnd(map);
        }

        private void TryAddMarker(AirdropBox airdrop)
        {
            if (_airdropMarkers.ContainsKey(airdrop))
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = "Airdrop",
                Color = Color.Lerp(Color.red, Color.white, 0.333f),
                ImagePath = "Markers/airdrop.png",
                Position = MathUtils.ConvertToMapPosition(airdrop.transform),
                Pivot = new Vector2(0.5f, 0.25f),
                Text = "Airdrop"
            };

            var marker = _lastMapView.AddMapMarker(markerDef);
            _airdropMarkers[airdrop] = marker;
        }

        private void TryRemoveMarkers()
        {
            foreach (var airdrop in _airdropMarkers.Keys.ToList())
            {
                TryRemoveMarker(airdrop);
            }
        }

        private void TryRemoveMarker(AirdropBox airdrop)
        {
            if (!_airdropMarkers.ContainsKey(airdrop))
            {
                return;
            }

            _airdropMarkers[airdrop].ContainingMapView.RemoveMapMarker(_airdropMarkers[airdrop]);
            _airdropMarkers.Remove(airdrop);
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
