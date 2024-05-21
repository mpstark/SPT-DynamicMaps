using System.Linq;
using InGameMap.Data;
using InGameMap.UI.Components;
using InGameMap.Utils;
using UnityEngine;

namespace InGameMap.DynamicMarkers
{
    public class LockedDoorMarkerMutator : IDynamicMarkerProvider
    {
        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            // do nothing
        }

        public void OnRaidEnd(MapView map)
        {
            // do nothing
        }

        public void OnShowInRaid(MapView map, string mapInternalName)
        {
            var player = GameUtils.GetMainPlayer();
            // player.Inventory.Equipment.FindItem()

            var markers = map.GetMapMarkersByCategory("LockedDoor");
            foreach (var marker in markers)
            {
                if (string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                {
                    continue;
                }

                var key = player.Inventory.Equipment.GetAllItems().FirstOrDefault(i => i.TemplateId == marker.AssociatedItemId);
                marker.Color = key != null
                    ? Color.green
                    : Color.red;
            }
        }

        public void OnHideInRaid(MapView map)
        {
            // do nothing
        }
    }
}
