using System.IO;
using System.Linq;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class LockedDoorMarkerMutator : IDynamicMarkerProvider
    {
        // FIXME: move to configuration somehow
        private static string doorWithKeyPath = Path.Combine(Plugin.Path, "Markers/door_with_key.png");
        private static string doorWithLockPath = Path.Combine(Plugin.Path, "Markers/door_with_lock.png");

        public void OnShowInRaid(MapView map)
        {
            var player = GameUtils.GetMainPlayer();
            var markers = map.GetMapMarkersByCategory("LockedDoor");

            foreach (var marker in markers)
            {
                if (string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                {
                    continue;
                }

                // TODO: GetAllItems is a BSG extension method under GClass
                var hasKey = player.Inventory.Equipment.GetAllItems().Any(i => i.TemplateId == marker.AssociatedItemId);

                marker.Image.sprite = hasKey
                    ? TextureUtils.GetOrLoadCachedSprite(doorWithKeyPath)
                    : TextureUtils.GetOrLoadCachedSprite(doorWithLockPath);

                marker.Color = hasKey
                    ? Color.green
                    : Color.red;
            }
        }

        public void OnShowOutOfRaid(MapView map)
        {
            var profile = GameUtils.GetPlayerProfile();
            var markers = map.GetMapMarkersByCategory("LockedDoor");

            foreach (var marker in markers)
            {
                if (string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                {
                    continue;
                }

                // TODO: GetAllItems is a BSG extension method under GClass
                var keyInStash = profile.Inventory.Stash.GetAllItems().Any(i => i.TemplateId == marker.AssociatedItemId);
                var keyInEquipment = profile.Inventory.Equipment.GetAllItems().Any(i => i.TemplateId == marker.AssociatedItemId);

                // change icon
                marker.Image.sprite = (keyInStash || keyInEquipment)
                    ? TextureUtils.GetOrLoadCachedSprite(doorWithKeyPath)
                    : TextureUtils.GetOrLoadCachedSprite(doorWithLockPath);

                // change color
                marker.Color = keyInEquipment
                    ? Color.green
                    : keyInStash
                        ? Color.yellow
                        : Color.red;
            }
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            if (GameUtils.IsInRaid())
            {
                OnShowInRaid(map);
            }
            else
            {
                OnShowOutOfRaid(map);
            }
        }

        public void OnDisable(MapView map)
        {
            var markers = map.GetMapMarkersByCategory("LockedDoor");

            // replace all markers with yellow and with lock
            foreach (var marker in markers)
            {
                if (string.IsNullOrWhiteSpace(marker.AssociatedItemId))
                {
                    continue;
                }

                marker.Image.sprite = TextureUtils.GetOrLoadCachedSprite(doorWithLockPath);
                marker.Color = Color.yellow;
            }
        }

        public void OnRaidEnd(MapView map)
        {
            // do nothing
        }

        public void OnHideInRaid(MapView map)
        {
            // do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }
    }
}
