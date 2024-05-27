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

        [PatchPostfix]
        public static void PatchPostfix()
        {
            OnRaidEnd?.Invoke();
        }
    }
}
