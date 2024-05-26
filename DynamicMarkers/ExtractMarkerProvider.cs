using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class ExtractMarkerProvider : IDynamicMarkerProvider
    {
        private bool _showExtractStatusInRaid = true;
        public bool ShowExtractStatusInRaid
        {
            get
            {
                return _showExtractStatusInRaid;
            }

            set
            {
                if (_showExtractStatusInRaid == value)
                {
                    return;
                }

                _showExtractStatusInRaid = value;

                // force update all statuses
                foreach (var exfil in _extractMarkers.Keys)
                {
                    UpdateExfilStatus(exfil, exfil.Status);
                }
            }
        }

        private Dictionary<ExfiltrationPoint, MapMarker> _extractMarkers
            = new Dictionary<ExfiltrationPoint, MapMarker>();

        public void OnShowInRaid(MapView map)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var profile = GameUtils.GetMainPlayer().Profile;

            // get valid exfil points
            ExfiltrationPoint[] exfils;
            if (!GameUtils.IsScavRaid())
            {
                exfils = gameWorld.ExfiltrationController.EligiblePoints(profile);
            }
            else
            {
                var profileId = profile.ProfileId;
                exfils = gameWorld.ExfiltrationController.ScavExfiltrationPoints
                            .Where(p => p.EligibleIds.Contains(profileId))
                            .ToArray();
            }

            foreach (var exfil in exfils)
            {
                TryAddMarker(map, exfil);

                // update color based on exfil status
                UpdateExfilStatus(exfil, exfil.Status);

                // subscribe to status changes while map is shown
                exfil.OnStatusChanged += UpdateExfilStatus;
            }
        }

        public void OnHideInRaid(MapView map)
        {
            // unsubscribe from updates while map is hidden
            foreach (var exfil in _extractMarkers.Keys)
            {
                exfil.OnStatusChanged -= UpdateExfilStatus;
            }
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            foreach (var exfil in _extractMarkers.Keys.ToList())
            {
                TryRemoveMarker(exfil);
                TryAddMarker(map, exfil);
            }
        }

        public void OnDisable(MapView map)
        {
            TryRemoveMarkers();
        }

        private void TryRemoveMarkers()
        {
            foreach (var exfil in _extractMarkers.Keys.ToList())
            {
                TryRemoveMarker(exfil);
            }
        }

        private void UpdateExfilStatus(ExfiltrationPoint exfil, EExfiltrationStatus status)
        {
            if (!_extractMarkers.ContainsKey(exfil))
            {
                return;
            }

            var marker = _extractMarkers[exfil];
            if (!_showExtractStatusInRaid)
            {
                marker.Color = Color.yellow;
                return;
            }

            switch (exfil.Status)
            {
                case EExfiltrationStatus.NotPresent:
                    marker.Color = Color.red;
                    break;
                case EExfiltrationStatus.UncompleteRequirements:
                    marker.Color = Color.yellow;
                    return;
                default:
                    marker.Color = Color.green;
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
                Text = exfil.Settings.Name.BSGLocalized(),
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

            _extractMarkers[exfil].ContainingMapView.RemoveMapMarker(_extractMarkers[exfil]);
            _extractMarkers.Remove(exfil);
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
