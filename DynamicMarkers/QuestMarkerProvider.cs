using System;
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
            RemoveAllMarkers();
        }

        private void AddQuestObjectiveMarkers(MapView map)
        {
            var player = GameUtils.GetMainPlayer();
            var quests = QuestUtils.GetIncompleteQuests(player);

            foreach (var quest in quests)
            {
                var questName = quest.Template.NameLocaleKey.BSGLocalized();

                try
                {
                    var conditions = QuestUtils.GetIncompleteQuestConditions(quest);
                    foreach (var condition in conditions)
                    {
                        var conditionName = condition.id.BSGLocalized();

                        var markerDefs = QuestUtils.GetMarkerDefsForCondition(condition, questName, conditionName);
                        foreach (var markerDef in markerDefs)
                        {
                            var marker = map.AddMapMarker(markerDef);
                            _questMarkers.Add(marker);
                        }
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Caught exception trying to add conditions for quest with quest: {questName}");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
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
