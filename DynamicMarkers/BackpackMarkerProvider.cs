using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.UI.DragAndDrop;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class BackpackMarkerProvider : IDynamicMarkerProvider
    {
        // TODO: move to config
        private const string _backpackCategory = "Backpack";
        private const string _backpackImagePath = "Markers/backpack.png";
        private static Color _backpackColor = Color.green;
        private static Vector2 _backpackSize = new Vector2(30f, 30f);
        //

        private MapView _lastMapView;
        private Dictionary<int, MapMarker> _backpackMarkers = new Dictionary<int, MapMarker>();

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;

            RemoveStaleMarkers();
            AddThrownBackpacks();

            // register for if an item is registered
            GameWorldRegisterLootItemPatch.OnRegisterLoot += OnRegisterLoot;

            // register for if item is destroyed (probably picked up)
            GameWorldDestroyLootPatch.OnDestroyLoot += OnDestroyLoot;
        }

        public void OnHideInRaid(MapView map)
        {
            GameWorldRegisterLootItemPatch.OnRegisterLoot -= OnRegisterLoot;
            GameWorldDestroyLootPatch.OnDestroyLoot -= OnDestroyLoot;
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;
            var gameWorld = Singleton<GameWorld>.Instance;

            foreach (var itemNetId in _backpackMarkers.Keys.ToList())
            {
                TryRemoveMarker(itemNetId);

                // check if item exists as loot item in the world
                if (!gameWorld.LootItems.ContainsKey(itemNetId))
                {
                    continue;
                }

                TryAddMarker(map, gameWorld.LootItems.GetByKey(itemNetId));
            }
        }

        public void OnRaidEnd(MapView map)
        {
            GameWorldRegisterLootItemPatch.OnRegisterLoot -= OnRegisterLoot;
            GameWorldDestroyLootPatch.OnDestroyLoot -= OnDestroyLoot;
            TryRemoveMarkers();
        }

        public void OnDisable(MapView map)
        {
            GameWorldRegisterLootItemPatch.OnRegisterLoot -= OnRegisterLoot;
            GameWorldDestroyLootPatch.OnDestroyLoot -= OnDestroyLoot;
            TryRemoveMarkers();
        }

        private void AddThrownBackpacks()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            foreach (var pair in PlayerInventoryThrowItemPatch.ThrownItems)
            {
                var itemNetId = pair.Key;
                var item = pair.Value;
                var itemType = ItemViewFactory.GetItemType(item.GetType());

                // check if item is already in markers or is not a backpack and doesn't not exist as loot item in the world
                if (_backpackMarkers.ContainsKey(itemNetId)
                    || itemType != EItemType.Backpack
                    || !gameWorld.LootItems.ContainsKey(itemNetId))
                {
                    continue;
                }

                TryAddMarker(_lastMapView, gameWorld.LootItems.GetByKey(itemNetId));
            }
        }

        private void OnRegisterLoot(LootItem lootItem)
        {
            if (lootItem == null || lootItem.Item == null)
            {
                return;
            }

            var itemNetId = lootItem.GetNetId();
            var itemType = ItemViewFactory.GetItemType(lootItem.Item.GetType());

            if (_backpackMarkers.ContainsKey(itemNetId)
                || itemType != EItemType.Backpack
                || !PlayerInventoryThrowItemPatch.ThrownItems.ContainsKey(itemNetId))
            {
                return;
            }

            TryAddMarker(_lastMapView, lootItem);
        }

        private void OnDestroyLoot(LootItem lootItem)
        {
            if (lootItem == null || lootItem.Item == null)
            {
                return;
            }

            TryRemoveMarker(lootItem.GetNetId());
        }

        private void RemoveStaleMarkers()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            foreach (var itemNetId in _backpackMarkers.Keys.ToList())
            {
                // check if item is in world
                if (gameWorld.LootItems.ContainsKey(itemNetId))
                {
                    continue;
                }

                TryRemoveMarker(itemNetId);
            }
        }

        private void TryAddMarker(MapView map, LootItem lootItem)
        {
            if (lootItem == null || lootItem.Item == null)
            {
                return;
            }

            var itemNetId = lootItem.GetNetId();
            if (_backpackMarkers.ContainsKey(itemNetId))
            {
                return;
            }

            // try adding the marker
            var marker = map.AddTransformMarker(lootItem.TrackableTransform, lootItem.Item.ShortName.BSGLocalized(),
                                                _backpackCategory, _backpackColor, _backpackImagePath, _backpackSize);

            _backpackMarkers[itemNetId] = marker;
        }

        private void TryRemoveMarker(int itemNetId)
        {
            if (!_backpackMarkers.ContainsKey(itemNetId))
            {
                return;
            }

            _backpackMarkers[itemNetId].ContainingMapView.RemoveMapMarker(_backpackMarkers[itemNetId]);
            _backpackMarkers.Remove(itemNetId);
        }

        private void TryRemoveMarkers()
        {
            foreach (var itemNetId in _backpackMarkers.Keys.ToList())
            {
                TryRemoveMarker(itemNetId);
            }
            _backpackMarkers.Clear();
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }

        public void OnShowOutOfRaid(MapView map)
        {
            // do nothing
        }
    }
}
