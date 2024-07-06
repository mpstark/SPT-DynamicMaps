using System;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class GameWorldOnDestroyPatch : ModulePatch
    {
        internal static event Action OnRaidEnd;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnDestroy));
        }

        [PatchPrefix]
        public static void PatchPrefix()
        {
            try
            {
                OnRaidEnd?.Invoke();
            }
            catch(Exception e)
            {
                Plugin.Log.LogError($"Caught error while doing end of raid calculations");
                Plugin.Log.LogError($"{e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
            }
        }
    }

    internal class GameWorldUnregisterPlayerPatch : ModulePatch
    {
        internal static event Action<IPlayer> OnUnregisterPlayer;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.UnregisterPlayer));
        }

        [PatchPostfix]
        public static void PatchPostfix(IPlayer iPlayer)
        {
            OnUnregisterPlayer?.Invoke(iPlayer);
        }
    }

    internal class GameWorldRegisterLootItemPatch : ModulePatch
    {
        internal static event Action<LootItem> OnRegisterLoot;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("RegisterLoot").MakeGenericMethod(typeof(LootItem));
        }

        [PatchPostfix]
        public static void PatchPostfix(LootItem loot)
        {
            OnRegisterLoot?.Invoke(loot);
        }
    }

    internal class GameWorldDestroyLootPatch : ModulePatch
    {
        internal static event Action<LootItem> OnDestroyLoot;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(m => m.Name == "DestroyLoot" && m.GetParameters().FirstOrDefault(p => p.Name == "loot") != null);
        }

        [PatchPrefix]
        public static void PatchPrefix(Object loot)
        {
            try
            {
                if (loot is LootItem lootItem)
                {
                    OnDestroyLoot?.Invoke(lootItem);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Caught error while running DestroyLoot patch");
                Plugin.Log.LogError($"{e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
            }
        }
    }
}
