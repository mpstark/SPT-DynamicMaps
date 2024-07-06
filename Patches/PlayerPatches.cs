using System;
using System.Collections.Generic;
using System.Reflection;
using SPT.Reflection.Patching;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class PlayerOnDeadPatch : ModulePatch
    {
        internal static event Action<Player> OnDead;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.OnDead));
        }

        [PatchPostfix]
        public static void PatchPostfix(Player __instance)
        {
            OnDead?.Invoke(__instance);
        }
    }

    internal class PlayerInventoryThrowItemPatch : ModulePatch
    {
        private static FieldInfo _playerInventoryControllerPlayerField = AccessTools.Field(typeof(Player.PlayerInventoryController), "player_0");

        internal static event Action<int, Item> OnThrowItem;
        internal static Dictionary<int, Item> ThrownItems = new Dictionary<int, Item>();

        private bool _hasRegisteredEvents = false;

        protected override MethodBase GetTargetMethod()
        {
            if (!_hasRegisteredEvents)
            {
                GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;
                GameWorldDestroyLootPatch.OnDestroyLoot += OnDestroyLoot;
                _hasRegisteredEvents = true;
            }

            return AccessTools.Method(typeof(Player.PlayerInventoryController),
                                      nameof(Player.PlayerInventoryController.ThrowItem));
        }

        [PatchPostfix]
        public static void PatchPostfix(Player.PlayerInventoryController __instance, Item item)
        {
            // only look at main player's dropped items
            var player = _playerInventoryControllerPlayerField.GetValue(__instance) as Player;
            if (player == null || player != GameUtils.GetMainPlayer())
            {
                return;
            }

            var itemNetId = item.Id.GetHashCode();
            ThrownItems[itemNetId] = item;
            OnThrowItem?.Invoke(itemNetId, item);
        }

        internal static void OnDestroyLoot(LootItem lootItem)
        {
            if (lootItem == null || lootItem.Item == null)
            {
                return;
            }

            ThrownItems.Remove(lootItem.GetNetId());
        }

        internal static void OnRaidEnd()
        {
            ThrownItems.Clear();
        }
    }
}
