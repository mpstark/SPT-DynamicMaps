using System;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
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
}
