using System.Collections.Generic;
using DynamicMaps.Data;
using DynamicMaps.DynamicMarkers;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;

namespace DynamicMaps
{
    public class QuestMarkerProvider : IDynamicMarkerProvider
    {
        private List<MapMarker> _questMarkers = new List<MapMarker>();

        public void OnShowInRaid(MapView map, string mapInternalName)
        {
            if (GameUtils.IsScavRaid())
            {
                return;
            }

            QuestUtils.TryCaptureQuestData();
            AddQuestObjectiveMarkers(map);
        }

        public void OnHideInRaid(MapView map)
        {
            // TODO: don't just be lazy and try to update markers
            RemoveAllMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            if (!GameUtils.IsInRaid())
            {
                return;
            }

            RemoveAllMarkers();
            AddQuestObjectiveMarkers(map);
        }

        public void OnRaidEnd(MapView map)
        {
            QuestUtils.DiscardQuestData();
            RemoveAllMarkers();
        }

        private void AddQuestObjectiveMarkers(MapView map)
        {
            var player = GameUtils.GetMainPlayer();

            var markerDefs = QuestUtils.GetMarkerDefsForPlayer(player);
            foreach (var markerDef in markerDefs)
            {
                var marker = map.AddMapMarker(markerDef);
                _questMarkers.Add(marker);
            }
        }

        private void RemoveAllMarkers()
        {
            foreach (var marker in _questMarkers)
            {
                marker.ContainingMapView.RemoveMapMarker(marker);
            }
            _questMarkers.Clear();
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
