using System;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using EFT.UI;
using HarmonyLib;

namespace SimpleCrosshair.Patches
{
    internal class BattleUIScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MapScreen), nameof(MapScreen.Show));
        }

        [PatchPostfix]
        public static void PatchPostfix(MapScreen __instance)
        {
            Plugin.Instance.TryAttachToBattleUIScreen(__instance);
        }
    }
}
