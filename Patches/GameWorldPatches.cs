using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using DynamicMaps.Utils;

namespace DynamicMaps.Patches
{
    internal class GameWorldOnGameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        public static void PatchPostfix(GameWorld __instance)
        {
            QuestUtils.OnGameStarted(__instance);
        }
    }

    internal class GameWorldOnDestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnDestroy));
        }

        [PatchPostfix]
        public static void PatchPostfix()
        {
            Plugin.Instance.Map?.OnRaidEnd();
            QuestUtils.OnGameEnded();
        }
    }
}
