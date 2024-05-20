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

                // update color based on exfil status
                UpdateExfilStatus(exfil, exfil.Status);

                // subscribe to status changes while map is shown
                exfil.OnStatusChanged += UpdateExfilStatus;
                Plugin.Log.LogInfo($"Subscribed to {exfil.Settings.Name.Localized()}");
            }
        }

        public void OnHideInRaid(MapView map)
        {
            Plugin.Log.LogInfo($"OnHideInRaid");

            // unsubscribe from updates while map is hidden
            foreach (var exfil in _extractMarkers.Keys)
            {
                exfil.OnStatusChanged -= UpdateExfilStatus;
                Plugin.Log.LogInfo($"Unsubscribed from {exfil.Settings.Name.Localized()}");
            }
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

        private void UpdateExfilStatus(ExfiltrationPoint exfil, EExfiltrationStatus status)
        {
            if (!_extractMarkers.ContainsKey(exfil))
            {
                return;
            }

            var marker = _extractMarkers[exfil];
            switch (exfil.Status)
            {
                case EExfiltrationStatus.NotPresent:
                    marker.Color = Color.red;
                    Plugin.Log.LogInfo($"Changed {exfil.Settings.Name.Localized()} to red");
                    break;
                case EExfiltrationStatus.UncompleteRequirements:
                    marker.Color = Color.yellow;
                    Plugin.Log.LogInfo($"Changed {exfil.Settings.Name.Localized()} to yellow");
                    return;
                default:
                    marker.Color = Color.green;
                    Plugin.Log.LogInfo($"Changed {exfil.Settings.Name.Localized()} to green");
                    break;
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
                Category = "Extract",
                ImagePath = "Markers/exit.png",
                Text = exfil.Settings.Name.Localized(),
                Position = MathUtils.ConvertToMapPosition(exfil.transform)
            };

            var marker = map.AddMapMarker(markerDef);
            _extractMarkers[exfil] = marker;
        }

        private void TryRemoveMarker(ExfiltrationPoint exfil)
        {
            if (!_extractMarkers.ContainsKey(exfil))
            {
                return;
            }

            exfil.OnStatusChanged -= UpdateExfilStatus;
            Plugin.Log.LogInfo($"Unsubscribed from {exfil.Settings.Name.Localized()}");

            _extractMarkers[exfil].ContainingMapView.RemoveMapMarker(_extractMarkers[exfil]);
            _extractMarkers.Remove(exfil);
        }
    }
}
