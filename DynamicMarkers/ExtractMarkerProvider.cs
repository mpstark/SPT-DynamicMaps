using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using InGameMap.Data;
using InGameMap.UI.Components;
using InGameMap.Utils;
using UnityEngine;

namespace InGameMap.DynamicMarkers
{
    public class ExtractMarkerProvider : IDynamicMarkerProvider
    {
        private Dictionary<ExfiltrationPoint, MapMarker> _extractMarkers
            = new Dictionary<ExfiltrationPoint, MapMarker>();

        public void OnShowInRaid(MapView map, string mapInternalName)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var exfils = gameWorld.ExfiltrationController.EligiblePoints(GameUtils.GetMainPlayer().Profile);

            foreach (var exfil in exfils)
            {
                TryAddMarker(map, exfil);
            }
        }

        public void OnHideInRaid(MapView map)
        {
            // do nothing
        }

        public void OnRaidEnd(MapView map)
        {
            foreach (var exfil in _extractMarkers.Keys.ToList())
            {
                TryRemoveMarker(exfil);
            }
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
            foreach (var exfil in _extractMarkers.Keys.ToList())
            {
                TryRemoveMarker(exfil);
                TryAddMarker(map, exfil);
            }
        }

        private void TryAddMarker(MapView map, ExfiltrationPoint exfil)
        {
            if (_extractMarkers.ContainsKey(exfil))
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                Category = "extract",
                ImagePath = "Markers/exit.png",
                Text = exfil.Settings.Name.Localized(),
                Position = MathUtils.TransformToMapPosition(exfil.transform)
            };

            var marker = map.AddMapMarker(markerDef);
            marker.Color = Color.green;

            if (exfil.Status == EExfiltrationStatus.UncompleteRequirements)
            {
                marker.Color = Color.yellow;
            }

            _extractMarkers[exfil] = marker;
        }

        private void TryRemoveMarker(ExfiltrationPoint exfil)
        {
            if (!_extractMarkers.ContainsKey(exfil))
            {
                return;
            }

            _extractMarkers[exfil].ContainingMapView.RemoveMapMarker(_extractMarkers[exfil]);
            _extractMarkers.Remove(exfil);
        }
    }
}
